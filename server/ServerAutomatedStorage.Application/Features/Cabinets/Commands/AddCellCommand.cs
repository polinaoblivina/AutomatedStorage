using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Features.Cabinets.DTOs;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Cabinets.Commands
{
    public sealed record AddCellCommand(Guid CabinetId, string Name) : IRequest<CellDTO>;

    public sealed class AddCellValidator : AbstractValidator<AddCellCommand>
    {
        public AddCellValidator()
        {
            RuleFor(x => x.CabinetId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class AddCellHandler(
        ICabinetRepository cabinetRepository,
        IRepository<Cell> cellRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<AddCellCommand, CellDTO>
    {
        public async Task<CellDTO> Handle(AddCellCommand request, CancellationToken cancellationToken)
        {
            var cabinet = await cabinetRepository.GetWithCellsAsync(request.CabinetId, cancellationToken)
                ?? throw new NotFoundException("Шкаф не найден.");

            var cell = new Cell(request.Name);
            cabinet.AddCell(cell);

            await cellRepository.AddAsync(cell, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return CellDTO.From(cell);
        }
    }
}
