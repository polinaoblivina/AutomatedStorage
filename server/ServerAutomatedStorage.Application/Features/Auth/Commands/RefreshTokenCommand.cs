using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Application.Features.Auth.DTOs;
using ServerAutomatedStorage.Application.Features.Users.DTOs;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Auth.Commands
{
    public sealed record RefreshTokenCommand(string? RefreshToken) : IRequest<AuthResult>;

    public sealed class RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUserRepository userRepository,
        IIdentityService identityService,
        IJwtTokenGenerator tokenGenerator,
        IUnitOfWork unitOfWork) : IRequestHandler<RefreshTokenCommand, AuthResult>
    {
        public static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

        public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                throw new UnauthorizedException("Сессия не найдена. Войдите снова.");

            var stored = await refreshTokenRepository.GetByHashAsync(
                refreshTokenGenerator.Hash(request.RefreshToken), cancellationToken)
                ?? throw new UnauthorizedException("Сессия не найдена. Войдите снова.");

            var nowUtc = DateTime.UtcNow;

            if (stored.WasRotatedWithin(ReuseGracePeriod, nowUtc))
                throw new ConflictException("Сессия уже обновлена параллельным запросом. Повторите запрос.");

            if (stored.IsRevoked)
            {
                await refreshTokenRepository.RevokeFamilyAsync(stored.FamilyId, nowUtc, cancellationToken);
                throw new UnauthorizedException("Сессия отозвана. Войдите снова.");
            }

            if (stored.IsExpired(nowUtc))
                throw new UnauthorizedException("Сессия истекла. Войдите снова.");

            var user = await userRepository.GetByIdWithRoleAsync(stored.UserId, cancellationToken);
            if (user is null || !user.IsActive)
            {
                stored.Revoke(nowUtc);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw new UnauthorizedException("Учётная запись отключена.");
            }

            var newRefreshToken = refreshTokenGenerator.Generate();
            var next = stored.Rotate(newRefreshToken.TokenHash, newRefreshToken.ExpiresAtUtc, nowUtc);
            await refreshTokenRepository.AddAsync(next, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var accessToken = tokenGenerator.GenerateToken(user);
            var emails = await identityService.GetEmailsAsync([user.Id], cancellationToken);

            return new AuthResult(
                new LoginResponse(accessToken.Token, accessToken.ExpiresAtUtc,
                    UserDTO.From(user, emails.GetValueOrDefault(user.Id))),
                newRefreshToken);
        }
    }
}
