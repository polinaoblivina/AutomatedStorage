using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Features.Items.DTOs;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Items.Commands
{
    public sealed record MoveItemCommand(Guid ItemId, Guid CellId) : IRequest<ItemDTO>;

    public sealed class MoveItemValidator : AbstractValidator<MoveItemCommand>
    {
        public MoveItemValidator()
        {
            RuleFor(x => x.ItemId).NotEmpty();
            RuleFor(x => x.CellId).NotEmpty();
        }
    }

    public sealed class MoveItemHandler(
        IItemRepository itemRepository,
        ICellRepository cellRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<MoveItemCommand, ItemDTO>
    {
        public async Task<ItemDTO> Handle(MoveItemCommand request, CancellationToken cancellationToken)
        {
            var item = await itemRepository.GetWithCellAsync(request.ItemId, cancellationToken)
                ?? throw new NotFoundException("Товар не найден.");

            if (item.CellId == request.CellId)
                return ItemDTO.From(item);

            var target = await cellRepository.GetWithItemAsync(request.CellId, cancellationToken)
                ?? throw new NotFoundException("Ячейка не найдена.");

            await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

            if (item.Cell is { } source)
            {
                source.RemoveItem();
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            target.PlaceItem(item);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ItemDTO.From(item);

            //проверка что вторая чейка пустая
        }
    }
}
