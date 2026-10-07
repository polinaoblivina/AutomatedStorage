using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Common.Exceptions
{
    public sealed class NotFoundException(string message) : Exception(message);
}
