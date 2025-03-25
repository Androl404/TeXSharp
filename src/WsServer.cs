using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketServer {
    private readonly string ip;
    private readonly int port;
    private readonly HttpListener listener;
    private readonly CancellationTokenSource serverCancellation;
    private readonly ConcurrentDictionary<Guid, WebSocket> clients;
    private readonly ConcurrentDictionary<Guid, string> clientStates;

    public WebSocketServer(string ip = "*", int port = 6969) {
        this.ip = ip;
        this.port = port;
        this.listener = new HttpListener();
        this.listener.Prefixes.Add($"http://{ip}:{port}/");
        this.serverCancellation = new CancellationTokenSource();
        this.clients = new ConcurrentDictionary<Guid, WebSocket>();
        this.clientStates = new ConcurrentDictionary<Guid, string>();
    }

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
            await StopAsync();
        }
    }

    private async void ProcessWebSocketRequest(HttpListenerContext context) {
        try {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;
            var clientId = Guid.NewGuid();

            if (clients.TryAdd(clientId, webSocket)) {
                Console.WriteLine($"Client {clientId} connected");
                await HandleClientSession(clientId, webSocket);
            }
        } catch (Exception ex) {
            Console.WriteLine($"WebSocket connection error: {ex.Message}");
            context.Response.StatusCode = 500;
            context.Response.Close();
        }
    }

    private async Task HandleClientSession(Guid clientId, WebSocket webSocket) {
        var buffer = new byte[4096];
        try {
            while (webSocket.State == WebSocketState.Open) {
                try {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        serverCancellation.Token);

                    if (result.MessageType == WebSocketMessageType.Close) {
                        await HandleClientDisconnection(clientId, true);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text) {
                        var message =
                            Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await HandleMessage(clientId, message);
                    }
                } catch (WebSocketException wsEx)
                    when (wsEx.WebSocketErrorCode ==
                              WebSocketError.ConnectionClosedPrematurely ||
                          webSocket.State != WebSocketState.Open) {
                    // Handle abrupt client disconnection
                    await HandleClientDisconnection(clientId, false);
                    break;
                }
            }
        } catch (OperationCanceledException) {
            // Server is shutting down
            await HandleClientDisconnection(clientId, false);
        } catch (Exception ex) {
            Console.WriteLine($"Client {clientId} session error: {ex.Message}");
            await HandleClientDisconnection(clientId, false);
        }
    }

    private async Task HandleMessage(Guid senderId, string message) {
        Console.WriteLine($"Received from {senderId}: {message}");

        // Example: Broadcast message to all other clients
        foreach (var client in clients) {
            if (client.Key != senderId &&
                client.Value.State == WebSocketState.Open) {
                try {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await client.Value.SendAsync(new ArraySegment<byte>(bytes),
                                                 WebSocketMessageType.Text,
                                                 true,
                                                 serverCancellation.Token);
                } catch (WebSocketException) {
                    // Handle failed message delivery by removing the client
                    await HandleClientDisconnection(client.Key, false);
                }
            }
        }
    }

    private async Task HandleClientDisconnection(Guid clientId,
                                                 bool isGraceful) {
        if (clients.TryRemove(clientId, out var webSocket)) {
            try {
                if (webSocket.State == WebSocketState.Open && isGraceful) {
                    // Only try graceful shutdown if the connection is still
                    // open
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Server closing connection", CancellationToken.None);
                }
            } catch (WebSocketException) {
                // Ignore websocket errors during shutdown
            } finally {
                clientStates.TryRemove(clientId, out _);
                Console.WriteLine(
                    $"Client {clientId} disconnected {(isGraceful ? "gracefully" : "abruptly")}");
            }
        }
    }

    public async Task StopAsync() {
        try {
            serverCancellation.Cancel();

            // Close all client connections
            var disconnectionTasks = clients.Keys.Select(
                clientId => HandleClientDisconnection(clientId, true));

            // Wait for all clients to disconnect with timeout
            await Task.WhenAll(disconnectionTasks)
                .WaitAsync(TimeSpan.FromSeconds(5));

            listener.Stop();
            Console.WriteLine("Server stopped");
        } catch (Exception ex) {
            Console.WriteLine($"Error stopping server: {ex.Message}");
        }
    }

    public async Task BroadcastMessage(string message) {
        var buffer = Encoding.UTF8.GetBytes(message);
        var failedClients = new List<Guid>();

        foreach (var client in clients) {
            try {
                if (client.Value.State == WebSocketState.Open) {
                    await client.Value.SendAsync(new ArraySegment<byte>(buffer),
                                                 WebSocketMessageType.Text,
                                                 true,
                                                 serverCancellation.Token);
                } else {
                    failedClients.Add(client.Key);
                }
            } catch (WebSocketException) {
                failedClients.Add(client.Key);
            }
        }

        // Clean up any failed clients
        foreach (var clientId in failedClients) {
            await HandleClientDisconnection(clientId, false);
        }
    }
}

class Program {
    static async Task Main() {
        var server = new WebSocketServer();

        Console.CancelKeyPress += async (sender, e) => {
            e.Cancel = true;
            await server.StopAsync();
        };

        await server.StartAsync();
    }
}
