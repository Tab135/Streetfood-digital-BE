# Quest System

## Overview

A **Quest** is a goal a user can enroll in and complete by finishing a set of **Quest Tasks**. Quests are optionally linked to a **Campaign**.

---

## Data Model

### Quest (`BO/Entities/Quest.cs`)

| Field | Description |
|---|---|
| `QuestId` | Primary key |
| `Title` | Display name |
| `Description` | Optional details |
| `ImageUrl` | Banner image |
| `IsActive` | Whether users can enroll |
| `CampaignId` | Optional link to a Campaign |
| `QuestTasks` | Collection of tasks to complete |

### QuestTask (`BO/Entities/QuestTask.cs`)

Each quest has **1 or more tasks**. A task defines *what to do* and *what reward to get*:

| Field | Description |
|---|---|
| `Type` | What action triggers progress (`REVIEW`, `ORDER_AMOUNT`, `VISIT`, `SHARE`, `CREATE_GHOST_PIN`) |
| `TargetValue` | How many times/how much to reach (e.g., write 3 reviews) |
| `Description` | Human-readable goal description |
| `RewardType` | What the user earns (`BADGE`, `POINTS`, `VOUCHER`) |
| `RewardValue` | The reward ID or point amount |

---

## User Flow

```
User enrolls in Quest
        ↓
UserQuest created (status: IN_PROGRESS)
UserQuestTask created per task (currentValue: 0)
        ↓
User performs actions (review, order, visit, share, create ghost pin)
        ↓
QuestProgressService.UpdateProgressAsync() called
        ↓
currentValue++ for matching tasks
        ↓
currentValue >= targetValue?
  → IsCompleted = true → Reward distributed immediately
        ↓
All tasks completed?
  → UserQuest.Status = "COMPLETED"
```

---

## Task Types (`BO/Enums/QuestTaskType.cs`)

| Value | Type | Trigger |
|---|---|---|
| 1 | `REVIEW` | User submits a restaurant review |
| 2 | `ORDER_AMOUNT` | User spends a certain amount on orders |
| 3 | `VISIT` | User visits (checks in to) a vendor |
| 4 | `SHARE` | User shares content |
| 5 | `CREATE_GHOST_PIN` | User creates a ghost pin (unverified branch suggestion) |

---

## Reward Types (`BO/Enums/QuestRewardType.cs`)

| Value | Type | What happens |
|---|---|---|
| 1 | `BADGE` | A `UserBadge` record is created |
| 2 | `POINTS` | `user.Point` is incremented |
| 3 | `VOUCHER` | A `UserVoucher` is created or its quantity +1 |

---

## Key Design Decisions

- **Rewards are per-task**, not per-quest — each task has its own reward, distributed immediately when that task completes.
- **Progress is event-driven** — `QuestProgressService.UpdateProgressAsync()` is called from other services (e.g., order service, review service, branch service) with the task type and increment value.
- **Delete guard** — a quest cannot be deleted while users are enrolled in it.
- **No duplicate enrollment** — enrolling twice in the same quest throws an error.

---

## Progress Trigger Locations

| Task Type | Service | Method |
|---|---|---|
| `CREATE_GHOST_PIN` | `BranchService` | `CreateUserBranchAsync` |

---

## API Endpoints (`StreetFood/Controllers/QuestController.cs`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/quests` | List all quests (admin, paginated, filterable) |
| `GET` | `/api/quests/public` | List active quests (customer-facing) |
| `GET` | `/api/quests/{id}` | Get quest by ID |
| `POST` | `/api/quests` | Create a quest |
| `PUT` | `/api/quests/{id}` | Update a quest |
| `DELETE` | `/api/quests/{id}` | Delete a quest (fails if users enrolled) |
| `POST` | `/api/quests/{id}/enroll` | Enroll current user in a quest |
| `GET` | `/api/quests/my` | Get current user's enrolled quests |
| `GET` | `/api/quests/campaign/{campaignId}/progress` | Get quest progress for a campaign |

---

## Mobile Frontend Types (`src/features/quests/types/quest.ts`)

```ts
export type QuestTaskType = 'REVIEW' | 'ORDER_AMOUNT' | 'VISIT' | 'SHARE' | 'CREATE_GHOST_PIN';
export type QuestRewardType = 'BADGE' | 'POINTS' | 'VOUCHER';
export type UserQuestStatus = 'IN_PROGRESS' | 'COMPLETED' | 'EXPIRED';
```

The `UserQuestProgress` interface tracks per-task progress:

```ts
interface UserQuestTaskProgress {
  currentValue: number;   // how far the user has progressed
  targetValue: number;    // goal to reach
  isCompleted: boolean;
  rewardClaimed: boolean;
}
```
