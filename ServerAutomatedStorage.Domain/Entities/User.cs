using ServerAutomatedStorage.Domain.Common;
using ServerAutomatedStorage.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Entities
{
    public class User : Entity
    {
        public string FullName { get; private set; } = null!;
        public string? CardUid { get; private set; }
        public bool IsActive { get; private set; }

        public Guid RoleId { get; private set; }
        public Role Role { get; private set; } = null!;

        private readonly List<Transaction> _transactions = new();
        public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

        private User() { }

        public User(string fullName, Role role, string? cardUid = null)
        {
            FullName = Guard.AgainstNullOrWhiteSpace(fullName, nameof(FullName));
            Role = Guard.AgainstNull(role, nameof(role));
            RoleId = role.Id;
            CardUid = NormalizeCardUid(cardUid);
            IsActive = true;
        }
        public void Rename(string fullName) => FullName = Guard.AgainstNullOrWhiteSpace(fullName, nameof(FullName));

        public void AssignCard(string cardUid) =>
            CardUid = NormalizeCardUid(Guard.AgainstNullOrWhiteSpace(cardUid, nameof(CardUid)));

        public void RemoveCard() => CardUid = null;

        public void ChangeRole(Role role)
        {
            Role = Guard.AgainstNull(role, nameof(role));
            RoleId = role.Id;
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        public bool Can(Permission permission) => IsActive && Role.Has(permission);

        public static string? NormalizeCardUid(string? cardUid) =>
            string.IsNullOrWhiteSpace(cardUid) ? null : cardUid.Trim().ToUpperInvariant();
    }
}
