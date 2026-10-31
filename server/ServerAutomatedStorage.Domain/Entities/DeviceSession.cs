using ServerAutomatedStorage.Domain.Common;

namespace ServerAutomatedStorage.Domain.Entities
{
    public class DeviceSession : Entity
    {
        public string Key { get; private set; } = null!;
        public Guid UserId { get; private set; }

        private DeviceSession() { }

        public DeviceSession(string key, Guid userId)
        {
            Key = Guard.AgainstNullOrWhiteSpace(key, nameof(Key));
            UserId = Guard.AgainstDefault(userId, nameof(UserId));
        }
    }
}
