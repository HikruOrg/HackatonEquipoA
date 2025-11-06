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

#### Step 2: Configure API Permissions
1. In your app registration, go to "API permissions"
2. Click "Add a permission" → "Microsoft Graph" → "Delegated permissions"
3. Add these permissions:
   ```
   Mail.Read
   Mail.ReadBasic
   User.Read
   ```
4. Click "Grant admin consent" (if you're an admin)

#### Step 3: Get App Registration Details
1. Go to "Overview" tab
2. Copy the following values:
   ```
   Application (client) ID: [your-client-id]
   Directory (tenant) ID: [your-tenant-id]
   ```

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
   ```

### Option 3: .env File (Development)
Create a `.env` file in the project root:
```env
AZURE_OPENAI_API_KEY=your-azure-openai-api-key
AZURE_OPENAI_ENDPOINT=https://your-resource-name.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT=gpt-4
MICROSOFT_GRAPH_CLIENT_ID=your-client-id
MICROSOFT_GRAPH_TENANT_ID=your-tenant-id
```

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