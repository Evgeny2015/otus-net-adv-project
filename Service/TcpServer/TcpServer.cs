using System.Buffers;
using System.Net;
using System.Net.Sockets;
using CommandParser;
using DataStore;

namespace TcpServer;

public class TcpServer
{
    private readonly IPAddress _ipAddress;
    private readonly int _port;
    private Socket? _serverSocket;
    private readonly DataStore.DataStore _dataStore;
    private readonly SemaphoreSlim _connectionSemaphore;
    private const int MaxMessageSize = 4096; // 4KB limit

    public TcpServer(IPAddress ipAddress, int port, DataStore.DataStore? dataStore = null, int maxConcurrentConnections = 100)
    {
        _ipAddress = ipAddress;
        _port = port;
        _dataStore = dataStore ?? new DataStore.DataStore();
        _connectionSemaphore = new SemaphoreSlim(maxConcurrentConnections, maxConcurrentConnections);
    }

    public TcpServer() : this(IPAddress.Loopback, 8080, null)
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

                // Wait for semaphore before processing client
                await _connectionSemaphore.WaitAsync(cancellationToken);

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

                        // Check for memory exhaustion: if message exceeds 4KB limit
                        if (receiveResult > MaxMessageSize)
                        {
                            Console.WriteLine($"Client {clientSocket.RemoteEndPoint} sent {receiveResult} bytes, exceeding {MaxMessageSize} limit. Disconnecting.");
                            break; // Will close socket and release semaphore
                        }

                        // Process received data
                        var data = new ReadOnlyMemory<byte>(buffer, 0, receiveResult);
                        await ProcessReceivedDataAsync(clientSocket, data, clientSocket.RemoteEndPoint?.ToString() ?? "unknown");
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

            // Release the connection semaphore
            _connectionSemaphore.Release();
        }
    }

    private async Task ProcessReceivedDataAsync(Socket clientSocket, ReadOnlyMemory<byte> data, string clientInfo)
    {
        try
        {
            // Convert bytes to string (assuming UTF-8 encoding)
            var text = System.Text.Encoding.UTF8.GetString(data.Span);

            // Parse command using CommandParser
            var parsedCommand = Parser.Parse(text.AsSpan());

            // Output parsed command to console
            Console.WriteLine($"[{clientInfo}] Command: '{parsedCommand.Command}', Key: '{parsedCommand.Key}', Value: '{parsedCommand.Value}'");

            // Convert to strings before async call
            string command = parsedCommand.Command.ToString();
            string key = parsedCommand.Key.ToString();
            string value = parsedCommand.Value.ToString();

            // Handle command
            string response = await HandleCommandAsync(command, key, value);

            // Send response back to client
            await SendResponseAsync(clientSocket, response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing data from {clientInfo}: {ex.Message}");
            await SendResponseAsync(clientSocket, $"ERROR: {ex.Message}");
        }
    }

    private async Task<string> HandleCommandAsync(string command, string key, string value)
    {
        if (string.IsNullOrEmpty(command))
        {
            return "ERROR: Empty command";
        }

        switch (command.ToUpperInvariant())
        {
            case "SET":
                if (string.IsNullOrEmpty(key))
                    return "ERROR: SET requires a key";
                if (string.IsNullOrEmpty(value))
                    return "ERROR: SET requires a value";

                // Store as byte array (UTF-8 encoded string)
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
                _dataStore.Set(key, bytes);
                return $"OK: Stored '{key}' = '{value}'";

            case "GET":
                if (string.IsNullOrEmpty(key))
                    return "ERROR: GET requires a key";

                byte[]? result = _dataStore.Get(key);
                if (result == null)
                    return $"ERROR: Key '{key}' not found";

                string stringValue = System.Text.Encoding.UTF8.GetString(result);
                return $"OK: '{key}' = '{stringValue}'";

            case "DELETE":
                if (string.IsNullOrEmpty(key))
                    return "ERROR: DELETE requires a key";

                bool deleted = _dataStore.Delete(key);
                return deleted ? $"OK: Deleted '{key}'" : $"ERROR: Key '{key}' not found";

            case "CONTAINS":
                if (string.IsNullOrEmpty(key))
                    return "ERROR: CONTAINS requires a key";

                bool contains = _dataStore.Contains(key);
                return contains ? $"OK: Key '{key}' exists" : $"OK: Key '{key}' does not exist";

            case "COUNT":
                int count = _dataStore.Count;
                return $"OK: {count} items";

            case "CLEAR":
                _dataStore.Clear();
                return "OK: Store cleared";

            case "STATS":
                var stats = _dataStore.GetStatistics();
                return $"OK: SET={stats.setCount}, GET={stats.getCount}, DELETE={stats.deleteCount}";

            default:
                return $"ERROR: Unknown command '{command}'";
        }
    }

    private async Task SendResponseAsync(Socket clientSocket, string response)
    {
        try
        {
            byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(response + "\n");
            await clientSocket.SendAsync(responseBytes, SocketFlags.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending response to {clientSocket.RemoteEndPoint}: {ex.Message}");
        }
    }

    public void Stop()
    {
        _serverSocket?.Close();
        _serverSocket?.Dispose();
        _serverSocket = null;
    }
}