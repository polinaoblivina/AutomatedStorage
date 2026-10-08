using ServerAutomatedStorage.Application.Common;
using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Users.DTOs
{
    public sealed record UserDTO(
        Guid Id,
        string FullName,
        string? Email,
        string? CardUid,
        bool IsActive,
        Guid RoleId,
        string RoleName,
        IReadOnlyList<string> Permissions,
        DateTime CreatedAtUtc)
    {
        public static UserDTO From(User user, string? email) => new(
            user.Id, user.FullName, email, user.CardUid, user.IsActive,
            user.RoleId, user.Role.Name, PermissionNames.Of(user.Role), user.CreatedAtUtc);
    }
}
