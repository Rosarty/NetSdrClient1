using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NetSdrClientApp.Networking;
using Xunit;

namespace NetSdrClientApp.Tests
{
public class TcpClientWrapperTests
{
private const int TestPort = 6000;

```
    [Theory]
    [InlineData("Hello")]
    [InlineData("Another message")]
    [InlineData("")]
    public async Task SendMessageAsync_WithConnection_SendsData_Correctly(string message)
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, TestPort);
        listener.Start();
        var client = new TcpClientWrapper("localhost", TestPort);

        var connectTask = Task.Run(() => listener.AcceptTcpClientAsync());
        client.Connect();
        using var serverClient = await connectTask;

        var data = Encoding.UTF8.GetBytes(message);
        await client.SendMessageAsync(message);

        var buffer = new byte[1024];
        int bytesRead = await serverClient.GetStream().ReadAsync(buffer, 0, buffer.Length);

        Assert.Equal(data.Length, bytesRead);
        var received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Assert.Equal(message, received);

        client.Disconnect();
        listener.Stop();
    }

    [Theory]
    [InlineData("Test1")]
    [InlineData("Test2")]
    [InlineData("Another")]
    public async Task MessageReceived_EventTriggered_OnIncomingData(string message)
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, TestPort);
        listener.Start();
        var client = new TcpClientWrapper("localhost", TestPort);

        var connectTask = Task.Run(() => listener.AcceptTcpClientAsync());
        client.Connect();
        using var serverClient = await connectTask;

        byte[] receivedData = null;
        var messageReceived = new TaskCompletionSource<bool>();
        client.MessageReceived += (s, data) =>
        {
            receivedData = data;
            messageReceived.SetResult(true);
        };

        byte[] toSend = Encoding.UTF8.GetBytes(message);
        await serverClient.GetStream().WriteAsync(toSend, 0, toSend.Length);

        await messageReceived.Task;
        Assert.NotNull(receivedData);
        Assert.Equal(message, Encoding.UTF8.GetString(receivedData));

        client.Disconnect();
        listener.Stop();
    }
}
```

}
