using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.Application.Features.Access.Services;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;
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
        WeighingRecorder weighingRecorder,
        IUnitOfWork unitOfWork) : IRequestHandler<RecordSessionCommand, RecordSessionResponse>
    {
        public const double NoiseThresholdGrams = WeighingRecorder.NoiseThresholdGrams;

        public async Task<RecordSessionResponse> Handle(RecordSessionCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByCardUidAsync(request.CardUid, cancellationToken);
            if (user is null || !user.Can(Permission.OpenCell))
                return new RecordSessionResponse(false, "Нет права доступа");

            var cell = await cellRepository.GetWithItemAsync(request.CellId, cancellationToken)
                ?? throw new NotFoundException("Ячейка не найдена.");

            var response = await weighingRecorder.RecordAsync(
                user, cell, request.WeightBefore, request.WeightAfter, occurredAtUtc: null, cancellationToken);

            if (response.Quantity > 0)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return response;
        }
    }
}
