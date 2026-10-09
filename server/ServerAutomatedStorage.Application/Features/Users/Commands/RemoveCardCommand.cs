using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Users.Commands
{
    public sealed record RemoveCardCommand(Guid UserId) : IRequest;

    public sealed class RemoveCardValidator : AbstractValidator<RemoveCardCommand>
    {
        public RemoveCardValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public sealed class RemoveCardHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<RemoveCardCommand>
    {
        public async Task Handle(RemoveCardCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new NotFoundException("Пользователь не найден.");

            user.RemoveCard();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
