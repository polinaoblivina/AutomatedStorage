using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Application.Features.Auth.DTOs;
using ServerAutomatedStorage.Application.Features.Users.DTOs;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Auth.Commands
{
    public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResult>;

    public sealed class LoginValidator : AbstractValidator<LoginCommand>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public sealed class LoginHandler(
        IIdentityService identityService,
        IUserRepository userRepository,
        IJwtTokenGenerator tokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<LoginCommand, AuthResult>
    {
        public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var domainUserId = await identityService.ValidateCredentialsAsync(request.Email, request.Password)
                ?? throw new UnauthorizedException("Неверный email или пароль.");

            var user = await userRepository.GetByIdWithRoleAsync(domainUserId, cancellationToken);
            if (user is null || !user.IsActive)
                throw new UnauthorizedException("Учётная запись отключена.");

            var nowUtc = DateTime.UtcNow;
            await refreshTokenRepository.DeleteExpiredAsync(user.Id, nowUtc, cancellationToken);

            var refreshToken = refreshTokenGenerator.Generate();
            await refreshTokenRepository.AddAsync(
                new RefreshToken(user.Id, refreshToken.TokenHash, refreshToken.ExpiresAtUtc), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var accessToken = tokenGenerator.GenerateToken(user);

            return new AuthResult(
                new LoginResponse(accessToken.Token, accessToken.ExpiresAtUtc, UserDTO.From(user, request.Email)),
                refreshToken);
        }
    }
}

