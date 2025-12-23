using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroTouch.UI.Services
{
    public class AppWebSocketClient
    {
        private ClientWebSocket? _client;
        private CancellationTokenSource? _cts;
        private Task? _receiveTask;

        public event Action<string>? OnMessageReceived;
        public event Action<string>? OnConnectionStatusChanged;

        public async Task ConnectAsync(string uri)
        {
            if (_client != null)
                return;

            // Create new instance for each connection attempt
            _client = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            
            try
            {
                await _client.ConnectAsync(new Uri(uri), _cts.Token);
                OnConnectionStatusChanged?.Invoke("Connected");
                Console.WriteLine($"Connected to {uri}");
                
                _receiveTask = Task.Run(ReceiveLoopAsync);
            }
            catch (Exception ex)
            {
                OnConnectionStatusChanged?.Invoke($"Error: {ex.Message}");
                Cleanup();
            }
        }
        
        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[2048];
            var sb = new StringBuilder();

            try
            {
                // start receiving messages
                while (_client?.State == WebSocketState.Open && !_cts!.IsCancellationRequested)
                {
                    var result = await _client.ReceiveAsync(buffer, _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                    if (result.EndOfMessage)
                    {
                        var message = sb.ToString();
                        sb.Clear();

                        OnMessageReceived?.Invoke(message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal on disconnect
            }
            catch (Exception ex)
            {
                OnConnectionStatusChanged?.Invoke($"Receive error: {ex.Message}");
            }
            finally
            {
                OnConnectionStatusChanged?.Invoke("Disconnected");
                Cleanup();
            }
        }

        public async Task DisconnectAsync()
        {
            if (_client == null)
                return;

            try
            {
                _cts?.Cancel();

                if (_client.State == WebSocketState.Open)
                {
                    await _client.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed by user",
                        CancellationToken.None
                    );
                }
            }
            catch { }
            finally
            {
                Cleanup();
            }
        }
        
        private void Cleanup()
        {
            _client?.Dispose();
            _cts?.Dispose();

            _client = null;
            _cts = null;
            _receiveTask = null;
        }
        
        public async Task SendAsync(string json)
        {
            if (_client == null || _client.State != WebSocketState.Open)
                return;

            var buffer = Encoding.UTF8.GetBytes(json);

            await _client.SendAsync(
                buffer,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
    }
}
