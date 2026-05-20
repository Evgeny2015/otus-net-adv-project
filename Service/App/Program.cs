namespace App;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting TCP Server...");

        // Create TCP server instance with default settings (127.0.0.1:8080)
        var server = new TcpServer.TcpServer();

        // Create cancellation token source for graceful shutdown
        using var cts = new CancellationTokenSource();

        // Handle Ctrl+C to stop the server
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\nStopping server...");
            cts.Cancel();
            e.Cancel = true; // Prevent the process from terminating immediately
        };

        try
        {
            // Start the server in background
            var serverTask = server.StartAsync(cts.Token);

            Console.WriteLine("Server is running. Press Ctrl+C to stop.");
            Console.WriteLine("Listening on 127.0.0.1:8080");

            // Wait for user input or cancellation
            await Task.WhenAny(
                serverTask,
                Task.Run(() =>
                {
                    Console.ReadLine(); // Wait for Enter key
                    cts.Cancel();
                })
            );

            // Stop the server
            server.Stop();

            // Wait for server to stop gracefully
            await serverTask;

            Console.WriteLine("Server stopped.");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Server stopped by user request.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}