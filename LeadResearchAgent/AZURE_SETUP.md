# 🔧 Azure Configuration Guide for Lead Research Agent

## 📋 Required Azure Resources

### 1. Azure OpenAI Service (for AI-powered text extraction)

#### Step 1: Create Azure OpenAI Resource
1. Go to [Azure Portal](https://portal.azure.com)
2. Click "Create a resource" → Search "Azure OpenAI"
3. Fill in the details:
   - **Subscription**: Your Azure subscription
   - **Resource Group**: Create new or use existing
   - **Region**: Choose a region that supports GPT-4 (e.g., East US, West Europe)
   - **Name**: `hikru-openai-service`
   - **Pricing Tier**: Standard S0

#### Step 2: Deploy GPT-4 Model
1. After creation, go to your Azure OpenAI resource
2. Navigate to "Model Deployments" → "Create New Deployment"
3. Select:
   - **Model**: `gpt-4` (or `gpt-4-turbo`)
   - **Deployment Name**: `gpt-4`
   - **Version**: Latest available
   - **Tokens per Minute Rate Limit**: 10K (or as needed)

#### Step 3: Get API Credentials
1. In your Azure OpenAI resource, go to "Keys and Endpoint"
2. Copy the following values:
   ```
   Key 1: [your-api-key]
   Endpoint: https://[your-resource-name].openai.azure.com/
   ```

### 2. Microsoft Graph App Registration (for Outlook email access)

#### Step 1: Create App Registration
1. Go to [Azure Portal](https://portal.azure.com) → "App registrations"
2. Click "New registration"
3. Fill in:
   - **Name**: `Hikru Lead Research Agent`
   - **Supported account types**: Accounts in this organizational directory only
   - **Redirect URI**: 
     - Platform: Public client/native (mobile & desktop)
     - URI: `http://localhost`
     - **Note**: If you see "distinct redirect URIs" error, the URI already exists - that's fine!

#### Step 2: Configure API Permissions
1. In your app registration, go to "API permissions"
2. Click "Add a permission" → "Microsoft Graph" → "Delegated permissions"
3. Add these permissions:
   ```
   Mail.Read
   Mail.ReadBasic
   Mail.Send
   User.Read
   ```
4. Click "Grant admin consent" (if you're an admin)

**For Automation**: See [AUTHENTICATION.md](AUTHENTICATION.md) for device code flow setup

**For Service Automation** (optional):
1. Go to "Certificates & secrets"
2. Create a new client secret
3. Save the secret value (shown only once)
4. In "API permissions", add **Application permissions**:
   - Mail.Read
   - Mail.ReadWrite
5. Grant admin consent

#### Step 3: Get App Registration Details
1. Go to "Overview" tab
2. Copy the following values:
   ```
   Application (client) ID: [your-client-id]
   Directory (tenant) ID: [your-tenant-id]
   ```

**📚 For detailed authentication setup and automation options, see [AUTHENTICATION.md](AUTHENTICATION.md)**

### 3. Azure AI Foundry / AI Studio (Optional - for enhanced AI capabilities)

#### Step 1: Create AI Foundry Project
1. Go to [Azure AI Foundry](https://ai.azure.com)
2. Click "New project" or use existing project
3. Note the following values:
   ```
   Project ID: [your-project-id]
   Endpoint: https://[your-project].openai.azure.com/
   API Key: [your-api-key]
   ```

#### Step 2: Deploy Models (if needed)
1. In AI Foundry, go to "Deployments"
2. Deploy required models (GPT-4, etc.)
3. Note deployment names for configuration

## 🔑 Environment Variables Setup

### Option 1: Windows PowerShell (Current Session)
```powershell
# Azure OpenAI Configuration
$env:AZURE_OPENAI_API_KEY="your-azure-openai-api-key"
$env:AZURE_OPENAI_ENDPOINT="https://your-resource-name.openai.azure.com/"
$env:AZURE_OPENAI_DEPLOYMENT="gpt-4"

# Microsoft Graph Configuration
$env:MICROSOFT_GRAPH_CLIENT_ID="your-client-id"
$env:MICROSOFT_GRAPH_TENANT_ID="your-tenant-id"
$env:MICROSOFT_GRAPH_CLIENT_SECRET="your-client-secret"  # Optional for app-only auth

# Email Configuration
$env:DEFAULT_SENDER_EMAIL="your-sender@company.com"
$env:DEFAULT_RECIPIENT_EMAIL="recipient@company.com"

# Azure AI Foundry Configuration (Optional)
$env:AZURE_AI_FOUNDRY_ENDPOINT="https://your-project.openai.azure.com/"
$env:AZURE_AI_FOUNDRY_API_KEY="your-ai-foundry-api-key"
$env:AZURE_AI_FOUNDRY_PROJECT_ID="your-project-id"
```

### Option 2: User Environment Variables (Persistent)
1. Press `Win + R`, type `sysdm.cpl`, press Enter
2. Click "Environment Variables"
3. Under "User variables", click "New" for each:
   ```
   AZURE_OPENAI_API_KEY = your-azure-openai-api-key
   AZURE_OPENAI_ENDPOINT = https://your-resource-name.openai.azure.com/
   AZURE_OPENAI_DEPLOYMENT = gpt-4
   MICROSOFT_GRAPH_CLIENT_ID = your-client-id
   MICROSOFT_GRAPH_TENANT_ID = your-tenant-id
   MICROSOFT_GRAPH_CLIENT_SECRET = your-client-secret
   DEFAULT_SENDER_EMAIL = your-sender@company.com
   DEFAULT_RECIPIENT_EMAIL = recipient@company.com
   AZURE_AI_FOUNDRY_ENDPOINT = https://your-project.openai.azure.com/
   AZURE_AI_FOUNDRY_API_KEY = your-ai-foundry-api-key
   AZURE_AI_FOUNDRY_PROJECT_ID = your-project-id
   ```

### Option 3: .env File (Development)
Create a `.env` file in the project root:
```env
AZURE_OPENAI_API_KEY=your-azure-openai-api-key
AZURE_OPENAI_ENDPOINT=https://your-resource-name.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT=gpt-4
MICROSOFT_GRAPH_CLIENT_ID=your-client-id
MICROSOFT_GRAPH_TENANT_ID=your-tenant-id
MICROSOFT_GRAPH_CLIENT_SECRET=your-client-secret
DEFAULT_SENDER_EMAIL=your-sender@company.com
DEFAULT_RECIPIENT_EMAIL=recipient@company.com
AZURE_AI_FOUNDRY_ENDPOINT=https://your-project.openai.azure.com/
AZURE_AI_FOUNDRY_API_KEY=your-ai-foundry-api-key
AZURE_AI_FOUNDRY_PROJECT_ID=your-project-id
```

### Option 4: appsettings.json Configuration
Update the `appsettings.json` file in your project:
```json
{
  "Azure": {
    "KeyVault": {
      "Url": "https://your-keyvault.vault.azure.net/"
    },
    "OpenAI": {
      "ApiKey": "your-azure-openai-api-key",
      "Endpoint": "https://your-resource-name.openai.azure.com/",
      "Deployment": "gpt-4"
    },
    "Graph": {
      "ClientId": "your-client-id",
      "TenantId": "your-tenant-id",
      "ClientSecret": "your-client-secret"
    },
    "AIFoundry": {
      "Endpoint": "https://your-project.openai.azure.com/",
      "ApiKey": "your-ai-foundry-api-key",
      "ProjectId": "your-project-id"
    }
  },
  "Email": {
    "DefaultSender": "your-sender@company.com",
    "DefaultRecipient": "recipient@company.com"
  }
}
```

## 🚀 New Features

### 📧 Email Integration
- **Send Results**: Automatically send lead research results via email
- **HTML Reports**: Beautiful HTML-formatted email reports with company details
- **JSON Attachments**: Detailed data attached for further processing
- **Configurable Recipients**: Set default recipient email addresses

### 🤖 Azure AI Foundry Integration
- **Enhanced ICP Scoring**: AI-powered lead scoring improvements
- **Company Enrichment**: Automatic data enrichment with AI insights
- **Market Analysis**: Trend analysis and strategic recommendations
- **Outreach Generation**: AI-powered personalized outreach messages

### 🔧 Configuration Management
- **Multiple Config Sources**: Environment variables, appsettings.json, Azure Key Vault
- **Centralized Credentials**: Secure credential management
- **Fallback Options**: Graceful degradation when services unavailable

## 🧪 Testing Your Configuration

### Test Azure OpenAI Connection
```powershell
# Set environment variables
$env:AZURE_OPENAI_API_KEY="your-key"
$env:AZURE_OPENAI_ENDPOINT="your-endpoint"

# Run the application
dotnet run
```

### Test Microsoft Graph Connection
When you run the application and choose option 1 (Outlook emails), a browser window will open asking you to sign in with your Microsoft account.

### Test Email Sending
Set `DEFAULT_RECIPIENT_EMAIL` to test automatic email sending after processing leads.

### Test AI Foundry Integration
Configure AI Foundry credentials to enable enhanced AI features like improved scoring and market analysis.

## 🔒 Security Best Practices

### 1. API Key Security
- **Never commit API keys to version control**
- Use Azure Key Vault for production environments
- Rotate keys regularly
- Use managed identities when possible

### 2. Graph Permissions
- Use least-privilege principle
- Only request permissions you actually need
- Consider using application permissions for service scenarios

### 3. Access Control
- Limit app registration access to specific users/groups
- Monitor API usage and set up alerts
- Implement proper error handling for authentication failures

## 💰 Cost Estimation

### Azure OpenAI Costs
- **GPT-4**: ~$0.03 per 1K input tokens, ~$0.06 per 1K output tokens
- **Estimated monthly cost**: $50-200 depending on usage
- **LinkSV Pulse processing**: ~$5-10 per newsletter

### Microsoft Graph
- **Free tier**: Up to 10,000 API calls per month
- **No additional cost** for basic email reading

## 🚨 Troubleshooting

### Common Issues

#### "No service was found for any of the supported types"
- Check that `AZURE_OPENAI_API_KEY` and `AZURE_OPENAI_ENDPOINT` are set
- Verify your Azure OpenAI deployment is active
- Ensure the deployment name matches your environment variable

#### "Authentication failed" for Graph
- Verify your client ID and tenant ID are correct
- Check that API permissions are granted
- Make sure your account has access to the mailbox

#### "No LinkSV Pulse emails found"
- Check that you have emails from LinkSV in your inbox
- Verify the search filters in `OutlookEmailService.cs`
- Try expanding the date range (currently set to 7 days)

## 📞 Support

If you encounter issues:
1. Check the Azure Portal for service status
2. Review the application logs for detailed error messages
3. Verify all environment variables are correctly set
4. Test with the demo mode first (option 2) to isolate issues

---

**Ready to process LinkSV Pulse newsletters with AI! 🚀**