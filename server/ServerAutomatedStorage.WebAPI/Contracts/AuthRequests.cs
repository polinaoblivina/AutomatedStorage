namespace ServerAutomatedStorage.WebAPI.Contracts
{
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
