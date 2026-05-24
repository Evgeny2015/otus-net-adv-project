using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;

namespace App;

class Program
{
    // Static ActivitySource and Meter for the application
    public static readonly ActivitySource ActivitySource = new("GeospatialDataStore.Server");
    public static readonly Meter Meter = new("GeospatialDataStore.Server");

    static async Task Main(string[] args)
    {
        // Configure OpenTelemetry with console exporter
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(ActivitySource.Name)
            .AddConsoleExporter()
            .Build();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(Meter.Name)
            .AddConsoleExporter()
            .Build();

        Console.WriteLine("Starting TCP Server...");

        // Create TCP server instance with default settings (127.0.0.1:8080)
        var server = new TcpServer.TcpServer(IPAddress.Loopback, 8080, activitySource: ActivitySource, meter: Meter);

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