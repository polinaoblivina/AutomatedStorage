using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Features.Cabinets.DTOs;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Cabinets.Queries
{
    public sealed record GetCabinetByIdQuery(Guid CabinetId) : IRequest<CabinetDTO>;

    public sealed class GetCabinetByIdValidator : AbstractValidator<GetCabinetByIdQuery>
    {
        public GetCabinetByIdValidator()
        {
            RuleFor(x => x.CabinetId).NotEmpty();
        }
    }

    public sealed class GetCabinetByIdHandler(ICabinetRepository cabinetRepository)
        : IRequestHandler<GetCabinetByIdQuery, CabinetDTO>
    {
        public async Task<CabinetDTO> Handle(GetCabinetByIdQuery request, CancellationToken cancellationToken)
        {
            var cabinet = await cabinetRepository.GetWithCellsAsync(request.CabinetId, cancellationToken)
                ?? throw new NotFoundException("Шкаф не найден.");

            return CabinetDTO.From(cabinet);
        }
    }
}
