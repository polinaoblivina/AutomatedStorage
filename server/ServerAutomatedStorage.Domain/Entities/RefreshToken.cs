using ServerAutomatedStorage.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Entities
{
    public class RefreshToken : Entity
    {
        public Guid UserId { get; private set; }
        public string TokenHash { get; private set; } = null!;
        public Guid FamilyId { get; private set; }
        public DateTime ExpiresAtUtc { get; private set; }
        public DateTime? RevokedAtUtc { get; private set; }
        public Guid? ReplacedByTokenId { get; private set; }

        private RefreshToken() { }

        public RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc, Guid? familyId = null)
        {
            UserId = Guard.AgainstDefault(userId, nameof(UserId));
            TokenHash = Guard.AgainstNullOrWhiteSpace(tokenHash, nameof(TokenHash));
            ExpiresAtUtc = expiresAtUtc;
            FamilyId = familyId ?? Guid.NewGuid();
        }

        public bool IsRevoked => RevokedAtUtc is not null;
        public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;
        public bool IsActive(DateTime nowUtc) => !IsRevoked && !IsExpired(nowUtc);

        public bool WasRotatedWithin(TimeSpan period, DateTime nowUtc) =>
            ReplacedByTokenId is not null && nowUtc - RevokedAtUtc < period;

        public void Revoke(DateTime nowUtc) => RevokedAtUtc ??= nowUtc;

        public RefreshToken Rotate(string newTokenHash, DateTime newExpiresAtUtc, DateTime nowUtc)
        {
            if (!IsActive(nowUtc))
                throw new DomainException("Нельзя обновить отозванный или истёкший токен.");

            var next = new RefreshToken(UserId, newTokenHash, newExpiresAtUtc, FamilyId);
            Revoke(nowUtc);
            ReplacedByTokenId = next.Id;
            return next;
        }
    }
}
