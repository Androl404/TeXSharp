using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WSocket;

public class WebSocketClient : IDisposable {
    private readonly ClientWebSocket webSocket;
    private readonly CancellationTokenSource clientCancellation;
    private readonly Uri serverUri;
    private Task receiveTask;
    private bool disposed;
    private const int DisconnectTimeoutMs =
        5000; // 5 second timeout for disconnection

    public event EventHandler<string> MessageReceived;
    public event EventHandler<Exception> ErrorOccurred;
    public event EventHandler Connected;
    public event EventHandler Disconnected;

    public WebSocketClient(string serverUrl) {
        this.serverUri = new Uri(serverUrl);
        this.webSocket = new ClientWebSocket();
        this.clientCancellation = new CancellationTokenSource();
    }

    public async Task ConnectAsync() {
        try {
            if (webSocket.State != WebSocketState.Open) {
                await webSocket.ConnectAsync(serverUri,
                                             clientCancellation.Token);
                Connected?.Invoke(this, EventArgs.Empty);

                // Start receiving messages
                receiveTask = ReceiveMessagesAsync();
            }
        } catch (Exception ex) {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    private async Task ReceiveMessagesAsync() {
        var buffer = new byte[4096];
        try {
            while (webSocket.State == WebSocketState.Open &&
                   !clientCancellation.Token.IsCancellationRequested) {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), clientCancellation.Token);

                if (result.MessageType == WebSocketMessageType.Close) {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text) {
                    var message =
                        Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessageReceived?.Invoke(this, message);
                }
            }
        } catch (OperationCanceledException) {
            // Normal cancellation, ignore
        } catch (WebSocketException) {
            // WebSocket was closed, ignore
        } catch (Exception ex)
            when (!clientCancellation.Token.IsCancellationRequested) {
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    public async Task SendMessageAsync(string message) {
        try {
            if (webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("Connection is not open");

            var buffer = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer),
                                      WebSocketMessageType.Text, true,
                                      clientCancellation.Token);
        } catch (Exception ex) {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    public async Task DisconnectAsync() {
        if (disposed)
            return;

        try {
            // Cancel ongoing operations
            clientCancellation.Cancel();

            // Create a timeout token
            using var timeoutCts =
                new CancellationTokenSource(DisconnectTimeoutMs);

            try {
                if (webSocket.State == WebSocketState.Open) {
                    // Try to close gracefully with timeout
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client disconnecting", timeoutCts.Token);
                }
            } catch (OperationCanceledException) {
                // Timeout or cancellation occurred, force close
                webSocket.Abort();
            } catch (WebSocketException) {
                // WebSocket might already be closed, ignore
            }

            // Wait for receive task with timeout if it exists
            if (receiveTask != null && !receiveTask.IsCompleted) {
                try {
                    await Task.WhenAny(receiveTask, Task.Delay(1000));
                } catch {
                    // Ignore any errors during receive task completion
                }
            }

            Disconnected?.Invoke(this, EventArgs.Empty);
        } catch (Exception ex) {
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (disposed)
            return;

        if (disposing) {
            try {
                // Force synchronous disconnection with timeout
                Task.Run(async () => await DisconnectAsync())
                    .Wait(DisconnectTimeoutMs);
            } catch {
                // Ignore any errors during forced disposal
            }

            clientCancellation.Dispose();
            webSocket.Dispose();
        }

        disposed = true;
    }

    public bool IsConnected => webSocket.State == WebSocketState.Open;
}

// Example usage
class Program {
    static async Task Main() {
        using var client = new WebSocketClient("ws://127.0.0.1:6969/");

        // Set up event handlers
        client.Connected += (s, e) => Console.WriteLine("Connected to server"); client.Disconnected += (s, e) =>
        Console.WriteLine("Disconnected from server");
        client.MessageReceived += (s, message) => Console.WriteLine($"Received: {message}");
        client.ErrorOccurred += (s, ex) => Console.WriteLine($"Error: {ex.Message}");

        try {
            await client.ConnectAsync();

            // Example: Send some messages
            await client.SendMessageAsync("Hello from client!");

            Console.WriteLine("Press Enter to disconnect...");
            while(true) client.SendMessageAsync(Console.ReadLine());

            // This should now complete within the timeout period
            await client.DisconnectAsync();
            Console.WriteLine("Disconnected successfully!");
        } catch (Exception ex) {
            Console.WriteLine($"Fatal error: {ex.Message}");
        } finally {
            // Ensure client is disposed
            client.Dispose();
        }
    }
}
