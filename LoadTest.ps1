# PowerShell script to run load test for TCP server
# This script builds the solution, starts the TCP server, runs the load test, and stops the server.
# Run load test powershell -ExecutionPolicy Bypass -File LoadTest.ps1


$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspaceRoot = $scriptPath

Write-Host "=== Load Test Runner ===" -ForegroundColor Cyan
Write-Host "Workspace: $workspaceRoot"

# Step 1: Build the solution in Debug (since Release may not be built)
Write-Host "`n1. Building solution (Debug configuration)..." -ForegroundColor Yellow
$solutionPath = Join-Path $workspaceRoot "GeospatialDataStore.slnx"
if (Test-Path $solutionPath) {
    dotnet build $solutionPath --configuration Debug --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed. Exiting." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Solution file not found, building LoadTest project directly..." -ForegroundColor Yellow
    $loadTestProject = Join-Path $workspaceRoot "LoadTest\LoadTest.csproj"
    dotnet build $loadTestProject --configuration Debug --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed. Exiting." -ForegroundColor Red
        exit 1
    }
}

# Step 2: Start TCP server in background using Start-Process
Write-Host "`n2. Starting TCP server on 127.0.0.1:8080..." -ForegroundColor Yellow
$serverProject = Join-Path $workspaceRoot "Service\App\App.csproj"
$serverProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "`"$serverProject`"", "--configuration", "Debug", "--no-build" -PassThru -NoNewWindow
$serverProcessId = $serverProcess.Id
Write-Host "TCP server started with PID: $serverProcessId" -ForegroundColor Gray

# Wait a few seconds for server to start
Write-Host "Waiting 5 seconds for server to start..." -ForegroundColor Gray
Start-Sleep -Seconds 5

# Verify server is listening (optional)
$tcpTest = Test-NetConnection -ComputerName 127.0.0.1 -Port 8080 -WarningAction SilentlyContinue -ErrorAction SilentlyContinue
if ($tcpTest.TcpTestSucceeded) {
    Write-Host "TCP server is listening on port 8080." -ForegroundColor Green
} else {
    Write-Host "Warning: TCP server may not be ready. Continuing anyway." -ForegroundColor Yellow
}

# Step 3: Run load test
Write-Host "`n3. Running load test..." -ForegroundColor Yellow
$loadTestProject = Join-Path $workspaceRoot "LoadTest\LoadTest.csproj"
$loadTestOutput = dotnet run --project $loadTestProject --configuration Debug --no-build 2>&1
$loadTestExitCode = $LASTEXITCODE

Write-Host "`n=== Load Test Output ===" -ForegroundColor Cyan
$loadTestOutput

# Step 4: Stop TCP server
Write-Host "`n4. Stopping TCP server (PID: $serverProcessId)..." -ForegroundColor Yellow
Stop-Process -Id $serverProcessId -Force -ErrorAction SilentlyContinue
Write-Host "TCP server stopped." -ForegroundColor Gray

# Step 5: Report results
Write-Host "`n=== Load Test Completed ===" -ForegroundColor Cyan
if ($loadTestExitCode -eq 0) {
    Write-Host "Load test finished successfully." -ForegroundColor Green
} else {
    Write-Host "Load test finished with errors (exit code: $loadTestExitCode)." -ForegroundColor Red
}

# Save output to file
$outputFile = Join-Path $workspaceRoot "loadtest_results.txt"
$loadTestOutput | Out-File -FilePath $outputFile -Encoding UTF8
Write-Host "Full output saved to: $outputFile" -ForegroundColor Gray

exit $loadTestExitCode