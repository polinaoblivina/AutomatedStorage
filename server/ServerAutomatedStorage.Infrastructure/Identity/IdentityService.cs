using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Domain.Common;
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
            var appUser = await FindByDomainIdAsync(domainUserId);
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

        public async Task CreateIdentityUserAsync(string email, string password, Guid domainUserId)
        {
            await EnsureEmailIsFreeAsync(email, domainUserId);

            var appUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DomainUserId = domainUserId
            };

            EnsureSucceeded(await userManager.CreateAsync(appUser, password), "Не удалось создать учётную запись");
        }

        private Task<ApplicationUser?> FindByDomainIdAsync(Guid domainUserId) =>
            userManager.Users.FirstOrDefaultAsync(u => u.DomainUserId == domainUserId);

        private async Task EnsureEmailIsFreeAsync(string email, Guid domainUserId)
        {
            var owner = await userManager.FindByEmailAsync(email);
            if (owner is not null && owner.DomainUserId != domainUserId)
                throw new ConflictException($"Email {email} уже используется другим пользователем.");
        }

        private static void EnsureSucceeded(IdentityResult result, string message)
        {
            if (result.Succeeded)
                return;

            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new DomainException($"{message}: {errors}");
        }

        public async Task SetCredentialsAsync(Guid domainUserId, string email, string password)
        {
            var appUser = await FindByDomainIdAsync(domainUserId);
            if (appUser is null)
            {
                await CreateIdentityUserAsync(email, password, domainUserId);
                return;
            }

            await EnsureEmailIsFreeAsync(email, domainUserId);

            if (!string.Equals(appUser.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                appUser.Email = email;
                appUser.UserName = email;
                EnsureSucceeded(await userManager.UpdateAsync(appUser), "Не удалось изменить email");
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(appUser);
            EnsureSucceeded(await userManager.ResetPasswordAsync(appUser, token, password), "Не удалось задать пароль");
        }
    }
}
