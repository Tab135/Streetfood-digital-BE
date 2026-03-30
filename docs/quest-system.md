# Quest System

## Overview

A **Quest** is a goal a user can enroll in and complete by finishing a set of **Quest Tasks**. Quests are optionally linked to a **Campaign**.

---

## Data Model

### Quest (`BO/Entities/Quest.cs`)

| Field          | Description                                                         |
| -------------- | ------------------------------------------------------------------- |
| `QuestId`      | Primary key                                                         |
| `Title`        | Display name                                                        |
| `Description`  | Optional details                                                    |
| `ImageUrl`     | Banner image                                                        |
| `IsActive`     | Whether users can enroll                                            |
| `IsStandalone` | `true` = not linked to any campaign; `false` = part of a campaign  |
| `CampaignId`   | Optional link to a Campaign (null when `IsStandalone = true`)       |
| `QuestTasks`   | Collection of tasks to complete                                     |

### QuestTask (`BO/Entities/QuestTask.cs`)

Each quest has **1 or more tasks**. A task defines _what to do_ and _what reward to get_:

| Field         | Description                                                                           |
| ------------- | ------------------------------------------------------------------------------------- |
| `Type`        | What action triggers progress (`REVIEW`, `ORDER_AMOUNT`, `SHARE`, `CREATE_GHOST_PIN`) |
| `TargetValue` | How many times/how much to reach (e.g., write 3 reviews)                              |
| `Description` | Human-readable goal description                                                       |
| `RewardType`  | What the user earns (`BADGE`, `POINTS`, `VOUCHER`)                                    |
| `RewardValue` | The reward ID or point amount                                                         |

---

## User Flow

```
User enrolls in Quest
        ↓
UserQuest created (status: IN_PROGRESS)
UserQuestTask created per task (currentValue: 0)
        ↓
User performs actions (review, order, share, create ghost pin)
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
        ↓ (alternatively)
User manually stops quest
  → UserQuest.Status = "STOPPED"
```

---

## Task Types (`BO/Enums/QuestTaskType.cs`)

| Value | Type               | Trigger                                                 |
| ----- | ------------------ | ------------------------------------------------------- |
| 1     | `REVIEW`           | User submits a restaurant review                        |
| 2     | `ORDER_AMOUNT`     | User spends a certain amount on orders                  |
| 3     | `SHARE`            | User shares content via the app's share function        |
| 4     | `CREATE_GHOST_PIN` | User creates a ghost pin (unverified branch suggestion) |

> **Note:** The `VISIT` task type was removed. Check-in functionality is no longer supported.

---

## Reward Types (`BO/Enums/QuestRewardType.cs`)

| Value | Type      | What happens                                  |
| ----- | --------- | --------------------------------------------- |
| 1     | `BADGE`   | A `UserBadge` record is created               |
| 2     | `POINTS`  | `user.Point` is incremented                   |
| 3     | `VOUCHER` | A `UserVoucher` is created or its quantity +1 |

---

## Key Design Decisions

- **Rewards are per-task**, not per-quest — each task has its own reward, distributed immediately when that task completes.
- **Progress is event-driven** — `QuestProgressService.UpdateProgressAsync()` is called from other services (e.g., order service, review service, branch service) with the task type and increment value.
- **Delete guard** — a quest cannot be deleted while users are enrolled in it.
- **No duplicate enrollment** — enrolling twice in the same quest throws an error.
- **One active standalone quest at a time** — a user cannot enroll in a new standalone quest (`IsStandalone = true`) while another standalone quest is already `IN_PROGRESS`. Campaign-linked quests (`IsStandalone = false`) are not subject to this limit.
- **User can stop a quest** — calling `POST /api/Quest/{questId}/stop` sets `UserQuest.Status = "STOPPED"`. Earned rewards from already-completed tasks are kept; incomplete tasks are abandoned.
- **`IsStandalone` flag** — distinguishes quests that exist independently from those that are part of a campaign. Only system campaigns (not vendor campaigns) may have linked quests.

---

## Progress Trigger Locations

| Task Type          | Service         | Method                  |
| ------------------ | --------------- | ----------------------- |
| `REVIEW`           | `FeedbackService` | `CreateFeedbackAsync` |
| `ORDER_AMOUNT`     | `OrderService`  | `CompleteOrderAsync`    |
| `SHARE`            | `QuestService`  | `ShareStallAsync`       |
| `CREATE_GHOST_PIN` | `BranchService` | `CreateUserBranchAsync` |

---

## API Endpoints (`StreetFood/Controllers/QuestController.cs`)

| Method   | Endpoint                                     | Description                                              |
| -------- | -------------------------------------------- | -------------------------------------------------------- |
| `GET`    | `/api/Quest`                                 | List all quests (admin, paginated, filterable)           |
| `GET`    | `/api/Quest/public`                          | List active quests (customer-facing, paginated)          |
| `GET`    | `/api/Quest/{id}`                            | Get quest by ID                                          |
| `POST`   | `/api/Quest`                                 | Create a quest                                           |
| `PUT`    | `/api/Quest/{id}`                            | Update a quest                                           |
| `DELETE` | `/api/Quest/{id}`                            | Delete a quest (fails if users enrolled)                 |
| `POST`   | `/api/Quest/{id}/enroll`                     | Enroll current user in a quest → returns `UserQuestProgress` |
| `POST`   | `/api/Quest/{id}/stop`                       | Stop (abandon) current user's quest → returns `UserQuestProgress` with status `STOPPED` |
| `GET`    | `/api/Quest/my`                              | Get current user's enrolled quests (filterable by status) |
| `POST`   | `/api/Quest/share/{branchId}`                | Record a stall share (triggers SHARE task progress)      |
| `GET`    | `/api/Quest/campaign/{campaignId}/progress`  | Get quest progress for a campaign                        |
| `GET`    | `/api/Badge/{id}`                            | Get badge detail by ID                                   |
| `GET`    | `/api/vouchers/{id}`                         | Get voucher detail by ID                                 |
| `POST`   | `/api/Quest/{id}/image`                      | Upload quest banner image (multipart/form-data)          |

---

## Mobile Frontend Types (`src/features/quests/types/quest.ts`)

```ts
export type QuestTaskType =
  | 'REVIEW'
  | 'ORDER_AMOUNT'
  | 'SHARE'
  | 'CREATE_GHOST_PIN';

export type QuestRewardType = 'BADGE' | 'POINTS' | 'VOUCHER';

export type UserQuestStatus =
  | 'IN_PROGRESS'
  | 'COMPLETED'
  | 'EXPIRED'
  | 'STOPPED';
```

`QuestResponse` (public quest listing):

```ts
interface QuestResponse {
  questId: number;
  title: string;
  description: string | null;
  imageUrl: string | null;
  startDate: string;
  endDate: string;
  isActive: boolean;
  isStandalone: boolean;        // true = not part of any campaign
  campaignId: number | null;
  createdAt: string;
  updatedAt: string | null;
  taskCount: number;
  tasks: QuestTaskResponse[];
}
```

`UserQuestProgress` tracks per-task progress for an enrolled user:

```ts
interface UserQuestProgress {
  userQuestId: number;
  questId: number;
  title: string;
  description: string | null;
  imageUrl: string | null;
  startDate: string;
  endDate: string;
  isStandalone: boolean;
  status: UserQuestStatus;
  startedAt: string;
  completedAt: string | null;
  campaignId: number | null;
  totalTasks: number;
  completedTasks: number;
  tasks: UserQuestTaskProgress[];
}

interface UserQuestTaskProgress {
  userQuestTaskId: number;
  questTaskId: number;
  type: QuestTaskType;
  targetValue: number;
  description: string | null;
  rewardType: QuestRewardType;
  rewardValue: number;
  currentValue: number;
  isCompleted: boolean;
  completedAt: string | null;
  rewardClaimed: boolean;
}
```

Reward detail types fetched on demand:

```ts
interface QuestBadgeDetail {
  badgeId: number;
  badgeName: string;
  iconUrl: string;
  description: string | null;
}

interface QuestVoucherDetail {
  voucherId: number;
  name: string;
  type: string;
  discountValue: number;
  maxDiscountValue: number | null;
  remain: number;
}
```
