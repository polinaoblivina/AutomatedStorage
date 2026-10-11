using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Domain.Common;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Users.Commands
{
    public sealed record DeactivateUserCommand(Guid UserId, Guid PerformedByUserId) : IRequest;

    public sealed class DeactivateUserValidator : AbstractValidator<DeactivateUserCommand>
    {
        public DeactivateUserValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.PerformedByUserId).NotEmpty();
        }
    }

    public sealed class DeactivateUserHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<DeactivateUserCommand>
    {
        public async Task Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == request.PerformedByUserId)
                throw new DomainException("Нельзя отключить собственную учётную запись.");

            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new NotFoundException("Пользователь не найден.");

            user.Deactivate();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await refreshTokenRepository.RevokeAllForUserAsync(user.Id, DateTime.UtcNow, cancellationToken);
        }
    }
}
