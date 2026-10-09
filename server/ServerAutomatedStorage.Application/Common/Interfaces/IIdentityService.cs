using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<Guid?> ValidateCredentialsAsync(string email, string password);
        Task<bool> ChangePasswordAsync(Guid domainUserId, string currentPassword, string newPassword);
        Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(IEnumerable<Guid> domainUserIds, CancellationToken ct = default);
        Task CreateIdentityUserAsync(string email, string password, Guid domainUserId);
        Task SetCredentialsAsync(Guid domainUserId, string email, string password);
    }
}
