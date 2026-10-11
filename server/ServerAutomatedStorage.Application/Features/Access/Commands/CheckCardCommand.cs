using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Access.Commands
{
    public sealed record CheckCardCommand(string CardUid) : IRequest<CheckCardResponse>;

    public sealed class CheckCardValidator : AbstractValidator<CheckCardCommand>
    {
        public CheckCardValidator()
        {
            RuleFor(x => x.CardUid).NotEmpty().MaximumLength(50);
        }
    }

    public sealed class CheckCardHandler(IUserRepository userRepository)
        : IRequestHandler<CheckCardCommand, CheckCardResponse>
    {
        public async Task<CheckCardResponse> Handle(CheckCardCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByCardUidAsync(request.CardUid, cancellationToken);

            if (user is null)
                return new CheckCardResponse(false, CardCheckResult.UnknownCard, "Карта не зарегистрирована");
            if (!user.IsActive)
                return new CheckCardResponse(false, CardCheckResult.UserInactive, "Пользователь отключён", user.FullName);
            if (!user.Can(Permission.OpenCell))
                return new CheckCardResponse(false, CardCheckResult.NoPermission, "Нет права доступа", user.FullName);

            return new CheckCardResponse(true, CardCheckResult.Granted, "Доступ разрешён", user.FullName);
        }
    }
}
