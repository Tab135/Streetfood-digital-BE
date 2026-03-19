namespace BO.Entities;

public enum OrderStatus
{
    Pending = 0,
    AwaitingVendorConfirmation = 1,
    Paid = 2,
    Cancelled = 3,
    Complete = 4
}
