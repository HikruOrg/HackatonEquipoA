# Lead Research Agent Setup Script
# Run this in PowerShell to set up the project

Write-Host "🚀 Setting up Lead Research Agent - Hikru Hackathon Team A" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green

# Check if .NET 8 is installed
Write-Host "`n📋 Checking prerequisites..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET SDK not found. Please install .NET 8.0 SDK" -ForegroundColor Red
    Write-Host "   Download from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# Restore NuGet packages
Write-Host "`n📦 Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ NuGet packages restored successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to restore NuGet packages" -ForegroundColor Red
    exit 1
}

# Build the project
Write-Host "`n🔨 Building the project..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Project built successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}

# Check if sample data files exist
Write-Host "`n📄 Checking sample data files..." -ForegroundColor Yellow
$dataFiles = @("Data\icp.json", "Data\enrichment.csv", "Data\sample_newsletter.txt")
foreach ($file in $dataFiles) {
    if (Test-Path $file) {
        Write-Host "✅ Found: $file" -ForegroundColor Green
    } else {
        Write-Host "❌ Missing: $file" -ForegroundColor Red
    }
}

# Setup environment file
Write-Host "`n🔧 Setting up environment configuration..." -ForegroundColor Yellow
if (!(Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "✅ Created .env file from template" -ForegroundColor Green
    Write-Host "   Please edit .env with your Azure OpenAI credentials for full functionality" -ForegroundColor Yellow
} else {
    Write-Host "ℹ️  .env file already exists" -ForegroundColor Cyan
}

Write-Host "`n🎯 Setup Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Edit .env file with your Azure OpenAI credentials (optional)" -ForegroundColor White
Write-Host "2. Customize Data\icp.json with your target customer profile" -ForegroundColor White
Write-Host "3. Update Data\enrichment.csv with your company database" -ForegroundColor White
Write-Host "4. Run the application: dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "For demo purposes, you can run it immediately with the sample data!" -ForegroundColor Green
Write-Host ""
Write-Host "Happy hacking! 🚀" -ForegroundColor Magenta