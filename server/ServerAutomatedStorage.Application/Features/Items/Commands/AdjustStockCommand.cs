using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Items.Commands
{
    public sealed record AdjustStockCommand(Guid ItemId, int ActualQuantity, Guid PerformedByUserId) : IRequest;

    public sealed class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
    {
        public AdjustStockValidator()
        {
            RuleFor(x => x.ItemId).NotEmpty();
            RuleFor(x => x.ActualQuantity).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1_000_000);
            RuleFor(x => x.PerformedByUserId).NotEmpty();
        }
    }

    public sealed class AdjustStockHandler(
        IItemRepository itemRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<AdjustStockCommand>
    {
        public async Task Handle(AdjustStockCommand request, CancellationToken cancellationToken)
        {
            var item = await itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
                ?? throw new NotFoundException("Товар не найден.");

            var difference = request.ActualQuantity - item.StockQuantity;
            if (difference == 0)
                return;

            var weightBefore = item.StockQuantity * item.UnitWeightGrams;
            item.CorrectStock(request.ActualQuantity);
            var weightAfter = item.StockQuantity * item.UnitWeightGrams;

            await transactionRepository.AddAsync(
                new Transaction(request.PerformedByUserId, item.Id, Math.Abs(difference),
                    weightBefore, weightAfter, TransactionType.Adjustment),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
