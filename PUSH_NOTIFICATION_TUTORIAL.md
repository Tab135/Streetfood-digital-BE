# How to Add a New Push Notification Event

This guide shows how to add a new Expo push notification event to the backend.
All the infrastructure is already in place — you just need to follow 2 steps.

---

## Step 1: Add a new `NotificationType` (if needed)

**File:** `BO/Entities/NotificationType.cs`

```csharp
public enum NotificationType
{
    NewFeedback = 0,
    VendorReply = 1,
    OrderStatusUpdate = 2,
    // Add yours here:
    PaymentSuccess = 3,
}
```

> Skip this step if you're reusing an existing type.

---

## Step 2: Call `NotifyAsync()` in your service

Find the service method where the event happens and add a single call:

```csharp
// 1. Build the push data payload (this is what the mobile app receives)
var pushData = new
{
    type = "payment_success",       // mobile reads this to decide navigation
    orderId = order.OrderId,        // include IDs for deep linking
    branchName = branch.Name,
};

// 2. Send notification (saves to DB + SignalR + Expo push)
await _notificationService.NotifyAsync(
    recipientUserId,                         // who receives it
    NotificationType.PaymentSuccess,         // enum value
    "Payment Successful",                    // push title
    $"Your payment for order #{order.OrderId} was successful",  // push body
    order.OrderId,                           // referenceId (stored in DB)
    pushData);                               // sent to mobile via Expo
```

That's it. The `NotifyAsync` method handles everything:
- Saves a `Notification` record to the database
- Pushes via **SignalR** (in-app real-time)
- Pushes via **Expo** (background/closed app)

---

## If your service doesn't have `INotificationService` injected

Add it to the constructor:

```csharp
public class YourService : IYourService
{
    private readonly INotificationService _notificationService;

    public YourService(
        // ... existing dependencies ...
        INotificationService notificationService)
    {
        // ... existing assignments ...
        _notificationService = notificationService;
    }
}
```

No DI registration changes needed — `INotificationService` is already registered.

---

## Push Data Payload Contract (with mobile)

The mobile app reads `data.type` to decide which screen to navigate to.
Coordinate with the mobile team when adding new types.

### Existing types:

| `data.type` | Fields | Mobile screen |
|-------------|--------|---------------|
| `order_status` | `orderId`, `branchName`, `orderStatus` | OrderStatus |
| `vendor_reply` | `feedbackId`, `branchId`, `branchName` | ReviewList |

### Example — adding a new type:

```csharp
var pushData = new
{
    type = "promotion",              // new type
    campaignId = campaign.CampaignId,
    branchName = branch.Name,
    discountPercent = 20,
};
```

> Always include a `type` field so mobile knows how to handle it.

---

## Full Example: Adding a "New Order" notification to vendors

```csharp
// In PaymentService.cs, after payment is confirmed:

var branch = await _branchRepository.GetByIdAsync(order.BranchId);
var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId.Value);

var pushData = new
{
    type = "new_order",
    orderId = order.OrderId,
    branchName = branch.Name,
    table = order.Table,
};

await _notificationService.NotifyAsync(
    vendor.UserId,
    NotificationType.NewOrder,    // add this to the enum first
    "New Order",
    $"New order #{order.OrderId} at {branch.Name}",
    order.OrderId,
    pushData);
```

---

## API Endpoints (for mobile)

| Method | Path | Body | Purpose |
|--------|------|------|---------|
| POST | `/api/notifications/register-token` | `{ expoPushToken, platform }` | Register device on login |
| POST | `/api/notifications/remove-token` | `{ expoPushToken }` | Unregister device on logout |
| GET | `/api/notifications` | query: `page`, `pageSize` | Get notification history |
| GET | `/api/notifications/unread-count` | — | Get unread count |
| PUT | `/api/notifications/{id}/read` | — | Mark one as read |
| PUT | `/api/notifications/read-all` | — | Mark all as read |

All endpoints require `Authorization: Bearer <jwt>` header.

---

## Notes

- Push notifications are **best-effort** — if Expo API fails, it logs to stderr but doesn't throw
- A user can have multiple tokens (multiple devices)
- `register-token` does an upsert — safe to call on every app launch
- Call `remove-token` on logout to stop sending to that device
