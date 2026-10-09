using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Users.Commands
{
    public sealed record AssignCardCommand(Guid UserId, string CardUid) : IRequest;

    public sealed class AssignCardValidator : AbstractValidator<AssignCardCommand>
    {
        public AssignCardValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.CardUid).NotEmpty().MaximumLength(50);
        }
    }

    public sealed class AssignCardHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<AssignCardCommand>
    {
        public async Task Handle(AssignCardCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new NotFoundException("Пользователь не найден.");

            var owner = await userRepository.GetByCardUidAsync(request.CardUid, cancellationToken);
            if (owner is not null && owner.Id != user.Id)
                throw new ConflictException("Карта уже привязана к другому пользователю.");

            user.AssignCard(request.CardUid);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
