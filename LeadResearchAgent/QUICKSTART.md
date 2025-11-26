# Quick Start: Automating Outlook Email Authentication

## 🚀 For Automation (Recommended)

### 1. Configure Azure App Registration
```bash
# In Azure Portal:
# 1. Create App Registration (or use existing)
# 2. Add redirect URI: http://localhost (type: Public client/native)
# 3. Add delegated permissions: Mail.Read, Mail.Send, User.Read
# 4. Grant admin consent
```

### 2. Set Environment Variables
```bash
# Windows PowerShell
$env:MICROSOFT_GRAPH_CLIENT_ID="your-client-id"
$env:MICROSOFT_GRAPH_TENANT_ID="your-tenant-id"
$env:USE_DEVICE_CODE_AUTH="true"
$env:AZURE_OPENAI_API_KEY="your-openai-key"
$env:AZURE_OPENAI_ENDPOINT="https://your-endpoint.openai.azure.com/"

# Linux/Mac
export MICROSOFT_GRAPH_CLIENT_ID="your-client-id"
export MICROSOFT_GRAPH_TENANT_ID="your-tenant-id"
export USE_DEVICE_CODE_AUTH="true"
export AZURE_OPENAI_API_KEY="your-openai-key"
export AZURE_OPENAI_ENDPOINT="https://your-endpoint.openai.azure.com/"
```

### 3. Run the Application
```bash
dotnet run
```

### 4. First-Time Authentication
When you run the application, you'll see:
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

**What to do:**
1. Open https://microsoft.com/devicelogin on ANY device (phone, tablet, another computer)
2. Enter the displayed code
3. Sign in with your Microsoft 365 account
4. Return to the application - it will continue automatically

### 5. Subsequent Runs
After the first authentication, tokens are cached. Future runs will:
- ✅ Use cached tokens automatically
- ✅ No authentication required
- ✅ Fully automated

---

## 📋 Other Authentication Methods

### Interactive Browser (Default)
No special configuration needed. Just run:
```bash
dotnet run
```
A browser window will open for sign-in.

### Client Credentials (Service Accounts)
For fully automated service-to-service authentication:
```bash
$env:MICROSOFT_GRAPH_CLIENT_SECRET="your-client-secret"
```
**Note:** Requires application permissions and code changes to access specific user mailboxes.

---

## 🔧 Troubleshooting

### "Redirect URI not configured"
Add `http://localhost` as redirect URI in Azure Portal.
If you get "distinct redirect URIs" error, it's already configured - that's fine!

### "Authentication failed"
1. Verify Client ID and Tenant ID are correct
2. Check API permissions are granted
3. Ensure your account has mailbox access
4. Try device code authentication

### "Token expired"
Delete cached tokens:
```bash
# Windows
del %LOCALAPPDATA%\.IdentityService

# Linux/Mac
rm -rf ~/.IdentityService
```

---

## 📚 More Information

- **Complete Guide:** [AUTHENTICATION.md](AUTHENTICATION.md)
- **Azure Setup:** [AZURE_SETUP.md](AZURE_SETUP.md)
- **Main README:** [README.md](README.md)

---

**Ready to automate! 🎯**
