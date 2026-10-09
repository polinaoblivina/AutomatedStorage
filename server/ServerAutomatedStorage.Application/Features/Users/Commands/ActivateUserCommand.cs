using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Users.Commands
{
    public sealed record ActivateUserCommand(Guid UserId) : IRequest;

    public sealed class ActivateUserValidator : AbstractValidator<ActivateUserCommand>
    {
        public ActivateUserValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public sealed class ActivateUserHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<ActivateUserCommand>
    {
        public async Task Handle(ActivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new NotFoundException("Пользователь не найден.");

            user.Activate();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
