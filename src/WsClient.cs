using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// We use a custom namespace for WebSocket classes
namespace WSocket;

/// <summary>
/// Represents a simple WebSocket client that can connect to a server,
/// send and receive messages, and handle connection lifecycle events.
/// </summary>
/// <remarks>
/// This class implements the <c>IDisposable</c> interface.
/// </remarks>
public class WebSocketClient : IDisposable {
    /// <value>WebSocket client from the standart library.</value>
    private readonly ClientWebSocket webSocket;
    /// <value>Cancellation token source from the standard library.</value>
    private readonly CancellationTokenSource clientCancellation;
    /// <value>Server uri to connect.</value>
    private readonly Uri serverUri;
    /// <value>Task used to receive messages from the server.</value>
    private Task receiveTask;
    /// <value>Boolean, if the client is disposed (temrinated) or not.</value>
    private bool disposed;
    /// <value>Integer for the timeout for disconnection (in milliseconds).</value>
    private const int DisconnectTimeoutMs = 5000; // 5 second timeout for disconnection

    /// <value>Raised when a message is received from the server.</value>
    public event EventHandler<string> MessageReceived;

    /// <value>Raised when an error occurs during communication.</value>
    public event EventHandler<Exception> ErrorOccurred;

    /// <value>Raised when the client successfully connects to the server.</value>
    public event EventHandler Connected;

    /// <value>Raised when the client disconnects from the server.</value>
    public event EventHandler Disconnected;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketClient"/> class with the given server URL.
    /// </summary>
    /// <param name="serverUrl">The WebSocket server URL to connect to.</param>
    public WebSocketClient(string serverUrl) {
        this.serverUri = new Uri(serverUrl);
        this.webSocket = new ClientWebSocket();
        this.clientCancellation = new CancellationTokenSource();
    }

    /// <summary>
    /// Connects to the WebSocket server asynchronously.
    /// </summary>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    public async Task ConnectAsync() {
        try {
            if (webSocket.State != WebSocketState.Open) {
                await webSocket.ConnectAsync(serverUri, clientCancellation.Token);
                Connected?.Invoke(this, EventArgs.Empty);

                // Start receiving messages
                receiveTask = ReceiveMessagesAsync();
            }
        } catch (Exception ex) {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    /// <summary>
    /// Continuously listens for messages from the server in a background task.
    /// </summary>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    private async Task ReceiveMessagesAsync() {
        var buffer = new byte[4096];
        try {
            while (webSocket.State == WebSocketState.Open && !clientCancellation.Token.IsCancellationRequested) {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), clientCancellation.Token);

                if (result.MessageType == WebSocketMessageType.Close) {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text) {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessageReceived?.Invoke(this, message);
                }
            }
        } catch (OperationCanceledException) {
            // Normal cancellation, ignore
        } catch (WebSocketException) {
            // WebSocket was closed, ignore
        } catch (Exception ex) when (!clientCancellation.Token.IsCancellationRequested) {
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    /// <summary>
    /// Sends a text message to the connected WebSocket server.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    public async Task SendMessageAsync(string message) {
        try {
            if (webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("Connection is not open");

            var buffer = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, clientCancellation.Token);
        } catch (Exception ex) {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    /// <summary>
    /// Gracefully disconnects from the server asynchronously, with a timeout.
    /// </summary>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    public async Task DisconnectAsync() {
        if (disposed)
            return;

        try {
            clientCancellation.Cancel();

            using var timeoutCts = new CancellationTokenSource(DisconnectTimeoutMs);

            try {
                if (webSocket.State == WebSocketState.Open) {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", timeoutCts.Token);
                }
            } catch (OperationCanceledException) {
                webSocket.Abort();
            } catch (WebSocketException) {
                // Connection might already be closed
            }

            if (receiveTask != null && !receiveTask.IsCompleted) {
                try {
                    await Task.WhenAny(receiveTask, Task.Delay(1000));
                } catch {
                    // Ignore cleanup errors
                }
            }

            Disconnected?.Invoke(this, EventArgs.Empty);
        } catch (Exception ex) {
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    /// <summary>
    /// Disposes the WebSocketClient and releases all resources.
    /// </summary>
    /// <returns>This methods does return anything.</returns>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose pattern to cleanup managed/unmanaged resources.
    /// </summary>
    /// <remarks>
    /// This methods come from the <c>IDisposable</c> interface.
    /// </remarks>
    /// <param name="disposing">True to cleanup managed resources.</param>
    /// <returns>This methods does return anything.</returns>
    protected virtual void Dispose(bool disposing) {
        if (disposed)
            return;

        if (disposing) {
            try {
                Task.Run(async () => await DisconnectAsync()).Wait(DisconnectTimeoutMs);
            } catch {
                // Ignore forced disconnect errors
            }

            clientCancellation.Dispose();
            webSocket.Dispose();
        }

        disposed = true;
    }

    /// <value>Indicates whether the client is currently connected to the WebSocket server.</value>
    public bool IsConnected => webSocket.State == WebSocketState.Open;
}
