using System.Buffers;
using System.Net;
using System.Net.Sockets;
using CommandParser;

namespace TcpServer;

public class TcpServer
{
    private readonly IPAddress _ipAddress;
    private readonly int _port;
    private Socket? _serverSocket;

    public TcpServer(IPAddress ipAddress, int port)
    {
        _ipAddress = ipAddress;
        _port = port;
    }

    public TcpServer() : this(IPAddress.Loopback, 8080)
    {
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Bind to local IP address and port
        var endPoint = new IPEndPoint(_ipAddress, _port);
        _serverSocket.Bind(endPoint);

        // Start listening
        _serverSocket.Listen(backlog: 100);

        Console.WriteLine($"TCP Server started on {_ipAddress}:{_port}");

        // Infinite loop to accept connections
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var clientSocket = await _serverSocket.AcceptAsync(cancellationToken);
                Console.WriteLine($"New client connected: {clientSocket.RemoteEndPoint}");

                // Start processing client in background task
                _ = ProcessClientAsync(clientSocket, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Server is stopping
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
            }
        }
    }

    private async Task ProcessClientAsync(Socket clientSocket, CancellationToken cancellationToken)
    {
        try
        {
            using (clientSocket)
            {
                // Buffer for reading data
                var buffer = ArrayPool<byte>.Shared.Rent(4096);

                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // Read data from client
                        var receiveResult = await clientSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            SocketFlags.None,
                            cancellationToken);

                        // If 0 bytes received, client has disconnected
                        if (receiveResult == 0)
                        {
                            Console.WriteLine($"Client {clientSocket.RemoteEndPoint} disconnected");
                            break;
                        }

                        // Process received data
                        var data = new ReadOnlyMemory<byte>(buffer, 0, receiveResult);
                        await ProcessReceivedDataAsync(data, clientSocket.RemoteEndPoint?.ToString() ?? "unknown");
                    }
                }
                finally
                {
                    // Return buffer to pool
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Server is stopping
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing client {clientSocket.RemoteEndPoint}: {ex.Message}");
        }
        finally
        {
            try
            {
                // Ensure socket is closed
                if (clientSocket.Connected)
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
    }

    private async Task ProcessReceivedDataAsync(ReadOnlyMemory<byte> data, string clientInfo)
    {
        try
        {
            // Convert bytes to string (assuming UTF-8 encoding)
            var text = System.Text.Encoding.UTF8.GetString(data.Span);

            // Parse command using CommandParser
            var parsedCommand = Parser.Parse(text.AsSpan());

            // Output parsed command to console
            Console.WriteLine($"[{clientInfo}] Command: '{parsedCommand.Command}', Key: '{parsedCommand.Key}', Value: '{parsedCommand.Value}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing data from {clientInfo}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    public void Stop()
    {
        _serverSocket?.Close();
        _serverSocket?.Dispose();
        _serverSocket = null;
    }
}