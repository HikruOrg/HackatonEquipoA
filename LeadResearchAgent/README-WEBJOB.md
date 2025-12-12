# Lead Research Agent - Azure WebJob Configuration

## Environment Variables

### Required Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `MICROSOFT_GRAPH_CLIENT_ID` | Azure AD App Client ID | `14d82eec-204b-4c2f-b7e8-296a70dab67e` |
| `MICROSOFT_GRAPH_TENANT_ID` | Azure AD Tenant ID | `your-tenant-id` |
| `MICROSOFT_GRAPH_CLIENT_SECRET` | Azure AD App Client Secret | `your-client-secret` |
| `MICROSOFT_GRAPH_USER_ID` | User ID or "me" for delegated auth | `me` or `user@domain.com` |
| `AZURE_FOUNDRY_ENDPOINT` | Azure Foundry Agent endpoint | `https://your-foundry-endpoint` |
| `AZURE_FOUNDRY_API_KEY` | Azure Foundry API Key | `your-api-key` |
| `AZURE_FOUNDRY_AGENT_ID` | Azure Foundry Agent/Assistant ID | `your-agent-id` |

### Optional Variables

| Variable | Description | Example | Default |
|----------|-------------|---------|---------|
| `WORKER_EXECUTION_INTERVAL` | Execution interval (format: "HH:mm:ss" or minutes as integer) | `60` or `01:00:00` | Empty (triggered mode) |

## Worker Architecture

### Execution Flow

The Worker uses an **interval-based approach** for continuous execution:

1. **Worker starts**
2. **Checks `WORKER_EXECUTION_INTERVAL`**:
   - **Not configured** ? Executes once and stops (triggered mode)
   - **Configured** ? Enters continuous loop
3. **Executes processing** (load emails ? analyze ? send results)
4. **Waits for configured interval**
5. **Repeats from step 3**

### Key Methods

#### `ParseExecutionInterval()`
- **Responsibility**: Parses and validates execution interval configuration
- **Supported formats**:
  - Minutes as integer: `"60"` (1 hour)
  - TimeSpan format: `"01:00:00"` (1 hour)
  - TimeSpan format: `"00:30:00"` (30 minutes)
- **Returns**: `TimeSpan?` - null if not configured (triggered mode)

#### `ExecuteAsync(CancellationToken stoppingToken)`
- Checks if interval is configured
- **No interval** ? Runs once, stops application
- **Has interval** ? Continuous loop with configured interval delay
- Handles cancellation gracefully

#### `RunProcessingAsync(CancellationToken stoppingToken)`
- Contains the actual business logic
- Loads emails, processes with Foundry agent, sends results
- Handles errors without stopping the continuous loop

## Execution Modes

### Mode 1: Continuous WebJob with Interval (Recommended)
**When to use**: Production environments requiring periodic execution

**Configuration:**
```json
// settings.job
{
  "is_continuous": true,
  "is_singleton": true,
  "stopping_wait_time": 300
}
```

**Environment Variables:**
```bash
# Run every hour
WORKER_EXECUTION_INTERVAL=60

# Or using TimeSpan format
WORKER_EXECUTION_INTERVAL=01:00:00

# Run every 30 minutes
WORKER_EXECUTION_INTERVAL=30

# Run every 2 hours
WORKER_EXECUTION_INTERVAL=120
# Or
WORKER_EXECUTION_INTERVAL=02:00:00
```

**How it works:**
- WebJob runs continuously in Azure
- Worker executes task, then waits for interval
- Automatically retries on next interval if errors occur
- Respects cancellation tokens for graceful shutdown

**Benefits:**
- ? Simple configuration - just set minutes or TimeSpan
- ? Self-contained - no external scheduler needed
- ? Automatic retry on next interval
- ? Easy to adjust interval without redeployment (environment variable)
- ? No missed executions due to timing windows

### Mode 2: Triggered Execution (Development/One-time runs)
**When to use**: Local development, testing, manual execution

**Configuration:**
```bash
# Remove or don't set WORKER_EXECUTION_INTERVAL
# WORKER_EXECUTION_INTERVAL=
```

**How it works:**
- Worker detects no interval configured
- Executes immediately
- Stops after completion

**Use cases:**
- Local development and testing
- One-time manual executions
- Azure WebJob triggered mode (manual trigger in portal)

## Azure WebJob Deployment

### Deploy as Continuous WebJob

**1. Publish the Application**

```bash
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```

**2. Create `settings.job`**

Create this file in your publish directory:
```json
{
  "is_continuous": true,
  "is_singleton": true,
  "stopping_wait_time": 300
}
```

**3. Create Deployment Package**

Zip all files in the publish directory including `settings.job`:
```
publish.zip
??? LeadResearchAgent.exe
??? *.dll
??? settings.job
??? ...other files
```

**4. Configure Environment Variables in Azure**

Navigate to: **Azure Portal ? App Service ? Configuration ? Application Settings**

Add all required environment variables, including:
```
WORKER_EXECUTION_INTERVAL = 60
```

**5. Upload to Azure WebJobs**

1. Go to **Azure Portal ? App Service ? WebJobs**
2. Click **+ Add**
3. Configure:
   - **Name**: LeadResearchAgent
   - **File Upload**: Upload your ZIP
   - **Type**: Continuous
   - **Scale**: Single Instance (is_singleton=true)
4. Click **OK**

**6. Start the WebJob**

The WebJob will start automatically and run continuously with your configured interval.

## Configuration Examples

### Hourly Execution (Most Common)
```bash
# Production environment
WORKER_EXECUTION_INTERVAL=60
```

### Every 2 Hours
```bash
WORKER_EXECUTION_INTERVAL=120
# Or
WORKER_EXECUTION_INTERVAL=02:00:00
```

### Every 30 Minutes
```bash
WORKER_EXECUTION_INTERVAL=30
# Or
WORKER_EXECUTION_INTERVAL=00:30:00
```

### Every 6 Hours
```bash
WORKER_EXECUTION_INTERVAL=360
# Or
WORKER_EXECUTION_INTERVAL=06:00:00
```

### Daily Execution
```bash
WORKER_EXECUTION_INTERVAL=1440
# Or
WORKER_EXECUTION_INTERVAL=24:00:00
```

## Local Development

### Running with Interval (Continuous Mode)
```bash
# Set environment variable
set WORKER_EXECUTION_INTERVAL=60

# Run application
dotnet run
```

**Expected behavior:**
- Executes immediately
- Waits 60 minutes
- Executes again
- Repeats indefinitely until stopped (Ctrl+C)

### Running in Triggered Mode (One-time Execution)
```bash
# Remove or comment out WORKER_EXECUTION_INTERVAL in launchSettings.json
dotnet run
```

**Expected behavior:**
- Executes immediately
- Completes and stops

### Testing with Short Intervals

For testing, use a short interval (e.g., 2 minutes):
```bash
set WORKER_EXECUTION_INTERVAL=2
dotnet run
```

## Monitoring and Logs

### Log Locations
- **Azure Portal**: App Service ? WebJobs ? Logs
- **Kudu Console**: `https://<your-app>.scm.azurewebsites.net` ? LogFiles ? Jobs ? Continuous ? LeadResearchAgent

### Key Log Messages

**Interval mode startup:**
```
Running in interval mode. Execution interval: 01:00:00
Lead Research Agent - starting execution at 2024-01-15 09:00:00
...
Processing completed successfully at 2024-01-15 09:05:23
Next execution in 01:00:00. Waiting...
```

**Triggered mode:**
```
WORKER_EXECUTION_INTERVAL not configured. Running in triggered mode.
Running in triggered mode (no interval configured)
Lead Research Agent - starting execution at 2024-01-15 09:00:00
...
Processing completed successfully at 2024-01-15 09:05:23
```

**Interval parsing:**
```
Execution interval configured: 01:00:00 (60 minutes)
```

## Troubleshooting

### Worker runs but doesn't repeat
**Symptoms:**
- Executes once successfully
- Stops instead of waiting for next interval

**Solution:**
- Verify `WORKER_EXECUTION_INTERVAL` is set in Azure App Service Configuration
- Check WebJob type is "Continuous" not "Triggered"
- Review logs for parsing errors: "Failed to parse WORKER_EXECUTION_INTERVAL"

### Interval not parsing correctly
**Symptoms:**
- Falls back to triggered mode
- Log shows: "Failed to parse WORKER_EXECUTION_INTERVAL"

**Solution:**
- Use simple integer for minutes: `60` (not `"60 minutes"`)
- Or use proper TimeSpan format: `01:00:00` (not `1:00:00`)
- Valid formats:
  - ? `60`
  - ? `01:00:00`
  - ? `00:30:00`
  - ? `60 minutes`
  - ? `1 hour`
  - ? `1h`

### Multiple instances running
**Symptoms:**
- Duplicate email notifications
- Logs from multiple instances

**Solution:**
- Verify `settings.job` has `"is_singleton": true`
- Check WebJob scale setting in Azure Portal
- Ensure App Service Plan supports singleton WebJobs

### WebJob stops unexpectedly
**Symptoms:**
- WebJob status shows "Stopped"
- No recurring execution

**Solution:**
- Check Application Insights / Log Stream for exceptions
- Verify `stopping_wait_time` is sufficient (300 seconds recommended)
- Review error logs in Kudu console
- Ensure App Service Plan has "Always On" enabled

### Authentication errors
**Symptoms:**
- "Failed to retrieve emails" errors
- 401/403 errors in logs

**Solution:**
- Verify all Microsoft Graph environment variables are set correctly
- Check Azure AD App permissions include `Mail.Read` and `Mail.Send`
- Ensure service principal has access to the mailbox
- Test credentials using Graph Explorer

### No emails found repeatedly
**Symptoms:**
- Every execution logs "No LinkSV Pulse emails found"
- Processing completes but no results

**Solution:**
- Verify `MICROSOFT_GRAPH_USER_ID` points to correct mailbox
- Check mailbox actually receives new emails
- Verify Graph API permissions include `Mail.Read`
- Consider adjusting email fetch logic if processing same emails

## Best Practices

1. **Use hourly intervals** for most scenarios (60 minutes)
2. **Enable "Always On"** in App Service to prevent cold starts
3. **Set `is_singleton: true`** to prevent duplicate processing
4. **Use integer minutes format** for simplicity (`60` instead of `01:00:00`)
5. **Monitor logs regularly** to ensure interval execution is working
6. **Test with short intervals locally** (2-5 minutes) before deploying
7. **Consider mailbox check frequency** - don't overwhelm email server
8. **Use Application Insights** for production monitoring and alerts
9. **Set reasonable timeout** in `stopping_wait_time` (300 seconds)
10. **Document your interval choice** for operations team

## Comparison: Interval vs. Scheduled Times

### Interval Approach (Current - Recommended) ?

**Pros:**
- ? Simpler configuration (just one number)
- ? No time window validation needed
- ? Works across time zones automatically
- ? Predictable execution pattern
- ? No missed executions
- ? Easy to adjust without code changes

**Cons:**
- ? Can't specify exact times (e.g., "9 AM daily")
- ? Drift over time (each execution might be slightly later)

**Best for:**
- Periodic processing (every X hours)
- Background tasks that don't need exact timing
- Services that should run continuously

### Scheduled Times Approach (Previous)

**Pros:**
- ? Exact time execution (e.g., "9:00 AM, 2:00 PM")
- ? Aligns with business hours/requirements

**Cons:**
- ? More complex configuration
- ? Requires external scheduler (CRON)
- ? Time window validation logic needed
- ? Can miss executions if timing off
- ? Time zone complications

**Best for:**
- Tasks that must run at specific times
- Business process automation with fixed schedules
- Report generation at specific times

## Migration from Scheduled Times

If migrating from the previous scheduled times approach:

**Old configuration:**
```bash
WORKER_SCHEDULED_TIMES=09:00,14:00,18:00
```

**New configuration (every 5 hours starting from first run):**
```bash
WORKER_EXECUTION_INTERVAL=300
```

**Note:** The interval approach runs from when the service starts, not at fixed clock times.
