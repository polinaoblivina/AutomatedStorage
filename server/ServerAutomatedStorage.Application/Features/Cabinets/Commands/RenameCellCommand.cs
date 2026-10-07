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
    public sealed record RenameCellCommand(Guid CabinetId, Guid CellId, string Name) : IRequest<CellDTO>;

    public sealed class RenameCellValidator : AbstractValidator<RenameCellCommand>
    {
        public RenameCellValidator()
        {
            RuleFor(x => x.CabinetId).NotEmpty();
            RuleFor(x => x.CellId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class RenameCellHandler(ICabinetRepository cabinetRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<RenameCellCommand, CellDTO>
    {
        public async Task<CellDTO> Handle(RenameCellCommand request, CancellationToken cancellationToken)
        {
            var cabinet = await cabinetRepository.GetWithCellsAsync(request.CabinetId, cancellationToken)
                ?? throw new NotFoundException("Шкаф не найден.");

            var cell = cabinet.Cells.FirstOrDefault(c => c.Id == request.CellId)
                ?? throw new NotFoundException("Ячейка не найдена в этом шкафу.");

            cabinet.RenameCell(cell.Id, request.Name);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return CellDTO.From(cell);
        }
    }
}
