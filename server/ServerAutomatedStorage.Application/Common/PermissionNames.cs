using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Common
{
    public static class PermissionNames
    {
        public static IReadOnlyList<string> Of(Role role) =>
            Enum.GetValues<Permission>()
                .Where(p => p != Permission.None && role.Has(p))
                .Select(p => p.ToString())
                .ToList();
    }
}
