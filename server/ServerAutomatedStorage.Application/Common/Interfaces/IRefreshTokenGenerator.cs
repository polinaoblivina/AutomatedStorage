using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Common.Interfaces
{
    public sealed record NewRefreshToken(string Token, string TokenHash, DateTime ExpiresAtUtc);

    public interface IRefreshTokenGenerator
    {
        NewRefreshToken Generate();
        string Hash(string token);
    }
}
