using ServerAutomatedStorage.Application.Features.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Auth.DTO
{
    public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, UserDTO User);
}
