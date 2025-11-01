using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class TcpClientWrapper : ITcpClient
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        public bool Connected => _tcpClient?.Connected == true && _stream != null;
        public event EventHandler<byte[]>? MessageReceived;

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
                _cts = new CancellationTokenSource();
                _tcpClient.Connect(_host, _port);
                _stream = _tcpClient.GetStream();

                Console.WriteLine($"Connected to {_host}:{_port}");
                _ = StartListeningAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
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
            _stream?.Close();
            _tcpClient?.Close();

            _cts = null;
            _tcpClient = null;
            _stream = null;

            Console.WriteLine("Disconnected.");
        }

        public async Task SendMessageAsync(byte[] data)
        {
            if (!Connected || _stream == null || !_stream.CanWrite)
                throw new InvalidOperationException("Not connected to a server.");

            string hexData = string.Join(" ", data.Select(b => b.ToString("X2")));
            Console.WriteLine($"Message sent: {hexData}");

            await _stream.WriteAsync(data, 0, data.Length);
        }

        public Task SendMessageAsync(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            return SendMessageAsync(data);
        }

        private async Task StartListeningAsync()
        {
            if (!Connected || _stream == null || !_stream.CanRead)
                throw new InvalidOperationException("Not connected to a server.");

            Console.WriteLine("Starting listening for incoming messages.");

            try
            {
                while (!_cts!.Token.IsCancellationRequested)
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);

                    if (bytesRead > 0)
                        MessageReceived?.Invoke(this, buffer.AsSpan(0, bytesRead).ToArray());
                }
            }
            catch (OperationCanceledException)
            {
                // очікуване завершення
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in listening loop: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Listener stopped.");
            }
        }
    }
}
