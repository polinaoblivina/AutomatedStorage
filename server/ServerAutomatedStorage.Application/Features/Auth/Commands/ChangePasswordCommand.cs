using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Domain.Common;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Auth.Commands
{
    public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;

    public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6)
                .NotEqual(x => x.CurrentPassword).WithMessage("Новый пароль должен отличаться от текущего.");
        }
    }

    public sealed class ChangePasswordHandler(
       IIdentityService identityService,
       IRefreshTokenRepository refreshTokenRepository) : IRequestHandler<ChangePasswordCommand>
    {
        public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            if (!await identityService.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword))
                throw new DomainException("Не удалось сменить пароль. Проверьте текущий пароль.");

            await refreshTokenRepository.RevokeAllForUserAsync(request.UserId, DateTime.UtcNow, cancellationToken);
        }
    }
}
