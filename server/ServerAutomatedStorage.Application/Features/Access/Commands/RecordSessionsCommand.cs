using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.Application.Features.Access.Services;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;

namespace ServerAutomatedStorage.Application.Features.Access.Commands
{
    public sealed record CellWeighing(Guid CellId, double WeightBefore, double WeightAfter);

    public sealed record RecordSessionsCommand(
        string SessionId,
        string CardUid,
        int AgeSeconds,
        IReadOnlyList<CellWeighing> Cells) : IRequest<RecordSessionsResponse>;

    public sealed class RecordSessionsValidator : AbstractValidator<RecordSessionsCommand>
    {
        public const int MaxAgeSeconds = 7 * 24 * 60 * 60;
        public const int MaxCells = 16;

        public RecordSessionsValidator()
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

    public sealed class RecordSessionsHandler(
        IUserRepository userRepository,
        ICellRepository cellRepository,
        IDeviceSessionRepository deviceSessionRepository,
        WeighingRecorder weighingRecorder,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider) : IRequestHandler<RecordSessionsCommand, RecordSessionsResponse>
    {
        public async Task<RecordSessionsResponse> Handle(RecordSessionsCommand request, CancellationToken cancellationToken)
        {
            if (await deviceSessionRepository.ExistsAsync(request.SessionId, cancellationToken))
                return new RecordSessionsResponse(true, "Сессия уже записана", AlreadyRecorded: true, []);

            var user = await userRepository.GetByCardUidAsync(request.CardUid, cancellationToken);
            if (user is null || !user.Can(Permission.OpenCell))
                return new RecordSessionsResponse(false, "Нет права доступа", AlreadyRecorded: false, []);

            var occurredAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(-request.AgeSeconds);
            var results = new List<CellSessionResult>();

            foreach (var weighing in request.Cells)
            {
                var cell = await cellRepository.GetWithItemAsync(weighing.CellId, cancellationToken);
                if (cell is null)
                {
                    results.Add(CellSessionResult.CellNotFound(weighing.CellId));
                    continue;
                }

                var response = await weighingRecorder.RecordAsync(
                    user, cell, weighing.WeightBefore, weighing.WeightAfter, occurredAtUtc, cancellationToken);
                results.Add(CellSessionResult.From(weighing.CellId, response));
            }

            await deviceSessionRepository.AddAsync(new DeviceSession(request.SessionId, user.Id), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RecordSessionsResponse(true, "Сессия записана", AlreadyRecorded: false, results);
        }
    }
}
