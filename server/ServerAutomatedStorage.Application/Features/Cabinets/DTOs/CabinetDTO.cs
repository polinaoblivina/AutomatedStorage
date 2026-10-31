using ServerAutomatedStorage.Domain.Entities;

namespace ServerAutomatedStorage.Application.Features.Cabinets.DTOs
{
    public sealed record CabinetDTO(
        Guid Id,
        string Name,
        string Location,
        IReadOnlyList<CellDTO> Cells)
    {
        public static CabinetDTO From(Cabinet cabinet) => new(
            cabinet.Id,
            cabinet.Name,
            cabinet.Location,
            [.. cabinet.Cells.OrderBy(c => c.Name).Select(CellDTO.From)]);
    }
}
