using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gtk;

// We use a custom namespace for WebSocket classes
namespace WSocket;

/// <summary>
/// Represents a WebSocket server that handles incoming WebSocket connections,
/// manages connected clients, and facilitates message broadcasting between them.
/// </summary>
public class WebSocketServer {
    /// <value>String containing the IPs to listen to.</value>
    private readonly string ip;
    /// <value>Port to listen to.</value>
    private readonly int port;
    /// <value><c>HttpListener</c> which listens from the standard library.</value>
    private readonly HttpListener listener;
    /// <value>Cancellation token source from the standard library.</value>
    private readonly CancellationTokenSource serverCancellation;
    /// <value>Dictionnary with all the web-sockets clients (<c>Guid</c> is the key).</value>
    private readonly ConcurrentDictionary<Guid, WebSocket> clients;
    /// <value>Dictionnary with all the state of the clients (<c>Guid</c> is the key).</value>
    private readonly ConcurrentDictionary<Guid, string> clientStates;
    /// <value>Buffer to which the server is linked.</value>
    private readonly GtkSource.Buffer editor_buffer;

    /// <value>Event raised when a message is received from any connected client.</value>
    public event EventHandler<string> MessageReceived;

    /// <value>Indicates whether the server has failed during startup. </value>
    public bool _Failed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketServer"/> class. This is the constructor.
    /// </summary>
    /// <param name="port">The port number to listen on.</param>
    /// <param name="editor_buffer">The GTK text buffer to send to clients on connect.</param>
    /// <param name="ip">The IP address to bind to (default is wildcard).</param>
    public WebSocketServer(int port, GtkSource.Buffer editor_buffer, string ip = "*") {
        this.ip = ip;
        this.port = port;
        this.listener = new HttpListener();
        this.listener.Prefixes.Add($"http://{ip}:{port}/");
        this.serverCancellation = new CancellationTokenSource();
        this.clients = new ConcurrentDictionary<Guid, WebSocket>();
        this.clientStates = new ConcurrentDictionary<Guid, string>();
        this.editor_buffer = editor_buffer;
    }

    /// <summary>
    /// Starts the WebSocket server and listens for incoming connections.
    /// </summary>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    public async Task StartAsync() {
        try {
            listener.Start();
            Console.WriteLine($"Server started on {ip}:{port}");

            while (!serverCancellation.Token.IsCancellationRequested) {
                var context = await listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest) {
                    ProcessWebSocketRequest(context);
                } else {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        } catch (Exception ex) {
            Console.WriteLine($"Server error: {ex.Message}");
            this._Failed = true;
            await StopAsync();
        }
    }

    /// <summary>
    /// Accepts and processes a new WebSocket client request.
    /// </summary>
    /// <param name="context">The HTTP listener context containing the WebSocket request.</param>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    private async Task ProcessWebSocketRequest(HttpListenerContext context) {
        try {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;
            var clientId = Guid.NewGuid();

            if (clients.TryAdd(clientId, webSocket)) {
                Console.WriteLine($"Client {clientId} connected");
                await BroadcastMessageToClient(clientId, $"full:{clientId}:START\n" + this.editor_buffer.Text + $"\nfull:{clientId}:STOP\n");
                await HandleClientSession(clientId, webSocket);
            }
        } catch (Exception ex) {
            Console.WriteLine($"WebSocket connection error: {ex.Message}");
            context.Response.StatusCode = 500;
            context.Response.Close();
        }
    }

    /// <summary>
    /// Handles the message loop for a connected WebSocket client.
    /// </summary>
    /// <param name="clientId">Unique identifier for the client.</param>
    /// <param name="webSocket">The WebSocket associated with the client.</param>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    private async Task HandleClientSession(Guid clientId, WebSocket webSocket) {
        var buffer = new byte[4096];
        try {
            while (webSocket.State == WebSocketState.Open) {
                try {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), serverCancellation.Token);

                    if (result.MessageType == WebSocketMessageType.Close) {
                        await HandleClientDisconnection(clientId, true);
                    }

                    if (result.MessageType == WebSocketMessageType.Text) {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await HandleMessage(clientId, message);
                    }
                } catch (WebSocketException wsEx) when (wsEx.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely || webSocket.State != WebSocketState.Open) {
                    await HandleClientDisconnection(clientId, false);
                    break;
                }
            }
        } catch (OperationCanceledException) {
            await HandleClientDisconnection(clientId, false);
        } catch (Exception ex) {
            Console.WriteLine($"Client {clientId} session error: {ex.Message}");
            await HandleClientDisconnection(clientId, false);
        }
    }

    /// <summary>
    /// Processes a message received from a client and broadcasts it to all other clients.
    /// </summary>
    /// <param name="senderId">The ID of the client that sent the message.</param>
    /// <param name="message">The message content.</param>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    private async Task HandleMessage(Guid senderId, string message) {
        MessageReceived?.Invoke(this, message);

        foreach (var client in clients) {
            if (client.Key != senderId && client.Value.State == WebSocketState.Open) {
                try {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await client.Value.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, serverCancellation.Token);
                } catch (WebSocketException) {
                    await HandleClientDisconnection(client.Key, false);
                }
            }
        }
    }

    /// <summary>
    /// Handles cleanup and removal of a disconnected client.
    /// </summary>
    /// <param name="clientId">The client's unique identifier.</param>
    /// <param name="isGraceful">True if disconnection was expected; otherwise false.</param>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    private async Task HandleClientDisconnection(Guid clientId, bool isGraceful) {
        if (clients.TryRemove(clientId, out var webSocket)) {
            try {
                if (webSocket.State == WebSocketState.Open && isGraceful) {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closing connection", CancellationToken.None);
                }
            } catch (WebSocketException) {
                // Ignore errors
            } finally {
                clientStates.TryRemove(clientId, out _);
                Console.WriteLine($"Client {clientId} disconnected {(isGraceful ? "gracefully" : "abruptly")}");
            }
        }
    }

    /// <summary>
    /// Stops the WebSocket server and gracefully disconnects all clients.
    /// </summary>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    public async Task StopAsync() {
        try {
            serverCancellation.Cancel();

            var disconnectionTasks = clients.Keys.Select(clientId => HandleClientDisconnection(clientId, true));
            await Task.WhenAll(disconnectionTasks).WaitAsync(TimeSpan.FromSeconds(5));

            listener.Stop();
            Console.WriteLine("Server stopped");
        } catch (Exception ex) {
            Console.WriteLine($"Error stopping server: {ex.Message}");
        }
    }

    /// <summary>
    /// Broadcasts a message to all connected clients.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    public async Task BroadcastMessage(string message) {
        var buffer = Encoding.UTF8.GetBytes(message);
        var failedClients = new List<Guid>();

        foreach (var client in clients) {
            try {
                if (client.Value.State == WebSocketState.Open) {
                    await client.Value.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, serverCancellation.Token);
                } else {
                    failedClients.Add(client.Key);
                }
            } catch (WebSocketException) {
                failedClients.Add(client.Key);
            }
        }

        foreach (var clientId in failedClients) {
            await HandleClientDisconnection(clientId, false);
        }
    }

    /// <summary>
    /// Sends a message to a specific connected client.
    /// </summary>
    /// <param name="clientId">The ID of the client to send the message to.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    public async Task BroadcastMessageToClient(Guid clientId, string message) {
        var buffer = Encoding.UTF8.GetBytes(message);
        var failedClients = new List<Guid>();

        var client_ws = clients[clientId];
        try {
            if (client_ws.State == WebSocketState.Open) {
                await client_ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, serverCancellation.Token);
            } else {
                failedClients.Add(clientId);
            }
        } catch (WebSocketException) {
            failedClients.Add(clientId);
        }
    }
}
