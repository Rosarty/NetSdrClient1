using EchoTspServer.Application.Interfaces;
using EchoTspServer.Application.Services;
using Moq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EchoTspServer.Tests
{
public class EchoServerTests
{
private const int TestPort = 5000;

```
    [Fact]
    public async Task StartAsync_ClientConnects_ClientHandlerCalled()
    {
        var loggerMock = new Mock<ILogger>();
        var clientHandlerMock = new Mock<IClientHandler>();
        var server = new EchoServer(TestPort, loggerMock.Object, clientHandlerMock.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(3000); // автоматична зупинка тесту через 3 секунди

        var serverTask = server.StartAsync();

        // Підключаємо тестового клієнта
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", TestPort);

        await Task.Delay(500); // чекаємо обробку підключення

        server.Stop();
        await serverTask;

        clientHandlerMock.Verify(h => h.HandleClientAsync(It.IsAny<TcpClient>(), It.IsAny<CancellationToken>()), 
                                  Times.AtLeastOnce);
        loggerMock.Verify(l => l.Info(It.Is<string>(s => s.Contains("Client connected"))), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Stop_ServerAlreadyStopped_NoException()
    {
        var loggerMock = new Mock<ILogger>();
        var clientHandlerMock = new Mock<IClientHandler>();
        var server = new EchoServer(TestPort, loggerMock.Object, clientHandlerMock.Object);

        server.Stop(); // перший виклик
        var exception = await Record.ExceptionAsync(() => Task.Run(() => server.Stop())); // другий виклик

        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_ListenerThrowsObjectDisposedException_ServerStopsGracefully()
    {
        var loggerMock = new Mock<ILogger>();
        var clientHandlerMock = new Mock<IClientHandler>();
        var server = new EchoServer(TestPort, loggerMock.Object, clientHandlerMock.Object);

        var serverTask = server.StartAsync();

        server.Stop();
        await serverTask;

        loggerMock.Verify(l => l.Info(It.Is<string>(s => s.Contains("Server shutdown"))), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ClientConnects_HandlerAndLoggerCalled()
    {
        var clientHandlerMock = new Mock<IClientHandler>();
        var loggerMock = new Mock<ILogger>();
        var server = new EchoServer(6002, loggerMock.Object, clientHandlerMock.Object);
        var serverTask = server.StartAsync();

        using (var client = new TcpClient())
        {
            await client.ConnectAsync("127.0.0.1", 6002);
            await Task.Delay(200);
        }

        server.Stop();
        await serverTask;

        clientHandlerMock.Verify(h => h.HandleClientAsync(It.IsAny<TcpClient>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        loggerMock.Verify(l => l.Info(It.Is<string>(s => s.Contains("Client connected"))), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_StopsGracefully_ObjectDisposedExceptionHandled()
    {
        var loggerMock = new Mock<ILogger>();
        var clientHandlerMock = new Mock<IClientHandler>();
        var server = new EchoServer(6003, loggerMock.Object, clientHandlerMock.Object);

        var serverTask = server.StartAsync();
        await Task.Delay(100);

        server.Stop();
        await serverTask;

        loggerMock.Verify(l => l.Info(It.Is<string>(s => s.Contains("Server shutdown"))), Times.Once);
    }
}
```

}

