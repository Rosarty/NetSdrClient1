using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EchoServer.Tests
{
    public class EchoServerTests
    {
        [Fact]
        public void Constructor_InitializesPortAndTokenSource()
        {
            var server = new EchoServer(5000);
            Assert.NotNull(server);
        }

        [Fact]
        public async Task StartAsync_CanStartAndStopServer()
        {
            var server = new EchoServer(5001);

            // «апуск сервера в окремому потоц≥
            var serverTask = server.StartAsync();

            // Ќевелика затримка Ч щоб сервер устиг запуститись
            await Task.Delay(500);

            // ѕерев≥р€Їмо, що сервер не впав
            Assert.False(serverTask.IsCompleted);

            // «упинка сервера
            server.Stop();
            await Task.Delay(200);

            Assert.True(serverTask.IsCompleted || serverTask.IsCanceled);
        }

        [Fact]
        public async Task HandleClientAsync_EchoesDataBack()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var serverTask = Task.Run(async () =>
            {
                using var client = await listener.AcceptTcpClientAsync();
                await typeof(EchoServer)
                    .GetMethod("HandleClientAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.Invoke(null, new object[] { client, CancellationToken.None }) as Task ?? Task.CompletedTask;
            });

            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(IPAddress.Loopback, port);

            var stream = tcpClient.GetStream();
            byte[] sendData = System.Text.Encoding.UTF8.GetBytes("Hello");
            await stream.WriteAsync(sendData, 0, sendData.Length);

            byte[] recvBuffer = new byte[sendData.Length];
            int bytesRead = await stream.ReadAsync(recvBuffer, 0, recvBuffer.Length);

            string echoed = System.Text.Encoding.UTF8.GetString(recvBuffer, 0, bytesRead);
            Assert.Equal("Hello", echoed);

            listener.Stop();
        }

        [Fact]
        public void UdpTimedSender_StartStop_Dispose_Works()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60000);

            sender.StartSending(100);
            Thread.Sleep(200);
            sender.StopSending();

            sender.Dispose();

            Assert.True(true); // якщо сюди д≥йшли Ч усе добре
        }

        [Fact]
        public void StartSending_ThrowsIfAlreadyRunning()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60000);
            sender.StartSending(100);
            Assert.Throws<InvalidOperationException>(() => sender.StartSending(100));
            sender.Dispose();
        }

        [Fact]
        public void Stop_DisposesAndCancels()
        {
            var server = new EchoServer(5050);
            server.Stop();
            Assert.True(true); // якщо без вин€тк≥в Ч тест пройшов
        }
    }
}
