# PowerShell Setup Script for Lead Research Agent
# Run this script to configure your Azure credentials

Write-Host "🚀 Lead Research Agent - Azure Configuration Setup" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

# Function to prompt for input with validation
function Get-UserInput {
    param(
        [string]$Prompt,
        [bool]$Required = $true,
        [bool]$IsSecret = $false
    )
    
    do {
        if ($IsSecret) {
            $value = Read-Host -Prompt $Prompt -AsSecureString
            $value = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($value))
        } else {
            $value = Read-Host -Prompt $Prompt
        }
        
        if ($Required -and [string]::IsNullOrWhiteSpace($value)) {
            Write-Host "❌ This field is required!" -ForegroundColor Red
        }
    } while ($Required -and [string]::IsNullOrWhiteSpace($value))
    
    return $value
}

Write-Host "`n🔧 Azure OpenAI Configuration" -ForegroundColor Yellow
$openaiKey = Get-UserInput "Enter your Azure OpenAI API Key" -IsSecret $true
$openaiEndpoint = Get-UserInput "Enter your Azure OpenAI Endpoint (e.g., https://your-resource.openai.azure.com/)"
$openaiDeployment = Get-UserInput "Enter your deployment name (default: gpt-4)" -Required $false
if ([string]::IsNullOrWhiteSpace($openaiDeployment)) { $openaiDeployment = "gpt-4" }

Write-Host "`n📧 Microsoft Graph Configuration" -ForegroundColor Yellow
$graphClientId = Get-UserInput "Enter your Microsoft Graph Client ID"
$graphTenantId = Get-UserInput "Enter your Microsoft Graph Tenant ID"
$graphClientSecret = Get-UserInput "Enter your Client Secret (optional for delegated auth)" -Required $false -IsSecret $true

Write-Host "`n📧 Email Configuration" -ForegroundColor Yellow
$senderEmail = Get-UserInput "Enter default sender email address" -Required $false
$recipientEmail = Get-UserInput "Enter default recipient email for results" -Required $false

Write-Host "`n🤖 Azure AI Foundry Configuration (Optional)" -ForegroundColor Yellow
Write-Host "Press Enter to skip AI Foundry configuration" -ForegroundColor Gray
$aiFoundryEndpoint = Get-UserInput "Enter AI Foundry Endpoint (optional)" -Required $false
$aiFoundryKey = Get-UserInput "Enter AI Foundry API Key (optional)" -Required $false -IsSecret $true
$aiFoundryProject = Get-UserInput "Enter AI Foundry Project ID (optional)" -Required $false

# Set environment variables for current session
Write-Host "`n🔐 Setting environment variables for current session..." -ForegroundColor Yellow

$env:AZURE_OPENAI_API_KEY = $openaiKey
$env:AZURE_OPENAI_ENDPOINT = $openaiEndpoint
$env:AZURE_OPENAI_DEPLOYMENT = $openaiDeployment
$env:MICROSOFT_GRAPH_CLIENT_ID = $graphClientId
$env:MICROSOFT_GRAPH_TENANT_ID = $graphTenantId

if (-not [string]::IsNullOrWhiteSpace($graphClientSecret)) {
    $env:MICROSOFT_GRAPH_CLIENT_SECRET = $graphClientSecret
}

if (-not [string]::IsNullOrWhiteSpace($senderEmail)) {
    $env:DEFAULT_SENDER_EMAIL = $senderEmail
}

if (-not [string]::IsNullOrWhiteSpace($recipientEmail)) {
    $env:DEFAULT_RECIPIENT_EMAIL = $recipientEmail
}

if (-not [string]::IsNullOrWhiteSpace($aiFoundryEndpoint)) {
    $env:AZURE_AI_FOUNDRY_ENDPOINT = $aiFoundryEndpoint
    $env:AZURE_AI_FOUNDRY_API_KEY = $aiFoundryKey
    $env:AZURE_AI_FOUNDRY_PROJECT_ID = $aiFoundryProject
}

# Ask if user wants to save permanently
Write-Host "`n💾 Do you want to save these settings permanently to user environment variables?" -ForegroundColor Yellow
$saveChoice = Read-Host "Enter Y to save permanently, N to keep for current session only (Y/N)"

if ($saveChoice -eq "Y" -or $saveChoice -eq "y") {
    Write-Host "Setting permanent environment variables..." -ForegroundColor Yellow
    
    [Environment]::SetEnvironmentVariable("AZURE_OPENAI_API_KEY", $openaiKey, "User")
    [Environment]::SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", $openaiEndpoint, "User")
    [Environment]::SetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT", $openaiDeployment, "User")
    [Environment]::SetEnvironmentVariable("MICROSOFT_GRAPH_CLIENT_ID", $graphClientId, "User")
    [Environment]::SetEnvironmentVariable("MICROSOFT_GRAPH_TENANT_ID", $graphTenantId, "User")
    
    if (-not [string]::IsNullOrWhiteSpace($graphClientSecret)) {
        [Environment]::SetEnvironmentVariable("MICROSOFT_GRAPH_CLIENT_SECRET", $graphClientSecret, "User")
    }
    
    if (-not [string]::IsNullOrWhiteSpace($senderEmail)) {
        [Environment]::SetEnvironmentVariable("DEFAULT_SENDER_EMAIL", $senderEmail, "User")
    }
    
    if (-not [string]::IsNullOrWhiteSpace($recipientEmail)) {
        [Environment]::SetEnvironmentVariable("DEFAULT_RECIPIENT_EMAIL", $recipientEmail, "User")
    }
    
    if (-not [string]::IsNullOrWhiteSpace($aiFoundryEndpoint)) {
        [Environment]::SetEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT", $aiFoundryEndpoint, "User")
        [Environment]::SetEnvironmentVariable("AZURE_AI_FOUNDRY_API_KEY", $aiFoundryKey, "User")
        [Environment]::SetEnvironmentVariable("AZURE_AI_FOUNDRY_PROJECT_ID", $aiFoundryProject, "User")
    }
    
    Write-Host "✅ Environment variables saved permanently!" -ForegroundColor Green
    Write-Host "⚠️  You may need to restart your terminal/IDE to see the changes." -ForegroundColor Yellow
}

# Create appsettings.json file
Write-Host "`n📄 Creating appsettings.json configuration file..." -ForegroundColor Yellow

$appsettingsContent = @{
    Azure = @{
        KeyVault = @{
            Url = ""
        }
        OpenAI = @{
            ApiKey = ""
            Endpoint = ""
            Deployment = "gpt-4"
        }
        Graph = @{
            ClientId = ""
            TenantId = ""
            ClientSecret = ""
        }
        AIFoundry = @{
            Endpoint = ""
            ApiKey = ""
            ProjectId = ""
        }
    }
    Email = @{
        DefaultSender = ""
        DefaultRecipient = ""
    }
    Logging = @{
        LogLevel = @{
            Default = "Information"
            Microsoft = "Warning"
            "Microsoft.Hosting.Lifetime" = "Information"
        }
    }
}

$appsettingsJson = $appsettingsContent | ConvertTo-Json -Depth 4
$appsettingsJson | Out-File -FilePath "appsettings.json" -Encoding UTF8

Write-Host "✅ Configuration completed!" -ForegroundColor Green
Write-Host "`n🚀 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Run 'dotnet build' to build the project" -ForegroundColor White
Write-Host "2. Run 'dotnet run' to start the Lead Research Agent" -ForegroundColor White
Write-Host "3. Choose option 1 to process Outlook emails or option 2 for demo mode" -ForegroundColor White

if (-not [string]::IsNullOrWhiteSpace($recipientEmail)) {
    Write-Host "`n📧 Email Integration Enabled:" -ForegroundColor Green
    Write-Host "Results will be automatically sent to: $recipientEmail" -ForegroundColor White
}

if (-not [string]::IsNullOrWhiteSpace($aiFoundryEndpoint)) {
    Write-Host "`n🤖 AI Foundry Integration Enabled:" -ForegroundColor Green
    Write-Host "Enhanced AI features available for scoring and analysis" -ForegroundColor White
}

Write-Host "`n📚 For detailed setup instructions, see AZURE_SETUP.md" -ForegroundColor Cyan
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")