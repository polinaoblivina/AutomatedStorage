namespace ServerAutomatedStorage.WebAPI.Contracts
{
    public sealed record UpdateCabinetRequest(string Name, string Location);

    public sealed record AddCellRequest(string Name);

    public sealed record RenameCellRequest(string Name);
}
