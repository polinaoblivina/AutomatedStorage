using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Features.Cabinets.DTOs;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Cabinets.Commands
{
    public sealed record CreateCabinetCommand(string Name, string Location) : IRequest<CabinetDTO>;

    public sealed class CreateCabinetValidator : AbstractValidator<CreateCabinetCommand>
    {
        public CreateCabinetValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Location).NotEmpty().MaximumLength(300);
        }
    }

    public sealed class CreateCabinetHandler(ICabinetRepository cabinetRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<CreateCabinetCommand, CabinetDTO>
    {
        public async Task<CabinetDTO> Handle(CreateCabinetCommand request, CancellationToken cancellationToken)
        {
            var cabinet = new Cabinet(request.Name, request.Location);

            await cabinetRepository.AddAsync(cabinet, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return CabinetDTO.From(cabinet);
        }
    }
}
