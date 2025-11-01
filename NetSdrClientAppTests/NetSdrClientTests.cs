using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class NetSdrClientTests
    {
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _udpMock;
        private NetSdrClient _client;

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _udpMock = new Mock<IUdpClient>();

            _tcpMock.SetupGet(c => c.Connected).Returns(true);
            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
        }

        [Test]
        public async Task ConnectAsync_ShouldSendInitializationMessages()
        {
            // Arrange
            _tcpMock.SetupGet(c => c.Connected).Returns(false);
            _tcpMock.Setup(c => c.Connect());
            _tcpMock.Setup(c => c.SendMessageAsync(It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask);

            // Act
            await _client.ConnectAsync();

            // Assert
            _tcpMock.Verify(c => c.Connect(), Times.Once);
            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public void Disconnect_ShouldInvokeTcpDisconnect()
        {
            // Arrange
            _tcpMock.Setup(c => c.Disconnect());

            // Act
            _client.Disconect();

            // Assert
            _tcpMock.Verify(c => c.Disconnect(), Times.Once);
        }

        [Test]
        public async Task StartIQAsync_ShouldSendStartMessage_AndStartUdpListening()
        {
            // Arrange
            _tcpMock.Setup(c => c.SendMessageAsync(It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask);
            _udpMock.Setup(c => c.StartListeningAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _client.StartIQAsync();

            // Assert
            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
            _udpMock.Verify(c => c.StartListeningAsync(), Times.Once);
            Assert.IsTrue(_client.IQStarted);
        }

        [Test]
        public async Task StopIQAsync_ShouldSendStopMessage_AndStopUdpListening()
        {
            // Arrange
            _tcpMock.Setup(c => c.SendMessageAsync(It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask);
            _udpMock.Setup(c => c.StopListening());

            // Act
            await _client.StopIQAsync();

            // Assert
            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
            _udpMock.Verify(c => c.StopListening(), Times.Once);
            Assert.IsFalse(_client.IQStarted);
        }

        [Test]
        public async Task ChangeFrequencyAsync_ShouldSendFrequencyMessage()
        {
            // Arrange
            _tcpMock.Setup(c => c.SendMessageAsync(It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask);

            // Act
            await _client.ChangeFrequencyAsync(144000000, 1);

            // Assert
            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
        }

        [Test]
        public void TcpClient_MessageReceived_ShouldResolveResponseTask()
        {
            // Arrange
            var bytes = new byte[] { 0x01, 0x02, 0x03 };
            var tcs = new TaskCompletionSource<byte[]>();
            var client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
            typeof(NetSdrClient)
                .GetField("responseTaskSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(client, tcs);

            // Act
            var method = typeof(NetSdrClient).GetMethod("_tcpClient_MessageReceived",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method!.Invoke(client, new object?[] { null, bytes });

            // Assert
            Assert.IsTrue(tcs.Task.IsCompleted);
            Assert.AreEqual(bytes, tcs.Task.Result);
        }

        [Test]
        public void UdpClient_MessageReceived_ShouldWriteSamplesFile()
        {
            // Arrange
            var body = new byte[] { 0x00, 0x01, 0x00, 0x02 };
            var msg = NetSdrMessageHelper.GetControlItemMessage(
                MsgTypes.GetControlItem, ControlItemCodes.IQOutputDataSampleRate, body);

            var method = typeof(NetSdrClient).GetMethod("_udpClient_MessageReceived",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (File.Exists("samples.bin"))
                File.Delete("samples.bin");

            // Act
            method!.Invoke(_client, new object?[] { null, msg });

            // Assert
            Assert.IsTrue(File.Exists("samples.bin"));
            Assert.Greater(new FileInfo("samples.bin").Length, 0);

            File.Delete("samples.bin");
        }
    }
}

