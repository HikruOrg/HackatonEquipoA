# 🔐 Authentication Guide for Lead Research Agent

This guide explains the different authentication methods available for automating Outlook email access.

## Authentication Options

The Lead Research Agent supports **three authentication methods** for accessing Microsoft Graph and Outlook emails:

### 1. Interactive Browser Authentication (Default)
**Best for**: Local development and initial setup
**Requires**: Browser access on the same machine

```bash
# No additional configuration needed
dotnet run
```

When you choose option 1 (Process Outlook emails), a browser window opens for you to sign in.

**Pros**: 
- Simple to use
- No additional Azure configuration needed
- Works with any Azure AD account

**Cons**: 
- Requires browser interaction
- Not suitable for automation or headless environments
- Tokens expire and require re-authentication

---

### 2. Device Code Authentication (Recommended for Automation)
**Best for**: Automation, CI/CD pipelines, remote servers, headless environments
**Requires**: Manual code entry via any device with internet access

```bash
# Enable device code authentication
export USE_DEVICE_CODE_AUTH=true  # Linux/Mac
$env:USE_DEVICE_CODE_AUTH="true"  # Windows PowerShell

dotnet run
```

When you run the application, it will display:
```
================================================
🔐 DEVICE CODE AUTHENTICATION
================================================
📱 User Code: ABC-DEF-GHI
🌐 Verification URL: https://microsoft.com/devicelogin
⏱️  Expires: 2024-01-15 10:30:00
================================================

✅ Please visit the URL above and enter the code to authenticate.
⏳ Waiting for authentication...
```

**How it works**:
1. Application generates a user code
2. You visit https://microsoft.com/devicelogin on ANY device
3. Enter the code and authenticate
4. Application automatically continues

**Pros**: 
- ✅ Perfect for automation and CI/CD
- ✅ No browser required on the same machine
- ✅ Can authenticate from a different device
- ✅ Tokens can be cached for future runs
- ✅ Works in Docker containers, remote servers, etc.

**Cons**: 
- Requires manual code entry (only once if tokens are cached)

---

### 3. Client Credentials Flow (Service Automation)
**Best for**: Service-to-service scenarios, application-only access
**Requires**: Client Secret and Application permissions

```bash
# Configure client secret
export MICROSOFT_GRAPH_CLIENT_SECRET="your-client-secret"  # Linux/Mac
$env:MICROSOFT_GRAPH_CLIENT_SECRET="your-client-secret"  # Windows PowerShell

dotnet run
```

**Azure Configuration Required**:
1. In Azure Portal → App Registration → Certificates & secrets
2. Create a new client secret
3. In API permissions, add **Application permissions** (not delegated):
   - Mail.Read (Application)
   - Mail.ReadWrite (Application)
   - User.Read.All (Application)
4. Grant admin consent

**Important Notes**:
- ⚠️  This method requires accessing a specific user's mailbox
- ⚠️  You need to modify the code to use `Users('user@domain.com').Messages` instead of `Me.Messages`
- ⚠️  Requires admin consent for application permissions
- ⚠️  Best for service accounts, not personal mailboxes

**Pros**: 
- Fully automated (no user interaction)
- Suitable for background services

**Cons**: 
- Requires more Azure configuration
- Needs application permissions and admin consent
- Code changes needed to access specific user mailboxes

---

## Quick Setup Guide

### For Local Development
No special configuration needed. Just run:
```bash
dotnet run
```
Choose option 1, and sign in when the browser opens.

### For Automation (Recommended)

1. **Set up your Azure App Registration**:
   ```bash
   # In Azure Portal → App Registrations → Your App
   # 1. Copy Client ID and Tenant ID
   # 2. Under "Authentication", add redirect URI:
   #    Type: Public client/native
   #    URI: http://localhost
   # 3. Under "API permissions", add delegated permissions:
   #    - Mail.Read
   #    - Mail.Send
   #    - User.Read
   # 4. Grant admin consent (if required)
   ```

2. **Configure environment variables**:
   ```bash
   # Linux/Mac
   export MICROSOFT_GRAPH_CLIENT_ID="your-client-id"
   export MICROSOFT_GRAPH_TENANT_ID="your-tenant-id"
   export USE_DEVICE_CODE_AUTH="true"
   
   # Windows PowerShell
   $env:MICROSOFT_GRAPH_CLIENT_ID="your-client-id"
   $env:MICROSOFT_GRAPH_TENANT_ID="your-tenant-id"
   $env:USE_DEVICE_CODE_AUTH="true"
   ```

3. **Run the application**:
   ```bash
   dotnet run
   ```

4. **Authenticate once**:
   - You'll see a device code displayed
   - Visit https://microsoft.com/devicelogin on any device
   - Enter the code and sign in
   - The token will be cached for future runs

5. **Subsequent runs**:
   - The application will use cached tokens
   - No need to authenticate again until tokens expire

---

## Troubleshooting

### "Redirect URI not configured" error
**Solution**: Add `http://localhost` as a redirect URI in Azure Portal:
1. Go to Azure Portal → App Registrations → Your App
2. Click "Authentication"
3. Under "Platform configurations", click "Add a platform"
4. Choose "Mobile and desktop applications"
5. Add custom redirect URI: `http://localhost`
6. Save changes

**Note**: If you get "distinct redirect URIs" error, the URI already exists - this is fine!

### "Authentication failed" error
**Solutions**:
1. Verify your Client ID and Tenant ID are correct
2. Check that API permissions are granted
3. Try device code authentication:
   ```bash
   export USE_DEVICE_CODE_AUTH=true
   dotnet run
   ```
4. Ensure your account has access to the mailbox

### "No LinkSV Pulse emails found"
**Solutions**:
1. Verify you have emails from LinkSV in your inbox
2. Check the search filter in `OutlookEmailService.cs`
3. Try with a broader date range

### "Token expired" error
**Solution**: Delete cached tokens and re-authenticate:
```bash
# Tokens are typically cached in:
# Windows: %LOCALAPPDATA%\.IdentityService
# Linux/Mac: ~/.IdentityService
rm -rf ~/.IdentityService  # or delete the folder manually
dotnet run
```

---

## Environment Variables Reference

| Variable | Required | Description |
|----------|----------|-------------|
| `MICROSOFT_GRAPH_CLIENT_ID` | Yes | Application (client) ID from Azure Portal |
| `MICROSOFT_GRAPH_TENANT_ID` | Yes | Directory (tenant) ID from Azure Portal |
| `MICROSOFT_GRAPH_CLIENT_SECRET` | Optional | Client secret for service automation |
| `USE_DEVICE_CODE_AUTH` | Optional | Set to "true" to enable device code flow |
| `AZURE_OPENAI_API_KEY` | Yes | Azure OpenAI API key |
| `AZURE_OPENAI_ENDPOINT` | Yes | Azure OpenAI endpoint URL |
| `AZURE_OPENAI_DEPLOYMENT` | Optional | Deployment name (default: "gpt-4") |
| `DEFAULT_RECIPIENT_EMAIL` | Optional | Email address to send results to |

---

## Security Best Practices

1. **Never commit secrets to version control**
   - Use environment variables
   - Use Azure Key Vault for production
   - Add `.env` to `.gitignore`

2. **Use least-privilege permissions**
   - Only request the permissions you need
   - Mail.Read, Mail.Send, User.Read are sufficient for most cases

3. **Rotate credentials regularly**
   - Rotate client secrets every 90 days
   - Monitor authentication logs

4. **Use managed identities when possible**
   - In Azure environments, use managed identities
   - Eliminates need for storing credentials

---

## Example: Automated Setup Script

```bash
#!/bin/bash
# automated-setup.sh

# Configure environment
export MICROSOFT_GRAPH_CLIENT_ID="your-client-id"
export MICROSOFT_GRAPH_TENANT_ID="your-tenant-id"
export USE_DEVICE_CODE_AUTH="true"
export AZURE_OPENAI_API_KEY="your-openai-key"
export AZURE_OPENAI_ENDPOINT="https://your-endpoint.openai.azure.com/"
export DEFAULT_RECIPIENT_EMAIL="results@yourdomain.com"

# First run - authenticate once
echo "🔐 First-time authentication required..."
dotnet run

# Subsequent runs - fully automated
echo "✅ Token cached! Future runs will be automated."
```

---

## CI/CD Integration Example

### GitHub Actions
```yaml
name: Process LinkSV Emails

on:
  schedule:
    - cron: '0 9 * * 1'  # Every Monday at 9 AM

jobs:
  process:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '8.0.x'
      
      - name: Run Lead Research Agent
        env:
          MICROSOFT_GRAPH_CLIENT_ID: ${{ secrets.GRAPH_CLIENT_ID }}
          MICROSOFT_GRAPH_TENANT_ID: ${{ secrets.GRAPH_TENANT_ID }}
          USE_DEVICE_CODE_AUTH: "true"
          AZURE_OPENAI_API_KEY: ${{ secrets.OPENAI_KEY }}
          AZURE_OPENAI_ENDPOINT: ${{ secrets.OPENAI_ENDPOINT }}
        run: |
          cd LeadResearchAgent
          dotnet run
```

**Note**: For CI/CD, you'll need to authenticate once manually and cache the tokens, or use a service principal with client credentials flow.

---

## Getting Help

If you encounter issues:
1. Check this authentication guide
2. Review the error messages - they include helpful troubleshooting tips
3. Verify your Azure configuration in the portal
4. Check the application logs for detailed error information

For more information:
- [Azure App Registration Documentation](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app)
- [Microsoft Graph Permissions](https://docs.microsoft.com/graph/permissions-reference)
- [Device Code Flow](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-device-code)

---

**Ready to automate your email processing! 🚀**
