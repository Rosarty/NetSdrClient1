using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public interface ILogger
{
    void Log(string message);
}

public class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine(message);
}

public class EchoServer
{
    private readonly int _port;
    private readonly ILogger _logger;
    private TcpListener? _listener;
    private readonly CancellationTokenSource _cts = new();

    public EchoServer(int port, ILogger? logger = null)
    {
        _port = port;
        _logger = logger ?? new ConsoleLogger();
    }

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _logger.Log($"Server started on port {_port}.");

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _logger.Log("Client connected.");
                _ = Task.Run(() => HandleClientAsync(client, _cts.Token));
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.Log("Server shutdown.");
    }

    internal async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using var stream = client.GetStream();
        byte[] buffer = new byte[8192];
        int bytesRead;

        while (!token.IsCancellationRequested &&
               (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
        {
            await stream.WriteAsync(buffer, 0, bytesRead, token);
            _logger.Log($"Echoed {bytesRead} bytes.");
        }

        _logger.Log("Client disconnected.");
    }

    public void Stop()
    {
        _cts.Cancel();
        _listener?.Stop();
        _logger.Log("Server stopped.");
    }
}
