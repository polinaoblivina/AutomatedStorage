using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Domain.Common;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Users.Commands
{
    public sealed record ChangeUserRoleCommand(Guid UserId, Guid RoleId, Guid PerformedByUserId) : IRequest;

    public sealed class ChangeUserRoleValidator : AbstractValidator<ChangeUserRoleCommand>
    {
        public ChangeUserRoleValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.RoleId).NotEmpty();
            RuleFor(x => x.PerformedByUserId).NotEmpty();
        }
    }

    public sealed class ChangeUserRoleHandler(
        IUserRepository userRepository,
        IRepository<Role> roleRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<ChangeUserRoleCommand>
    {
        public async Task Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == request.PerformedByUserId)
                throw new DomainException("Нельзя изменить собственную роль.");

            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new NotFoundException("Пользователь не найден.");

            var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
                ?? throw new NotFoundException("Роль не найдена.");

            user.ChangeRole(role);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
