using MediatR;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Auth.Commands
{
    public sealed record LogoutCommand(string? RefreshToken) : IRequest;

    public sealed class LogoutHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUnitOfWork unitOfWork) : IRequestHandler<LogoutCommand>
    {
        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return;

            var stored = await refreshTokenRepository.GetByHashAsync(
                refreshTokenGenerator.Hash(request.RefreshToken), cancellationToken);
            if (stored is null)
                return;

            stored.Revoke(DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
