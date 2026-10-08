using Microsoft.AspNetCore.Identity;
using ServerAutomatedStorage.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Identity
{
    public sealed class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
    {
        public async Task<Guid?> ValidateCredentialsAsync(string email, string password)
        {
            var appUser = await userManager.FindByEmailAsync(email);
            if (appUser is null)
                return null;

            return await userManager.CheckPasswordAsync(appUser, password) ? appUser.DomainUserId : null;
        }

        public async Task<bool> ChangePasswordAsync(Guid domainUserId, string currentPassword, string newPassword)
        {
            var appUser = await userManager.Users.FirstOrDefaultAsync(u => u.DomainUserId == domainUserId);
            if (appUser is null)
                return false;

            var result = await userManager.ChangePasswordAsync(appUser, currentPassword, newPassword);
            return result.Succeeded;
        }

        public async Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(
            IEnumerable<Guid> domainUserIds, CancellationToken ct = default)
        {
            var ids = domainUserIds.ToList();

            return await userManager.Users
                .Where(u => ids.Contains(u.DomainUserId) && u.Email != null)
                .ToDictionaryAsync(u => u.DomainUserId, u => u.Email!, ct);
        }
    }
}
