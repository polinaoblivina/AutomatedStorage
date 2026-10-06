using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Common
{
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }
}
