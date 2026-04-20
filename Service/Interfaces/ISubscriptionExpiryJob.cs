using System.Threading.Tasks;

namespace Service.Interfaces;

public interface ISubscriptionExpiryJob
{
    Task ExpireBranchSubscriptionAsync(int branchId);
    Task ReconcileExpiredSubscriptionsAsync();
}
