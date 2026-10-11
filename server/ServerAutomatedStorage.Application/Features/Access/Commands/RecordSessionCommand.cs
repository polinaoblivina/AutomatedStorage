using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;
using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Access.Commands
{
    public sealed record RecordSessionCommand(
        string CardUid,
        Guid CellId,
        double WeightBefore,
        double WeightAfter) : IRequest<RecordSessionResponse>;

    public sealed class RecordSessionValidator : AbstractValidator<RecordSessionCommand>
    {
        public RecordSessionValidator()
        {
            RuleFor(x => x.CardUid).NotEmpty().MaximumLength(50);
            RuleFor(x => x.CellId).NotEmpty();
            RuleFor(x => x.WeightBefore).Must(double.IsFinite);
            RuleFor(x => x.WeightAfter).Must(double.IsFinite);
        }
    }

    public sealed class RecordSessionHandler(
        IUserRepository userRepository,
        ICellRepository cellRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<RecordSessionCommand, RecordSessionResponse>
    {
        public const double NoiseThresholdGrams = 5.0;

        public async Task<RecordSessionResponse> Handle(RecordSessionCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByCardUidAsync(request.CardUid, cancellationToken);
            if (user is null || !user.Can(Permission.OpenCell))
                return new RecordSessionResponse(false, "Нет права доступа");

            var cell = await cellRepository.GetWithItemAsync(request.CellId, cancellationToken)
                ?? throw new NotFoundException("Ячейка не найдена.");

            if (cell.Item is not { } item)
                return new RecordSessionResponse(true, "В ячейке нет товара");

            var weightBefore = Math.Max(0, request.WeightBefore);
            var weightAfter = Math.Max(0, request.WeightAfter);

            var change = item.AnalyzeWeightChange(weightBefore, weightAfter, NoiseThresholdGrams);
            if (!change.HasChange)
                return new RecordSessionResponse(true, "Изменений нет", item.Name, StockQuantity: item.StockQuantity);

            if (change.Type == TransactionType.Withdrawal)
                item.CorrectStock(Math.Max(0, item.StockQuantity - change.Quantity));
            else
                item.AddStock(change.Quantity);

            await transactionRepository.AddAsync(
                new Transaction(user.Id, item.Id, change.Quantity, weightBefore, weightAfter, change.Type),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RecordSessionResponse(
                true, "Операция записана", item.Name, change.Type.ToString(), change.Quantity, item.StockQuantity);
        }
    }
}
