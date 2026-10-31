using ServerAutomatedStorage.Domain.Common;
using ServerAutomatedStorage.Domain.Enums;

namespace ServerAutomatedStorage.Domain.Entities
{
    public class Role : Entity
    {
        public string Name { get; private set; } = null!;
        public Permission Permissions { get; private set; }

        private readonly List<User> _users = [];
        public IReadOnlyCollection<User> Users => _users.AsReadOnly();
        private Role() { }
        public Role(string name, Permission permission)
        {
            Name = Guard.AgainstNullOrWhiteSpace(name, nameof(Name));
            Permissions = permission;
        }
        public bool Has(Permission permission) => Permissions.HasFlag(permission);

        public void GrantPermission(Permission permission) => Permissions |= permission;
        public void RevokePermission(Permission permission) => Permissions &= ~permission;

    }
}
