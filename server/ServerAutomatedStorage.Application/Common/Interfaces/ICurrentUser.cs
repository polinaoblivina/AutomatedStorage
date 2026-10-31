namespace ServerAutomatedStorage.Application.Common.Interfaces
{
    public interface ICurrentUser
    {
        Guid? UserId { get; }
        Guid RequiredUserId { get; }
    }
}
