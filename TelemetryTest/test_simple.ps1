# Simple test script for OpenTelemetry
Write-Host "Testing TCP Server with OpenTelemetry..." -ForegroundColor Green

# Function to send command via TCP
function Send-TcpCommand {
    param(
        [string]$Command,
        [string]$Server = "127.0.0.1",
        [int]$Port = 8080
    )

    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $client.Connect($Server, $Port)
        $stream = $client.GetStream()
        $writer = New-Object System.IO.StreamWriter($stream)
        $reader = New-Object System.IO.StreamReader($stream)

        $writer.WriteLine($Command)
        $writer.Flush()

        $response = $reader.ReadLine()

        $writer.Close()
        $reader.Close()
        $client.Close()

        return $response
    }
    catch {
        return "ERROR: $_"
    }
}

# Test commands
$commands = @(
    "SET test1 value1",
    "GET test1",
    "SET test2 value2",
    "GET test2",
    "DELETE test1",
    "COUNT",
    "INVALID"
)

foreach ($cmd in $commands) {
    Write-Host "Sending: $cmd" -ForegroundColor Cyan
    $response = Send-TcpCommand -Command $cmd
    Write-Host "Response: $response" -ForegroundColor Gray
    Start-Sleep -Milliseconds 300
}

Write-Host "`nTest completed. Check server console for OpenTelemetry traces and metrics." -ForegroundColor Green