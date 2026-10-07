using MediatR;
using ServerAutomatedStorage.Application.Features.Cabinets.DTOs;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Cabinets.Queries
{
    public sealed record GetCabinetsQuery : IRequest<IReadOnlyList<CabinetDTO>>;
    public sealed class GetCabinetsHandler(ICabinetRepository cabinetRepository)
        : IRequestHandler<GetCabinetsQuery, IReadOnlyList<CabinetDTO>>
    {
        public async Task<IReadOnlyList<CabinetDTO>> Handle(GetCabinetsQuery request, CancellationToken cancellationToken)
        {
            var cabinets = await cabinetRepository.GatAllWithCellsAsync(cancellationToken);
            return cabinets.Select(CabinetDTO.From).ToList();
        }
    }
}
