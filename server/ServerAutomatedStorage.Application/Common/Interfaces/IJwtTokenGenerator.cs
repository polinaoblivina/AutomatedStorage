using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Common.Interfaces
{
    public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
    public interface IJwtTokenGenerator
    {
        AccessToken GenerateToken(User user);
    }
}
