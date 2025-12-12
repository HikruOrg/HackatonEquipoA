# Lead Research Agent - Hikru Hackathon Team A

🎯 **Goal**: Automated newsletter analysis → extract companies → score vs. ICP → generate outreach → send results via email

An intelligent lead research agent that automatically monitors Outlook for LinkSV Pulse newsletters, analyzes them using Azure AI Foundry agents, scores companies against your Ideal Customer Profile (ICP), and sends personalized results via email.

## 🚀 Features

- **Automated Email Monitoring**: Continuously monitors Outlook inbox for LinkSV Pulse newsletters
- **Scheduled Execution**: Configurable execution intervals and daily scheduled times with timezone support
- **AI-Powered Analysis**: Uses Azure AI Foundry agents for intelligent newsletter processing
- **ICP Scoring**: Scores companies against your Ideal Customer Profile with interest levels (Alto/Medio/Bajo/Descartar)
- **Automated Results Distribution**: Sends formatted HTML emails with analysis results to configured recipients
- **Microsoft Graph Integration**: Full integration with Outlook for email reading and sending
- **Azure WebJob Compatible**: Can run as Azure WebJob in continuous or triggered mode

## 🏗️ Architecture

```
LeadResearchAgent/
├── Models/
│   ├── Company.cs                  # Company data structure
│   ├── EmailMessage.cs             # Email message model
│   ├── Empresa.cs                  # Company details (Spanish model)
│   ├── FoundryCompanyResult.cs     # Azure Foundry agent result model
│   ├── CamposRelevantes.cs         # Relevant fields for ICP matching
│   ├── Meta.cs                     # Metadata model
│   └── NewsletterEmail.cs          # Newsletter email model
├── Services/
│   └── OutlookEmailService.cs      # Microsoft Graph email operations
├── Agents/
│   └── AzureFoundryLeadAgent.cs    # Azure AI Foundry agent client
├── Worker.cs                        # Background service orchestration
├── Program.cs                       # Application entry point & DI configuration
└── README.md                        # This file
```

## 🛠️ Setup Instructions

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or Visual Studio Code
- Azure AI Foundry account with configured agent
- Azure App Registration for Microsoft Graph access
- Microsoft 365 account with Outlook access

### Step 1: Clone the Repository
```bash
git clone https://github.com/HikruOrg/HackatonEquipoA.git
cd HackatonEquipoA/LeadResearchAgent
```

### Step 2: Install Dependencies
```bash
dotnet restore
```

### Step 3: Configure Environment Variables

Create a `.env` file (copy from `.env.example`):

#### Required Variables

**Azure AI Foundry Configuration**
```bash
AZURE_FOUNDRY_ENDPOINT=https://your-foundry-endpoint.services.ai.azure.com/api/projects/YourProject
AZURE_FOUNDRY_API_KEY=your-foundry-api-key-here
AZURE_FOUNDRY_AGENT_ID=asst_your-agent-id-here
```

**Microsoft Graph Configuration**

For Production (App-Only Authentication):
```bash
MICROSOFT_GRAPH_CLIENT_ID=your-client-id-here
MICROSOFT_GRAPH_TENANT_ID=your-tenant-id-here
MICROSOFT_GRAPH_CLIENT_SECRET=your-client-secret-here
MICROSOFT_GRAPH_USER_ID=user@domain.com
```

For Development (Interactive Authentication):
```bash
MICROSOFT_GRAPH_CLIENT_ID=your-client-id-here
MICROSOFT_GRAPH_TENANT_ID=your-tenant-id-here
# Leave MICROSOFT_GRAPH_CLIENT_SECRET empty for interactive browser login
MICROSOFT_GRAPH_CLIENT_SECRET=
```

**Email Recipients Configuration**
```bash
# Single recipient
RECIPIENT_EMAILS=recipient@example.com

# Multiple recipients (semicolon or comma separated)
RECIPIENT_EMAILS=recipient1@example.com;recipient2@example.com;recipient3@example.com
```

#### Optional Variables - Worker Configuration

**Execution Interval** (Required for continuous mode)
```bash
# Check every 60 minutes
WORKER_EXECUTION_INTERVAL=60

# Or use TimeSpan format: "01:00:00" (1 hour)
WORKER_EXECUTION_INTERVAL=01:00:00
```

**Execution Time** (Optional - specific time of day)
```bash
# Execute only at 2:30 PM
WORKER_EXECUTION_TIME=14:30

# Execute only at 9:00 AM
WORKER_EXECUTION_TIME=09:00
```

**Timezone** (Optional - defaults to UTC)
```bash
# El Salvador / Central America
WORKER_TIMEZONE=Central America Standard Time

# US Pacific Time
WORKER_TIMEZONE=Pacific Standard Time

# US Eastern Time
WORKER_TIMEZONE=Eastern Standard Time
```

### Step 4: Azure App Registration Setup

1. Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory → App Registrations
2. Create a new registration or use existing
3. Under **API Permissions**, add:
   - `Mail.Read` - To read newsletters from Outlook
   - `Mail.Send` - To send result emails
4. Grant admin consent for the permissions
5. Under **Certificates & secrets**, create a new client secret
6. Copy the Client ID, Tenant ID, and Client Secret to your `.env` file

### Step 5: Run the Application

**Local Development:**
```bash
dotnet run
```

**Production (Azure WebJob):**
```bash
# Build and publish
dotnet publish -c Release -o ./publish

# Deploy to Azure App Service as WebJob
# Upload publish folder as Continuous WebJob
```

## 📊 Execution Modes

### Mode 1: Scheduled Daily Execution (Recommended for Production)
Best for processing newsletters at a specific time each day.

```bash
WORKER_EXECUTION_INTERVAL=15          # Check every 15 minutes
WORKER_EXECUTION_TIME=14:30           # Execute at 2:30 PM
WORKER_TIMEZONE=Central America Standard Time
```

**Behavior:**
- Worker checks every 15 minutes if current time is 2:30 PM
- Executes once per day at 2:30 PM (within tolerance window)
- Waits until next day for next execution
- Ideal for Azure WebJob in **Continuous** mode

### Mode 2: Continuous Interval (Good for Frequent Processing)
Best for processing newsletters multiple times per day.

```bash
WORKER_EXECUTION_INTERVAL=60          # Execute every hour
# Leave WORKER_EXECUTION_TIME empty
```

**Behavior:**
- Executes every 60 minutes regardless of time
- Continuously processes new newsletters
- Ideal for Azure WebJob in **Continuous** mode

### Mode 3: Triggered (Good for Manual Execution)
Best for on-demand processing or testing.

```bash
# Leave WORKER_EXECUTION_INTERVAL empty
```

**Behavior:**
- Executes once and exits
- Use for manual runs or Azure WebJob in **Triggered** mode
- Can be scheduled using Azure WebJob scheduler (cron)

## 🔄 Processing Workflow

1. **Email Monitoring**: Worker checks Outlook inbox for unread "Fw: Pulse of the Valley Premium" emails
2. **Content Extraction**: Extracts newsletter HTML content
3. **AI Analysis**: Sends content to Azure AI Foundry agent for processing
4. **Agent Processing**: 
   - Extracts company information
   - Scores against ICP criteria
   - Assigns interest levels (Alto/Medio/Bajo/Descartar)
   - Generates match reasoning
5. **Results Compilation**: Structures results with accepted and rejected companies
6. **Email Distribution**: Sends formatted HTML results to configured recipients
7. **Mark as Read**: Marks processed emails as read to avoid reprocessing

## 📧 Input/Output Examples

### Input: Outlook Email
```
Subject: Fw: Pulse of the Valley Premium
From: newsletter@linksv.com
Content: [HTML newsletter with company funding announcements]
```

### Output: Foundry Agent Results
```json
[
  {
    "empresa": {
      "nombre": "TechFlow Analytics",
      "sector": "FinTech",
      "pais": "USA",
      "ciudad": "San Francisco"
    },
    "empresaUrl": "https://techflow.com",
    "totalCapital": "15",
    "nivelInteres": "alto",
    "resumen": "AI-powered financial analytics platform for banks securing $8M Series A",
    "razonDeMatch": "Strong fit: FinTech sector, Series A stage, US-based, AI technology stack"
  },
  {
    "empresa": {
      "nombre": "Small Local Startup",
      "sector": "Retail",
      "pais": "USA"
    },
    "totalCapital": "0.5",
    "nivelInteres": "descartar",
    "resumen": "Local retail business",
    "razonDeMatch": "Outside ICP: Too early stage, wrong sector, insufficient funding"
  }
]
```

### Output: Email Sent
```html
<h2>Resultados Lead Research Agent</h2>

<h3>Aceptadas</h3>
<ul>
  <li>
    <strong><a href="https://techflow.com" target="_blank">TechFlow Analytics</a></strong> (FinTech, USA)<br>
    <b>Capital:</b> 15M <br>
    <b>Interés:</b> alto<br>
    <b>Resumen:</b> AI-powered financial analytics platform for banks securing $8M Series A<br>
    <b>Razón de match:</b> Strong fit: FinTech sector, Series A stage, US-based, AI technology stack<br>
  </li>
</ul>

<h3>Descartadas</h3>
<ul>
  <li>
    <strong>Small Local Startup</strong> (Retail, USA)<br>
    <b>Capital:</b> 0.5M <br>
    <b>Interés:</b> descartar<br>
    <b>Resumen:</b> Local retail business<br>
    <b>Razón de descarte:</b> Outside ICP: Too early stage, wrong sector, insufficient funding<br>
  </li>
</ul>
```

## 🔧 Technology Stack

- **Framework**: .NET 8.0 Console Application / Background Service
- **AI Engine**: Azure AI Foundry with Persistent Agents
- **Authentication**: Azure Identity (Managed Identity + Environment Credentials)
- **Email Integration**: Microsoft Graph SDK
- **Hosting**: Azure WebJobs compatible (Continuous/Triggered modes)
- **Architecture**: Worker Service pattern with dependency injection

## 🔐 Authentication Methods

### Option 1: Delegated Authentication (Development)
- Uses `InteractiveBrowserCredential`
- Opens browser for user login
- Good for local development and testing
- Requires user to be present

### Option 2: App-Only Authentication (Production)
- Uses `ClientSecretCredential`
- No user interaction needed
- Requires admin consent in Azure AD
- Recommended for Azure WebJob deployment

### Azure AI Foundry Authentication
- Uses `ChainedTokenCredential`
- Tries `ManagedIdentityCredential` first (Azure App Service)
- Falls back to `EnvironmentCredential` (local development with service principal)

## 📈 Interest Level Classification

The Azure AI Foundry agent classifies companies into 4 interest levels:

- **Alto** (High): Strong ICP match across multiple criteria
- **Medio** (Medium): Moderate ICP match with some misalignment
- **Bajo** (Low): Weak ICP match but still potentially interesting
- **Descartar** (Reject): Does not meet minimum ICP criteria

Results are organized into **Aceptadas** (Alto/Medio/Bajo) and **Descartadas** (Descartar) sections in the email.

## 🎯 Use Cases

1. **Automated Lead Monitoring**: Daily processing of investment newsletters
2. **Sales Prospecting**: Find companies matching your buyer persona automatically
3. **Competitive Intelligence**: Track funding in your market segment
4. **Partnership Discovery**: Identify potential integration partners daily
5. **Investment Tracking**: Monitor funding rounds in specific sectors

## 🚀 Deployment to Azure

### Azure App Service WebJob

1. **Publish the application:**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Create settings.job file** (already included):
   ```json
   {
     "schedule": "0 0 */1 * * *",
     "is_singleton": true
   }
   ```

3. **Zip the publish folder:**
   ```bash
   cd publish
   zip -r ../LeadResearchAgent.zip *
   ```

4. **Upload to Azure App Service:**
   - Go to Azure Portal → Your App Service → WebJobs
   - Add → Name: LeadResearchAgent
   - Type: Continuous (for scheduled execution) or Triggered (for manual)
   - Upload LeadResearchAgent.zip

5. **Configure Environment Variables:**
   - Go to Configuration → Application Settings
   - Add all required environment variables from `.env.example`

### Enable Managed Identity (Recommended)

1. Go to Azure Portal → Your App Service → Identity
2. Enable System Assigned Managed Identity
3. Grant the managed identity access to Azure AI Foundry
4. Remove `AZURE_FOUNDRY_API_KEY` from environment variables (authentication will use managed identity)

## 🛠️ Troubleshooting

### Email not being read
- Check `MICROSOFT_GRAPH_USER_ID` is correct
- Verify Mail.Read permission is granted
- Ensure emails are unread and match filter: `"Fw: Pulse of the Valley Premium"`

### Email not being sent
- Check `RECIPIENT_EMAILS` environment variable is set
- Verify Mail.Send permission is granted
- Check logs for specific error messages

### Execution time not triggering
- Verify `WORKER_TIMEZONE` matches your expected timezone
- Check `WORKER_EXECUTION_TIME` format is "HH:mm" (24-hour)
- Ensure `WORKER_EXECUTION_INTERVAL` is set (required for continuous mode)
- Review logs to see current time vs execution time

### Azure Foundry agent timeout
- Check `AZURE_FOUNDRY_ENDPOINT` and `AZURE_FOUNDRY_AGENT_ID` are correct
- Verify agent is deployed and active in Azure AI Foundry
- Check agent response format matches `FoundryCompanyResult` model

## 📝 Configuration Best Practices

1. **Production Environment:**
   - Use App-Only Authentication (client secret)
   - Enable Managed Identity for Azure AI Foundry
   - Set `WORKER_EXECUTION_INTERVAL` to 15-30 minutes
   - Set `WORKER_EXECUTION_TIME` to specific daily time
   - Configure proper timezone

2. **Development Environment:**
   - Use Interactive Authentication (no client secret)
   - Use API key for Azure AI Foundry
   - Set shorter intervals for testing (5-15 minutes)
   - Test with single recipient email first

3. **Security:**
   - Never commit `.env` file to source control
   - Rotate client secrets regularly
   - Use Azure Key Vault for sensitive configuration in production
   - Review and limit Microsoft Graph permissions to minimum required

## 🏆 Hackathon Team A

Building the future of intelligent lead research for Hikru! 

---

**Ready to automate your lead research? Let's get started!** 🎯