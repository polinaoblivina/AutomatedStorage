using ServerAutomatedStorage.Application.Common;
using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Roles.DTOs
{
    public sealed record RoleDTO(Guid Id, string Name, IReadOnlyList<string> Permissions)
    {
        public static RoleDTO From(Role role) => new(role.Id, role.Name, PermissionNames.Of(role));
    }
}
