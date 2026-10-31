using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;

namespace ServerAutomatedStorage.Application.Common
{
    public static class PermissionNames
    {
        public static IReadOnlyList<string> Of(Role role) =>
            [.. Enum.GetValues<Permission>()
                .Where(p => p != Permission.None && role.Has(p))
                .Select(p => p.ToString())];
    }
}
