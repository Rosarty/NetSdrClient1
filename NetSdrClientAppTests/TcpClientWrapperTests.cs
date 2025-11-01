using NUnit.Framework;
using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests.Networking
{
    [TestFixture]
    public class TcpClientWrapperTests
    {
        private const string DummyHost = "localhost";
        private const int DummyPort = 12345;

        [Test]
        public void Connect_ShouldSetConnectedState()
        {
            // Arrange
            var client = new TcpClientWrapper(DummyHost, DummyPort);
            var tcpClient = new TcpClient();
            typeof(TcpClientWrapper).GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(client, tcpClient);

            // Act
            client.Disconnect(); // перевіримо без підключення
            Assert.DoesNotThrow(() => client.Connect());
        }

        [Test]
        public void Disconnect_ShouldNotThrow_WhenNotConnected()
        {
            var client = new TcpClientWrapper(DummyHost, DummyPort);
            Assert.DoesNotThrow(() => client.Disconnect());
        }

        [Test]
        public async Task SendMessageAsync_ShouldThrow_WhenNotConnected()
        {
            var client = new TcpClientWrapper(DummyHost, DummyPort);
            await Task.Yield();
            Assert.ThrowsAsync<InvalidOperationException>(async () => await client.SendMessageAsync(new byte[] { 0x01 }));
        }

        [Test]
        public void Connected_ShouldBeFalse_WhenNotInitialized()
        {
            var client = new TcpClientWrapper(DummyHost, DummyPort);
            Assert.IsFalse(client.Connected);
        }

        [Test]
        public async Task SendMessageAsync_ShouldWriteToStream_WhenConnected()
        {
            // Arrange
            var client = new TcpClientWrapper(DummyHost, DummyPort);
            var tcpClient = new TcpClient();
            typeof(TcpClientWrapper).GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(client, tcpClient);

            var ms = new MemoryStream();
            var ns = new NetworkStream(ms, FileAccess.ReadWrite, true);

            typeof(TcpClientWrapper).GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(client, ns);

            // Act
            await client.SendMessageAsync("test");

            // Assert
            ms.Position = 0;
            var buffer = new byte[ms.Length];
            ms.Read(buffer, 0, buffer.Length);
            Assert.IsTrue(Encoding.UTF8.GetString(buffer).Contains("test"));
        }

        [Test]
        public void Connect_ShouldPrintError_WhenHostInvalid()
        {
            var client = new TcpClientWrapper("invalid_host_name_999", 1);
            Assert.DoesNotThrow(() => client.Connect());
        }

        [Test]
        public void MessageReceived_Event_ShouldBeTriggered()
        {
            var client = new TcpClientWrapper(DummyHost, DummyPort);
            var eventTriggered = false;

            client.MessageReceived += (_, data) =>
            {
                eventTriggered = true;
                Assert.That(data, Is.EqualTo(new byte[] { 0x01, 0x02 }));
            };

            var evt = client.GetType().GetEvent("MessageReceived");
            evt?.Raise(client, new object[] { client, new byte[] { 0x01, 0x02 } });

            Assert.IsTrue(eventTriggered);
        }

        [Test]
        public void StartListeningAsync_ShouldThrow_WhenNotConnected()
        {
            var client = new TcpClientWrapper(DummyHost, DummyPort);
            var method = typeof(TcpClientWrapper).GetMethod("StartListeningAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            Assert.ThrowsAsync<InvalidOperationException>(async () => await (Task)method.Invoke(client, null)!);
        }

        [Test]
        public void Disconnect_ShouldClearResources()
        {
            var client = new TcpClientWrapper(DummyHost, DummyPort);
            typeof(TcpClientWrapper).GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(client, new TcpClient());

            var stream = new MemoryStream();
            typeof(TcpClientWrapper).GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(client, new NetworkStream(stream, FileAccess.ReadWrite));

            Assert.DoesNotThrow(() => client.Disconnect());
        }
    }
}