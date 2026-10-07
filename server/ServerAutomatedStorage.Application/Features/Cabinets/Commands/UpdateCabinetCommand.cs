using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Features.Cabinets.DTOs;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Cabinets.Commands
{
    public sealed record UpdateCabinetCommand(Guid CabinetId, string Name, string Location) : IRequest<CabinetDTO>;

    public sealed class UpdateCabinetValidator : AbstractValidator<UpdateCabinetCommand>
    {
        public UpdateCabinetValidator()
        {
            RuleFor(x => x.CabinetId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Location).NotEmpty().MaximumLength(300);
        }
    }

    public sealed class UpdateCabinetHandler(ICabinetRepository cabinetRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateCabinetCommand, CabinetDTO>
    {
        public async Task<CabinetDTO> Handle(UpdateCabinetCommand request, CancellationToken cancellationToken)
        {
            var cabinet = await cabinetRepository.GetWithCellsAsync(request.CabinetId, cancellationToken)
                ?? throw new NotFoundException("Шкаф не найден.");

            cabinet.UpdateInfo(request.Name, request.Location);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return CabinetDTO.From(cabinet);
        }
    }
}
