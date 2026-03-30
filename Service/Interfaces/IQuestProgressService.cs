using BO.Enums;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IQuestProgressService
    {
        Task UpdateProgressAsync(int userId, QuestTaskType taskType, int incrementValue);
    }
}
