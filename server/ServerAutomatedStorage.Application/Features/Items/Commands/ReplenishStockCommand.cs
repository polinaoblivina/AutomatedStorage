using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;
using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Items.Commands
{
    public sealed record ReplenishStockCommand(Guid ItemId, int Quantity, Guid PerformedByUserId) : IRequest;

    public sealed class ReplenishStockValidator : AbstractValidator<ReplenishStockCommand>
    {
        public ReplenishStockValidator()
        {
            RuleFor(x => x.ItemId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100_000);
            RuleFor(x => x.PerformedByUserId).NotEmpty();
        }
    }

    public sealed class ReplenishStockHandler(
        IItemRepository itemRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<ReplenishStockCommand>
    {
        public async Task Handle(ReplenishStockCommand request, CancellationToken cancellationToken)
        {
            var item = await itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
                ?? throw new NotFoundException("Товар не найден.");

            if (item.IsArchived)
                throw new ConflictException("Товар в архиве. Сначала разместите его в ячейке.");

            var weightBefore = item.StockQuantity * item.UnitWeightGrams;
            item.AddStock(request.Quantity);
            var weightAfter = item.StockQuantity * item.UnitWeightGrams;

            await transactionRepository.AddAsync(
                new Transaction(request.PerformedByUserId, item.Id, request.Quantity,
                    weightBefore, weightAfter, TransactionType.Replenishment),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
