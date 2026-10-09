namespace ServerAutomatedStorage.WebAPI.Contracts
{
    public sealed record UpdateUserRequest(string FullName);

    public sealed record SetCredentialsRequest(string Email, string Password);

    public sealed record AssignCardRequest(string CardUid);

    public sealed record ChangeRoleRequest(Guid RoleId);
}
