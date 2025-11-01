using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class FakeLogger : ILogger
{
    public string Logs = "";
    public void Log(string message) => Logs += message + "\n";
}

public class FakeTcpClient : TcpClient
{
    private readonly MemoryStream _input;
    private readonly MemoryStream _output;

    public FakeTcpClient(string inputMessage)
    {
        _input = new MemoryStream(Encoding.UTF8.GetBytes(inputMessage));
        _output = new MemoryStream();
    }

    public override NetworkStream GetStream()
    {
        return new FakeNetworkStream(_input, _output);
    }

    public string GetOutput() => Encoding.UTF8.GetString(_output.ToArray());
}

public class FakeNetworkStream : NetworkStream
{
    private readonly Stream _input;
    private readonly Stream _output;

    public FakeNetworkStream(Stream input, Stream output)
        : base(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
    {
        _input = input;
        _output = output;
    }

    public override bool CanRead => _input.CanRead;
    public override bool CanWrite => _output.CanWrite;

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken token)
        => await _input.ReadAsync(buffer.AsMemory(offset, size), token);

    public override async Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken token)
        => await _output.WriteAsync(buffer.AsMemory(offset, size), token);
}

public class EchoServerTests
{
    [Fact]
    public async Task HandleClientAsync_ShouldEchoMessage()
    {
        // Arrange
        var logger = new FakeLogger();
        var server = new EchoServer(5000, logger);
        var client = new FakeTcpClient("Hello Test");

        // Act
        await server.HandleClientAsync(client, CancellationToken.None);

        // Assert
        var response = client.GetOutput();
        Assert.Equal("Hello Test", response);
        Assert.Contains("Echoed", logger.Logs);
    }
}
