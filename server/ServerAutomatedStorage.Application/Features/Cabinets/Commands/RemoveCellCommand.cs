using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Cabinets.Commands
{
    public sealed record RemoveCellCommand(Guid CabinetId, Guid CellId) : IRequest;

    public sealed class RemoveCellValidator : AbstractValidator<RemoveCellCommand>
    {
        public RemoveCellValidator()
        {
            RuleFor(x => x.CabinetId).NotEmpty();
            RuleFor(x => x.CellId).NotEmpty();
        }
    }

    public sealed class RemoveCellHandler(
        ICabinetRepository cabinetRepository,
        IRepository<Cell> cellRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<RemoveCellCommand>
    {
        public async Task Handle(RemoveCellCommand request, CancellationToken cancellationToken)
        {
            var cabinet = await cabinetRepository.GetWithCellsAsync(request.CabinetId, cancellationToken)
                ?? throw new NotFoundException("Шкаф не найден.");

            if (cabinet.Cells.All(c => c.Id != request.CellId))
                throw new NotFoundException("Ячейка не найдена в этом шкафу.");

            var cell = cabinet.RemoveCell(request.CellId);
            cellRepository.Remove(cell);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
