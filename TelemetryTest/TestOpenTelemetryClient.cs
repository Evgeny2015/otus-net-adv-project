using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class TestOpenTelemetryClient
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing OpenTelemetry with TCP Server...");

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", 8080);
            var stream = client.GetStream();

            // Test commands to generate telemetry
            string[] commands = {
                "SET key1 value1",
                "GET key1",
                "SET key2 value2",
                "GET key2",
                "DELETE key1",
                "COUNT",
                "INVALID_COMMAND"
            };

            foreach (var cmd in commands)
            {
                Console.WriteLine($"Sending: {cmd}");
                var data = Encoding.UTF8.GetBytes(cmd + "\n");
                await stream.WriteAsync(data);

                // Read response
                var buffer = new byte[1024];
                var bytesRead = await stream.ReadAsync(buffer);
                var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Response: {response.Trim()}");

                await Task.Delay(100); // Small delay between commands
            }

            Console.WriteLine("Test completed. Check server console for OpenTelemetry output.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}