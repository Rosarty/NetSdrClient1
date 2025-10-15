using System;
using System.Buffers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class TcpClientWrapper : ITcpClient, IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private Task? _listenerTask;

        public bool Connected
        {
            get
            {
                var client = _tcpClient;
                var stream = _stream;
                return client != null && client.Connected && stream != null;
            }
        }

        public event EventHandler<ReadOnlyMemory<byte>>? MessageReceived;

        public TcpClientWrapper(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Connect()
        {
            if (Connected)
            {
                Console.WriteLine($"Already connected to {_host}:{_port}");
                return;
            }

            _tcpClient = new TcpClient();
            try
            {
                _tcpClient.Connect(_host, _port);
                _stream = _tcpClient.GetStream();
                _cts = new CancellationTokenSource();

                // Start listening in the background
                _listenerTask = Task.Run(() => StartListeningAsync(_cts.Token));

                Console.WriteLine($"Connected to {_host}:{_port}");
            }
            catch
            {
                _tcpClient?.Dispose();
                _tcpClient = null;
                _stream = null;
                _cts?.Dispose();
                _cts = null;
                throw;
            }
        }

        public void Disconnect()
        {
            if (!Connected)
            {
                Console.WriteLine("No active connection to disconnect.");
                return;
            }

            _cts?.Cancel();

            try
            {
                _listenerTask?.Wait(1000); // Wait a short time for listener to stop
            }
            catch { /* ignored */ }

            _stream?.Dispose();
            _tcpClient?.Dispose();
            _cts?.Dispose();

            _stream = null;
            _tcpClient = null;
            _cts = null;
            _listenerTask = null;

            Console.WriteLine("Disconnected.");
        }

        public async Task SendMessageAsync(byte[] data)
        {
            var stream = _stream;
            if (Connected && stream != null && stream.CanWrite)
            {
                LogHex(data);
                await stream.WriteAsync(data, 0, data.Length);
            }
            else
            {
                throw new InvalidOperationException("Not connected to a server.");
            }
        }

        public Task SendMessageAsync(string str)
        {
            var data = Encoding.UTF8.GetBytes(str);
            return SendMessageAsync(data);
        }

        private async Task StartListeningAsync(CancellationToken token)
        {
            if (_stream == null) throw new InvalidOperationException("Stream is null.");

            Console.WriteLine("Started listening for incoming messages.");

            byte[] buffer = ArrayPool<byte>.Shared.Rent(8194);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead > 0)
                    {
                        // Use ArraySegment to avoid extra allocations
                        MessageReceived?.Invoke(this, new ReadOnlyMemory<byte>(buffer, 0, bytesRead));
                    }
                    else
                    {
                        // Connection closed gracefully
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in listening loop: {ex.Message}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                Console.WriteLine("Listener stopped.");
            }
        }

        private void LogHex(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 3);
            foreach (var b in data)
            {
                sb.Append(b.ToString("X2")).Append(' ');
            }
            Console.WriteLine($"Message sent: {sb}");
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
