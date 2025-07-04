# LiveWordXml Build Script
# Choose build type: 1=Small(6.8MB), 2=Standalone(78MB)

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("small", "standalone")]
    [string]$Type = "small"
)

Write-Host "Building LiveWordXml..." -ForegroundColor Green

if ($Type -eq "small") {
    Write-Host "Building small version (6.8MB, requires .NET 8)" -ForegroundColor Yellow
    Set-Location "src\LiveWordXml.Wpf"
    dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
    Write-Host "Small version built: bin\Release\net8.0-windows\win-x64\publish\LiveWordXml.Wpf.exe" -ForegroundColor Green
    Write-Host "Requires .NET 8 Desktop Runtime on target machine" -ForegroundColor Yellow
}
else {
    Write-Host "Building standalone version (78MB, no .NET required)" -ForegroundColor Yellow
    Set-Location "src\LiveWordXml.Wpf"
    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
    Write-Host "Standalone version built: bin\Release\net8.0-windows\win-x64\publish\LiveWordXml.Wpf.exe" -ForegroundColor Green
    Write-Host "No .NET runtime required on target machine" -ForegroundColor Green
}

Write-Host "Build completed!" -ForegroundColor Green