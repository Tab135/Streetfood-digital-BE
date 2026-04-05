using BO.Entities;
using System.Collections.Generic;

namespace Service.Auth
{
    internal static class TemporaryPhoneOtpOverrides
    {
        // Temporary QA shortcut for phone login/create-account flow.
        // Remove this class and its usages when no longer needed.
        public static readonly bool Enabled = true;
        public const string FixedOtp = "123456";

        private static readonly Dictionary<string, Role> PhoneRoleMap = new()
        {
            ["0908835619"] = Role.Vendor,
            ["0908735677"] = Role.Manager,
            ["0908835676"] = Role.Moderator,
            ["0981234567"] = Role.Admin
        };

        public static string? GetForcedOtp(string phoneNumber)
        {
            if (!Enabled)
            {
                return null;
            }

            return PhoneRoleMap.ContainsKey(Normalize(phoneNumber)) ? FixedOtp : null;
        }

        public static bool TryGetRole(string phoneNumber, out Role role)
        {
            role = Role.User;

            if (!Enabled)
            {
                return false;
            }

            return PhoneRoleMap.TryGetValue(Normalize(phoneNumber), out role);
        }

        private static string Normalize(string phoneNumber)
        {
            return phoneNumber.Trim();
        }
    }
}