using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IQuestProgressService
    {
        Task UpdateProgressAsync(int userId, string taskType, int incrementValue);
    }
}
