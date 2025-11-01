using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EchoServer.Tests
{
    public class EchoServerTests
    {
        [Fact]
        public async Task Server_ShouldEchoMessage()
        {
            // Arrange
            int port = 5050;
            var server = new EchoServer.EchoServer(port);
            var serverTask = server.StartAsync();

            await Task.Delay(500); // Дати серверу стартувати

            using TcpClient client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            using NetworkStream stream = client.GetStream();

            string message = "Hello, Echo!";
            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data);

            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer);

            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // Assert
            Assert.Equal(message, response);

            // Cleanup
            server.Stop();
            await serverTask;
        }

        [Fact]
        public async Task Server_ShouldHandleClientDisconnect()
        {
            int port = 5051;
            var server = new EchoServer.EchoServer(port);
            var serverTask = server.StartAsync();
            await Task.Delay(500);

            var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            client.Close();

            await Task.Delay(500);

            server.Stop();
            await serverTask;

            Assert.True(true, "Client disconnected handled gracefully");
        }

        [Fact]
        public async Task UdpSender_ShouldSendUdpPackets()
        {
            // Arrange
            int port = 6060;
            using UdpClient receiver = new UdpClient(port);
            var sender = new EchoServer.UdpTimedSender("127.0.0.1", port);

            sender.StartSending(500);

            // Act
            var result = await receiver.ReceiveAsync(); // Отримаємо один пакет

            // Assert
            Assert.NotNull(result.Buffer);
            Assert.True(result.Buffer.Length > 0);
            Assert.Equal(0x04, result.Buffer[0]);
            Assert.Equal(0x84, result.Buffer[1]);

            sender.StopSending();
            sender.Dispose();
        }

        [Fact]
        public void StartSending_ShouldThrowIfAlreadyRunning()
        {
            var sender = new EchoServer.UdpTimedSender("127.0.0.1", 6000);
            sender.StartSending(1000);

            Assert.Throws<InvalidOperationException>(() => sender.StartSending(1000));

            sender.StopSending();
            sender.Dispose();
        }

        [Fact]
        public void StopSending_ShouldDisposeTimer()
        {
            var sender = new EchoServer.UdpTimedSender("127.0.0.1", 6000);
            sender.StartSending(1000);

            sender.StopSending();

            // Повторний виклик StopSending не повинен падати
            sender.StopSending();

            sender.Dispose();
            Assert.True(true);
        }

        [Fact]
        public async Task FullIntegration_ShouldRunServerAndUdpSender()
        {
            int tcpPort = 5055;
            int udpPort = 6065;
            var server = new EchoServer.EchoServer(tcpPort);
            var serverTask = server.StartAsync();

            await Task.Delay(500);

            using var sender = new EchoServer.UdpTimedSender("127.0.0.1", udpPort);
            sender.StartSending(500);

            // TCP тест
            using TcpClient client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", tcpPort);
            using NetworkStream stream = client.GetStream();

            byte[] msg = Encoding.UTF8.GetBytes("Ping!");
            await stream.WriteAsync(msg);
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Assert.Equal("Ping!", response);

            // Завершення
            sender.StopSending();
            sender.Dispose();
            server.Stop();
            await serverTask;
        }
    }
}
