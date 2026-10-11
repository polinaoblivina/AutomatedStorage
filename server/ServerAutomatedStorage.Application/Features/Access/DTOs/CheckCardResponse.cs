using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Access.DTOs
{
    public enum CardCheckResult
    {
        Granted,
        UnknownCard,
        UserInactive,
        NoPermission
    }

    public sealed record CheckCardResponse(
        bool AccessGranted,
        CardCheckResult Result,
        string Message,
        string? UserName = null);
}
