using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Common
{
    public abstract class Entity
    {
        public Guid Id { get; protected set; }
        public DateTime CreatedAtUtc { get; protected set; }
        protected Entity()
        {
            Id = Guid.NewGuid();
            CreatedAtUtc = DateTime.UtcNow;
        }

    }
}
