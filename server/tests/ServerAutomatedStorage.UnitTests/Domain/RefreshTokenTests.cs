using ServerAutomatedStorage.Domain.Common;
using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.UnitTests.Domain
{
    public class RefreshTokenTests
    {
        private static readonly DateTime Now = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

        private static RefreshToken CreateToken() =>
            new(Guid.NewGuid(), "hash-1", Now.AddDays(7));

        [Fact]
        public void Rotate_ActiveToken_RevokesOldAndKeepsFamily()
        {
            var token = CreateToken();

            var next = token.Rotate("hash-2", Now.AddDays(7), Now);

            Assert.True(token.IsRevoked);
            Assert.Equal(next.Id, token.ReplacedByTokenId);
            Assert.Equal(token.FamilyId, next.FamilyId);
            Assert.Equal(token.UserId, next.UserId);
            Assert.True(next.IsActive(Now));
        }

        [Fact]
        public void Rotate_RevokedToken_Throws()
        {
            var token = CreateToken();
            token.Revoke(Now);

            Assert.Throws<DomainException>(() => token.Rotate("hash-2", Now.AddDays(7), Now));
        }

        [Fact]
        public void IsExpired_AfterExpiry_ReturnsTrue()
        {
            var token = CreateToken();

            Assert.False(token.IsExpired(Now.AddDays(6)));
            Assert.True(token.IsExpired(Now.AddDays(7)));
        }

        [Fact]
        public void WasRotatedWithin_OnlyInsideGracePeriod()
        {
            var token = CreateToken();
            token.Rotate("hash-2", Now.AddDays(7), Now);

            Assert.True(token.WasRotatedWithin(TimeSpan.FromSeconds(30), Now.AddSeconds(10)));
            Assert.False(token.WasRotatedWithin(TimeSpan.FromSeconds(30), Now.AddSeconds(40)));
        }
    }
}
