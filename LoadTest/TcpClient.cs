using System.Net.Sockets;
using System.Text;
using System.IO;
using DataStore;

namespace LoadTest;

public class TcpClient : IDisposable
{
    private Socket? _socket;
    private readonly string _host;
    private readonly int _port;

    public TcpClient(string host = "127.0.0.1", int port = 8080)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_socket != null && _socket.Connected)
            return;

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await _socket.ConnectAsync(_host, _port, cancellationToken);
    }

    public async Task<string> SetAsync(string key, byte[] value, CancellationToken cancellationToken = default)
    {
        if (_socket == null || !_socket.Connected)
            throw new InvalidOperationException("Client is not connected");

        // Format: SET <key> <value as base64?>
        // For simplicity, we'll send the bytes as base64 string
        string base64Value = Convert.ToBase64String(value);
        string command = $"SET {key} {base64Value}";

        byte[] buffer = Encoding.UTF8.GetBytes(command);
        await _socket.SendAsync(buffer, SocketFlags.None, cancellationToken);

        // Read response from server
        var responseBuffer = new byte[4096];
        var received = await _socket.ReceiveAsync(responseBuffer, SocketFlags.None, cancellationToken);
        string response = Encoding.UTF8.GetString(responseBuffer, 0, received);
        return response.TrimEnd('\n');
    }

    public async Task<string> SetAsync(string key, MovingObject value, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        value.SerializeToBinary(memoryStream);
        var bytes = memoryStream.ToArray();
        return await SetAsync(key, bytes, cancellationToken);
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_socket == null || !_socket.Connected)
            throw new InvalidOperationException("Client is not connected");

        // Send GET command
        string command = $"GET {key}";
        byte[] buffer = Encoding.UTF8.GetBytes(command);
        await _socket.SendAsync(buffer, SocketFlags.None, cancellationToken);

        // The server currently doesn't send a response, but we'll read anyway
        // For now, just return null as placeholder
        return null;
    }

    public void Dispose()
    {
        _socket?.Close();
        _socket?.Dispose();
        _socket = null;
    }
}