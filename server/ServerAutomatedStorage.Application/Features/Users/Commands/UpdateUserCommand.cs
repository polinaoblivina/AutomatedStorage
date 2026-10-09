using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Users.Commands
{
    public sealed record UpdateUserCommand(Guid UserId, string FullName) : IRequest;

    public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class UpdateUserHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateUserCommand>
    {
        public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new NotFoundException("Пользователь не найден.");

            user.Rename(request.FullName.Trim());
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
