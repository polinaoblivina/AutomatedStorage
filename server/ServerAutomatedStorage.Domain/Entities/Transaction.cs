using ServerAutomatedStorage.Domain.Common;
using ServerAutomatedStorage.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Entities
{
    public class Transaction : Entity
    {
        public Guid UserId { get; private set; }
        public User? User { get; private set; }

        public Guid ItemId { get; private set; }
        public Item? Item { get; private set; }

        public int Quantity { get; private set; }
        public double WeightBefore { get; private set; }
        public double WeightAfter { get; private set; }
        public TransactionType Type { get; private set; }

        private Transaction() { }

        public Transaction(Guid userId, Guid itemId, int quantity, double weightBefore, double weightAfter, TransactionType type, DateTime? occurredAtUtc = null)
        {
            UserId = Guard.AgainstDefault(userId, nameof(userId));
            ItemId = Guard.AgainstDefault(itemId, nameof(itemId));
            Quantity = Guard.AgainstNegativeOrZero(quantity, nameof(quantity));
            WeightBefore = Guard.AgainstNegative(weightBefore, nameof(weightBefore));
            WeightAfter = Guard.AgainstNegative(weightAfter, nameof(weightAfter));
            Type = type;

            if (occurredAtUtc is { } occurredAt)
                CreatedAtUtc = occurredAt;
        }

    }
}

