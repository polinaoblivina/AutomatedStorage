using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Common.Interfaces
{
    public interface ICurrentUser
    {
        Guid? UserId { get; }
        Guid RequiredUserId { get; }
    }
}
