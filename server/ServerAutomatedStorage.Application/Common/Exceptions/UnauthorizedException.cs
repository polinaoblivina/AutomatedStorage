using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Common.Exceptions
{
    public sealed class UnauthorizedException(string message) : Exception(message);
}
