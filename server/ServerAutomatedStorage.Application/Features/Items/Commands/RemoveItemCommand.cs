using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Items.Commands
{
    public sealed record RemoveItemCommand(Guid ItemId) : IRequest<RemoveItemResult>;

    public sealed record RemoveItemResult(bool Archived);

    public sealed class RemoveItemValidator : AbstractValidator<RemoveItemCommand>
    {
        public RemoveItemValidator()
        {
            RuleFor(x => x.ItemId).NotEmpty();
        }
    }

    public sealed class RemoveItemHandler(
        IItemRepository itemRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<RemoveItemCommand, RemoveItemResult>
    {
        public async Task<RemoveItemResult> Handle(RemoveItemCommand request, CancellationToken cancellationToken)
        {
            var item = await itemRepository.GetWithCellAsync(request.ItemId, cancellationToken)
                ?? throw new NotFoundException("Товар не найден.");

            if (!await transactionRepository.AnyForItemAsync(item.Id, cancellationToken))
            {
                itemRepository.Remove(item);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return new RemoveItemResult(false);
            }

            if (item.IsArchived)
                throw new ConflictException("Товар уже в архиве. По нему есть записи журнала, поэтому удалить его нельзя.");

            item.Cell!.RemoveItem();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new RemoveItemResult(true);
        }
    }
}
