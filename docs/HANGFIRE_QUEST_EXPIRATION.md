# Hangfire — Quest Expiration Jobs

## Overview

When a campaign ends, all `IN_PROGRESS` user quests belonging to that campaign must be marked `EXPIRED`. This is handled by a **Hangfire scheduled job** rather than a polling background service.

---

## How It Works

### 1. Job Scheduling (Campaign Created/Updated)

In `CampaignService`, after saving a campaign:

```csharp
BackgroundJob.Schedule<IQuestExpirationJob>(
    job => job.ExpireCampaignQuestsAsync(campaign.CampaignId),
    campaign.EndDate); // exact datetime to fire
```

This does **not** run anything immediately. It writes a record to the `hangfire.job` table in PostgreSQL saying: *"At this exact datetime, call `ExpireCampaignQuestsAsync(campaignId)`"*.

---

### 2. Hangfire Server

`AddHangfireServer()` starts a background thread inside the app that polls the `hangfire.job` table every few seconds. When it finds a job whose scheduled time has passed, it picks it up and executes it.

---

### 3. Job Execution (At Campaign EndDate)

`QuestExpirationJob.ExpireCampaignQuestsAsync(campaignId)`:

```csharp
var campaign = await _campaignRepository.GetByIdAsync(campaignId);
if (campaign == null) return;
if (campaign.EndDate > DateTime.UtcNow) return; // stale job guard

var userQuests = await _userQuestRepository.GetByUserAndCampaignQuestsInProgressAsync(campaignId);
foreach (var uq in userQuests)
{
    uq.Status = "EXPIRED";
    await _userQuestRepository.UpdateUserQuestAsync(uq);
}
```

---

### 4. Visual Flow

```
CreateCampaign() / UpdateCampaign()
    └─► BackgroundJob.Schedule(...)  ──► writes to hangfire.job table (PostgreSQL)
                                                  │
                                        (time passes until EndDate)
                                                  │
                                        Hangfire server polls DB ◄── runs in background
                                                  │
                                        fires ExpireCampaignQuestsAsync(campaignId)
                                                  │
                                        sets UserQuest.Status = "EXPIRED"
```

---

## Key Behaviors

| Behavior | Detail |
|---|---|
| **Persistence** | Jobs are stored in PostgreSQL — survive app restarts |
| **App was down at EndDate** | Hangfire runs the job immediately when the app comes back up |
| **EndDate is extended** | Old job fires but the stale guard (`EndDate > UtcNow`) aborts it; a new job was scheduled on update |
| **Automatic retries** | If the job throws an exception, Hangfire retries automatically (default: 10 attempts with exponential backoff) |
| **Dashboard** | `http://localhost:<PORT>/hangfire` — view all scheduled, succeeded, and failed jobs |

---

## Hangfire Tables in PostgreSQL

After first app startup, Hangfire auto-creates a `hangfire` schema with these tables:

```sql
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'hangfire';
```

| Table | Purpose |
|---|---|
| `hangfire.job` | All jobs and their state |
| `hangfire.state` | State transition history per job |
| `hangfire.jobqueue` | Queue for jobs ready to execute |
| `hangfire.counter` / `aggregatedcounter` | Statistics |
| `hangfire.hash` / `list` / `set` | Recurring job metadata |
| `hangfire.server` | Registered Hangfire server instances |
| `hangfire.lock` | Distributed locks to prevent duplicate execution |

> No EF Core migration is needed — Hangfire manages its own schema automatically.

---

## Related Files

| File | Role |
|---|---|
| `Service/Interfaces/IQuestExpirationJob.cs` | Interface for the job |
| `Service/QuestExpirationJob.cs` | Job implementation |
| `Service/CampaignService.cs` | Schedules the job on campaign create/update |
| `StreetFood/Program.cs` | Registers Hangfire storage, server, and dashboard |
