using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Features.Items.DTOs;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Items.Commands
{
    public sealed record CreateItemCommand(
        Guid CellId,
        string Name,
        double UnitWeightGrams,
        int MinQuantity = 0) : IRequest<ItemDTO>;

    public sealed class CreateItemValidator : AbstractValidator<CreateItemCommand>
    {
        public CreateItemValidator()
        {
            RuleFor(x => x.CellId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.UnitWeightGrams).GreaterThan(0);
            RuleFor(x => x.MinQuantity).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class CreateItemHandler(
        ICellRepository cellRepository,
        IItemRepository itemRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<CreateItemCommand, ItemDTO>
    {
        public async Task<ItemDTO> Handle(CreateItemCommand request, CancellationToken cancellationToken)
        {
            var cell = await cellRepository.GetWithItemAsync(request.CellId, cancellationToken)
                ?? throw new NotFoundException("Ячейка не найдена.");

            var item = new Item(request.Name, request.UnitWeightGrams, request.MinQuantity);
            cell.PlaceItem(item);

            await itemRepository.AddAsync(item, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ItemDTO.From(item);
        }
    }
}
