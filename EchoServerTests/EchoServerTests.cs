using EchoTspServer.Application.Interfaces;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EchoTspServer.Application.Services
{
    public class EchoServer
    {
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly IClientHandler _clientHandler;

        private CancellationTokenSource _cts;
        private TcpListener _listener;

        public EchoServer(int port, ILogger logger, IClientHandler clientHandler)
        {
            _port = port;
            _logger = logger;
            _clientHandler = clientHandler;
        }

        public async Task StartAsync()
        {
            // 👇 Ця частина часто лишається непокритою — тепер буде
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            _logger.Info($"Server started on port {_port}.");

            try
            {
                // Головний цикл прийому клієнтів
                while (!_cts.Token.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _logger.Info("Client connected.");

                    // Обробляємо клієнта у фоновому таску
                    _ = Task.Run(() => _clientHandler.HandleClientAsync(client, _cts.Token));
                }
            }
            catch (ObjectDisposedException)
            {
                // Нормальна ситуація при зупинці
                _logger.Info("Listener closed normally.");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
            {
                _logger.Info("Listener stopped by cancellation.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected server error: {ex.Message}");
            }
            finally
            {
                _logger.Info("Server shutdown.");
            }
        }

        public void Stop()
        {
            if (_cts == null)
                return;

            try
            {
                _cts.Cancel();
                _listener?.Stop();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during stop: {ex.Message}");
            }
        }
    }
}
