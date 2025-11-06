# Lead Research Agent - Hikru Hackathon Team A

🎯 **Goal**: Paste newsletter text → extract companies → score vs. ICP → outreach blurb

An intelligent lead research agent that analyzes newsletters to find companies matching your Ideal Customer Profile (ICP) and generates personalized outreach messages.

## 🚀 Features

- **Newsletter Text Extraction**: Converts newsletter content into structured JSON company data
- **ICP Scoring**: Scores companies against your Ideal Customer Profile criteria
- **CSV Enrichment**: Enhances company data with domain and headcount information
- **Outreach Generation**: Creates personalized 2-line outreach messages
- **AI-Powered**: Uses Azure AI Foundry and Semantic Kernel for intelligent processing
- **Microsoft Graph Integration**: Ready for email filtering capabilities

## 📋 Required Features (Balanced Implementation)

✅ **Extraction to strict JSON** - `{company, round, amount, sector, HQ, snippet}`  
✅ **Score against provided ICP.json** - Industry, stage, size, geo, tech hints  
✅ **Generate 2-line outreach angle** per company  
✅ **Light enrichment from CSV** - Domain, headcount (no external calls)  

## 🏗️ Architecture

```
LeadResearchAgent/
├── Models/
│   ├── Company.cs          # Company data structure
│   ├── ICP.cs             # Ideal Customer Profile model
│   └── EnrichmentData.cs  # CSV enrichment model
├── Services/
│   ├── CompanyExtractionService.cs  # Newsletter → JSON extraction
│   ├── ICPScoringService.cs        # ICP matching & scoring
│   ├── OutreachMessageService.cs   # Personalized message generation
│   └── EnrichmentService.cs        # CSV data enrichment
├── Agents/
│   └── LeadResearchAgent.cs        # Main orchestration agent
├── Data/
│   ├── icp.json                    # Your ICP configuration
│   ├── enrichment.csv              # Company enrichment data
│   └── sample_newsletter.txt       # Sample newsletter for testing
└── Program.cs                      # Main application entry point
```

## 🛠️ Setup Instructions

### Prerequisites
- .NET 8.0 SDK
- Visual Studio Code
- Azure OpenAI access (optional for full functionality)

### Step 1: Clone the Repository
```bash
git clone https://github.com/HikruOrg/HackatonEquipoA.git
cd HackatonEquipoA/LeadResearchAgent
```

### Step 2: Install Dependencies
```bash
dotnet restore
```

### Step 3: Configure Azure AI (Optional)
For full AI functionality, set environment variables:
```bash
# Windows PowerShell
$env:AZURE_OPENAI_API_KEY="your-api-key"
$env:AZURE_OPENAI_ENDPOINT="https://your-endpoint.openai.azure.com/"
$env:AZURE_OPENAI_DEPLOYMENT="gpt-4"
```

### Step 4: Customize Your ICP
Edit `Data/icp.json` to match your target customer profile:
```json
{
  "industry": ["FinTech", "SaaS", "AI"],
  "stage": ["Seed", "Series A", "Series B"],
  "size": {
    "min_employees": 10,
    "max_employees": 500,
    "funding_min": "1M",
    "funding_max": "50M"
  },
  "geo": ["USA", "Canada", "UK"],
  "tech_hints": ["API", "SaaS", "cloud", "automation"]
}
```

### Step 5: Add Enrichment Data
Update `Data/enrichment.csv` with your company database:
```csv
company,domain,headcount
TechFlow Analytics,techflow.com,45
DataVision Corp,datavision.io,120
```

### Step 6: Run the Application
```bash
dotnet run
```

## 📊 Input/Output Examples

### Input: Newsletter Text
```
TechFlow Analytics raises $8M Series A
San Francisco-based TechFlow Analytics has secured $8M in Series A funding...
```

### Output: Structured JSON
```json
[
  {
    "company": "TechFlow Analytics",
    "round": "Series A",
    "amount": "$8M",
    "sector": "FinTech",
    "HQ": "San Francisco, USA",
    "snippet": "AI-powered financial analytics platform for banks",
    "domain": "techflow.com",
    "headcount": 45,
    "icpScore": 0.85,
    "outreachMessage": "Congrats on your Series A! Love how you're automating risk assessment for banks.\nHikru could help scale your enterprise sales process - would love to explore how we could support your growth."
  }
]
```

## 🤖 AI-Powered Processing Pipeline

1. **Newsletter Analysis**: Semantic Kernel extracts company information using GPT-4
2. **ICP Matching**: Multi-factor scoring algorithm evaluates fit
3. **Data Enrichment**: CSV lookup adds domain and headcount
4. **Outreach Generation**: AI creates personalized messages based on company profile

## 🔧 Technology Stack

- **Framework**: .NET 8.0 Console Application
- **AI Engine**: Microsoft Semantic Kernel
- **AI Service**: Azure OpenAI / Azure AI Foundry
- **Graph API**: Microsoft Graph SDK (for email integration)
- **Data Processing**: Newtonsoft.Json, CsvHelper
- **Architecture**: Agent-based pattern with service layer

## 📈 Scoring Algorithm

Companies are scored on 5 criteria (weighted):
- **Industry Match** (25%): Exact or partial sector alignment
- **Funding Stage** (20%): Round type compatibility  
- **Geography** (15%): Location preferences
- **Company Size** (20%): Employee count and funding amount
- **Tech Stack** (20%): Technology keywords in description

Minimum score threshold: 0.3 (configurable)

## 🎯 Use Cases

1. **VC Deal Flow**: Analyze portfolio newsletters for investment opportunities
2. **Sales Prospecting**: Find companies matching your buyer persona
3. **Competitive Intelligence**: Track funding in your market segment
4. **Partnership Discovery**: Identify potential integration partners

## 🚀 Future Enhancements

- **Microsoft Graph Integration**: Auto-fetch newsletters from Outlook
- **Real-time API Enrichment**: Connect to Clearbit, ZoomInfo APIs
- **CRM Integration**: Push qualified leads to Salesforce/HubSpot
- **Multi-language Support**: Process newsletters in different languages
- **ML Model Fine-tuning**: Custom models for better extraction accuracy

## 🏆 Hackathon Team A

Building the future of intelligent lead research for Hikru! 

---

**Ready to find your perfect customers? Let's get started!** 🎯