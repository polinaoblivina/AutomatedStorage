using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;
using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Access.Services
{
    public sealed class WeighingRecorder(ITransactionRepository transactionRepository)
    {
        public const double NoiseThresholdGrams = 5.0;

        public async Task<RecordSessionResponse> RecordAsync(
            User user, Cell cell, double weightBefore, double weightAfter, DateTime? occurredAtUtc,
            CancellationToken cancellationToken)
        {
            if (cell.Item is not { } item)
                return new RecordSessionResponse(true, "В ячейке нет товара");

            weightBefore = Math.Max(0, weightBefore);
            weightAfter = Math.Max(0, weightAfter);

            var change = item.AnalyzeWeightChange(weightBefore, weightAfter, NoiseThresholdGrams);
            if (!change.HasChange)
                return new RecordSessionResponse(true, "Изменений нет", item.Name, StockQuantity: item.StockQuantity);

            if (change.Type == TransactionType.Withdrawal)
                item.CorrectStock(Math.Max(0, item.StockQuantity - change.Quantity));
            else
                item.AddStock(change.Quantity);

            await transactionRepository.AddAsync(
                new Transaction(user.Id, item.Id, change.Quantity, weightBefore, weightAfter, change.Type, occurredAtUtc),
                cancellationToken);

            return new RecordSessionResponse(
                true, "Операция записана", item.Name, change.Type.ToString(), change.Quantity, item.StockQuantity);
        }
    }
}
