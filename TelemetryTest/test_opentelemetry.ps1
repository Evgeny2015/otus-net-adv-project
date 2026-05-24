# Test script for OpenTelemetry verification
Write-Host "Starting TCP Server with OpenTelemetry..." -ForegroundColor Green

# Start the server in background
$serverProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project Service/App/App.csproj" -PassThru -NoNewWindow

# Wait for server to start
Start-Sleep -Seconds 3

Write-Host "Server started. Testing with simple commands..." -ForegroundColor Yellow

# Test commands
$commands = @(
    "SET test_key test_value",
    "GET test_key",
    "DELETE test_key",
    "COUNT",
    "INVALID_COMMAND"
)

foreach ($cmd in $commands) {
    Write-Host "Sending: $cmd" -ForegroundColor Cyan
    $response = echo $cmd | nc -w 2 127.0.0.1 8080
    Write-Host "Response: $response" -ForegroundColor Gray
    Start-Sleep -Milliseconds 500
}

Write-Host "`nOpenTelemetry metrics and traces should be visible in server console output above." -ForegroundColor Green
Write-Host "Press Enter to stop server..." -ForegroundColor Yellow
Read-Host

# Stop server
Stop-Process -Id $serverProcess.Id -Force
Write-Host "Server stopped." -ForegroundColor Red