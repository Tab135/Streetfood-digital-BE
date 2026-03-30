using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ISettingService
    {
        /// <summary>Returns the raw string value for a setting key, or null if not found.</summary>
        string? GetValue(string name);

        /// <summary>Returns the value parsed as int (returns defaultValue if missing or unparseable).</summary>
        int GetInt(string name, int defaultValue = 0);

        /// <summary>Returns the value parsed as decimal (returns defaultValue if missing or unparseable).</summary>
        decimal GetDecimal(string name, decimal defaultValue = 0m);

        /// <summary>Updates a setting value in both the DB and the in-memory cache immediately.</summary>
        Task UpdateAsync(string name, string newValue);

        /// <summary>Returns all settings (for admin inspection).</summary>
        IReadOnlyList<Setting> GetAll();

        /// <summary>Force-reload all settings from the database.</summary>
        Task ReloadAsync();
    }
}
