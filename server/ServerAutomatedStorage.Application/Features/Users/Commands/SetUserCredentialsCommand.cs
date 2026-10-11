using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Users.Commands
{
    public sealed record SetUserCredentialsCommand(Guid UserId, string Email, string Password) : IRequest;

    public sealed class SetUserCredentialsValidator : AbstractValidator<SetUserCredentialsCommand>
    {
        public SetUserCredentialsValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        }
    }

    public sealed class SetUserCredentialsHandler(
        IUserRepository userRepository,
        IIdentityService identityService,
        IRefreshTokenRepository refreshTokenRepository)
        : IRequestHandler<SetUserCredentialsCommand>
    {
        public async Task Handle(SetUserCredentialsCommand request, CancellationToken cancellationToken)
        {
            if (await userRepository.GetByIdAsync(request.UserId, cancellationToken) is null)
                throw new NotFoundException("Пользователь не найден.");

            await identityService.SetCredentialsAsync(request.UserId, request.Email.Trim(), request.Password);
            await refreshTokenRepository.RevokeAllForUserAsync(request.UserId, DateTime.UtcNow, cancellationToken);
        }
    }
}
