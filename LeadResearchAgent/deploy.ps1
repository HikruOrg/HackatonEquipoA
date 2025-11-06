# Git push script for Lead Research Agent
# Run this after completing development

Write-Host "🚀 Pushing Lead Research Agent to GitHub..." -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green

# Navigate to the root of the repository
Set-Location ".."

# Check git status
Write-Host "`n📋 Checking git status..." -ForegroundColor Yellow
git status

# Add all files
Write-Host "`n📦 Adding all files..." -ForegroundColor Yellow
git add .

# Commit with descriptive message
Write-Host "`n💾 Committing changes..." -ForegroundColor Yellow
$commitMessage = "✨ Complete Lead Research Agent Implementation

🎯 Features:
- Newsletter text extraction to JSON
- ICP scoring system
- Personalized outreach generation  
- CSV enrichment (no external APIs)
- Azure AI Foundry + Semantic Kernel integration
- Microsoft Graph SDK ready
- Fallback system for demo

🚀 Ready for hackathon demo!"

git commit -m $commitMessage

# Push to origin
Write-Host "`n🌐 Pushing to GitHub..." -ForegroundColor Yellow
git push origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Successfully pushed to GitHub!" -ForegroundColor Green
    Write-Host "🔗 Repository: https://github.com/HikruOrg/HackatonEquipoA" -ForegroundColor Cyan
} else {
    Write-Host "`n❌ Push failed. Please check your GitHub credentials and try again." -ForegroundColor Red
}

Write-Host "`n🎉 Lead Research Agent deployment complete!" -ForegroundColor Magenta