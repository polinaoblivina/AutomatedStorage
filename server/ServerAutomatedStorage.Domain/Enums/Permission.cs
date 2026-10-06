using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Enums
{
    [Flags]
    public enum Permission
    {
        None = 0,
        OpenCell = 1 << 0, 
        ManageStock = 1 << 1,
        ViewJournal = 1 << 2,
        ManageUsers = 1 << 3,
        ManageSystem = 1 << 4
    }
}
