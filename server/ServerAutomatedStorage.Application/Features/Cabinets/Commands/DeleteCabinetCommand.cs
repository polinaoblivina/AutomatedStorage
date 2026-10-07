using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Cabinets.Commands
{
    public sealed record DeleteCabinetCommand(Guid CabinetId) : IRequest;

    public sealed class DeleteCabinetValidator : AbstractValidator<DeleteCabinetCommand>
    {
        public DeleteCabinetValidator()
        {
            RuleFor(x => x.CabinetId).NotEmpty();
        }
    }

    public sealed class DeleteCabinetHandler(ICabinetRepository cabinetRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteCabinetCommand>
    {
        public async Task Handle(DeleteCabinetCommand request, CancellationToken cancellationToken)
        {
            var cabinet = await cabinetRepository.GetWithCellsAsync(request.CabinetId, cancellationToken)
                ?? throw new NotFoundException("Шкаф не найден.");

            cabinet.EnsureCanBeDeleted();
            cabinetRepository.Remove(cabinet);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
