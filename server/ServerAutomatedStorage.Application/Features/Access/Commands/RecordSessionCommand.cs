using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;

namespace ServerAutomatedStorage.Application.Features.Access.Commands
{
    public sealed record CellWeighing(Guid CellId, double WeightBefore, double WeightAfter);

    public sealed record RecordSessionCommand(
        string SessionId,
        string CardUid,
        int AgeSeconds,
        IReadOnlyList<CellWeighing> Cells) : IRequest<RecordSessionResponse>;

    public sealed class RecordSessionValidator : AbstractValidator<RecordSessionCommand>
    {
        public const int MaxAgeSeconds = 7 * 24 * 60 * 60;
        public const int MaxCells = 16;

        public RecordSessionValidator()
        {
            RuleFor(x => x.SessionId).NotEmpty().MaximumLength(64);
            RuleFor(x => x.CardUid).NotEmpty().MaximumLength(50);
            RuleFor(x => x.AgeSeconds).InclusiveBetween(0, MaxAgeSeconds);

            RuleFor(x => x.Cells)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(cells => cells.Count <= MaxCells)
                    .WithMessage($"В одной сессии не больше {MaxCells} ячеек.")
                .Must(cells => cells.Select(c => c.CellId).Distinct().Count() == cells.Count)
                    .WithMessage("Ячейки в сессии не должны повторяться.");

            RuleForEach(x => x.Cells).ChildRules(cell =>
            {
                cell.RuleFor(c => c.CellId).NotEmpty();
                cell.RuleFor(c => c.WeightBefore).Must(double.IsFinite);
                cell.RuleFor(c => c.WeightAfter).Must(double.IsFinite);
            });
        }
    }

    public sealed class RecordSessionHandler(
        IUserRepository userRepository,
        ICellRepository cellRepository,
        ITransactionRepository transactionRepository,
        IDeviceSessionRepository deviceSessionRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider) : IRequestHandler<RecordSessionCommand, RecordSessionResponse>
    {
        public const double NoiseThresholdGrams = 5.0;

        public async Task<RecordSessionResponse> Handle(RecordSessionCommand request, CancellationToken cancellationToken)
        {
            if (await deviceSessionRepository.ExistsAsync(request.SessionId, cancellationToken))
                return new RecordSessionResponse(true, "Сессия уже записана", AlreadyRecorded: true, []);

            var user = await userRepository.GetByCardUidAsync(request.CardUid, cancellationToken);
            if (user is null || !user.Can(Permission.OpenCell))
                return new RecordSessionResponse(false, "Нет права доступа", AlreadyRecorded: false, []);

            var occurredAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(-request.AgeSeconds);
            var results = new List<CellSessionResult>();

            foreach (var weighing in request.Cells)
                results.Add(await RecordCellAsync(user, weighing, occurredAtUtc, cancellationToken));

            await deviceSessionRepository.AddAsync(new DeviceSession(request.SessionId, user.Id), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RecordSessionResponse(true, "Сессия записана", AlreadyRecorded: false, results);
        }

        private async Task<CellSessionResult> RecordCellAsync(
            User user, CellWeighing weighing, DateTime occurredAtUtc, CancellationToken cancellationToken)
        {
            var cell = await cellRepository.GetWithItemAsync(weighing.CellId, cancellationToken);
            if (cell is null)
                return new CellSessionResult(weighing.CellId, false, "Ячейка не найдена");

            if (cell.Item is not { } item)
                return new CellSessionResult(weighing.CellId, true, "В ячейке нет товара");

            var weightBefore = Math.Max(0, weighing.WeightBefore);
            var weightAfter = Math.Max(0, weighing.WeightAfter);

            var change = item.AnalyzeWeightChange(weightBefore, weightAfter, NoiseThresholdGrams);
            if (!change.HasChange)
                return new CellSessionResult(weighing.CellId, true, "Изменений нет", item.Name,
                    StockQuantity: item.StockQuantity);

            if (change.Type == TransactionType.Withdrawal)
                item.CorrectStock(Math.Max(0, item.StockQuantity - change.Quantity));
            else
                item.AddStock(change.Quantity);

            await transactionRepository.AddAsync(
                new Transaction(user.Id, item.Id, change.Quantity, weightBefore, weightAfter, change.Type, occurredAtUtc),
                cancellationToken);

            return new CellSessionResult(weighing.CellId, true, "Операция записана", item.Name,
                change.Type.ToString(), change.Quantity, item.StockQuantity);
        }
    }
}
