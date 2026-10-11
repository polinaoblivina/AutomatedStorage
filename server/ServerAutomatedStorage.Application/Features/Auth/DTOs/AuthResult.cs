using ServerAutomatedStorage.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Auth.DTOs
{
    public sealed record AuthResult(LoginResponse Response, NewRefreshToken RefreshToken);
}
