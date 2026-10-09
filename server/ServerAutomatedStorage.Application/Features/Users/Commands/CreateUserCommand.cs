using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Application.Features.Users.DTOs;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Users.Commands
{
    public sealed record CreateUserCommand(
        string FullName,
        Guid RoleId,
        string? CardUid = null,
        string? Email = null,
        string? Password = null) : IRequest<UserDTO>;

    public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.RoleId).NotEmpty();
            RuleFor(x => x.CardUid).MaximumLength(50);

            When(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Password), () =>
            {
                RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
                RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
            });
        }
    }

    public sealed class CreateUserHandler(
        IUserRepository userRepository,
        IRepository<Role> roleRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork) : IRequestHandler<CreateUserCommand, UserDTO>
    {
        public async Task<UserDTO> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
                ?? throw new NotFoundException("Роль не найдена.");

            var cardUid = User.NormalizeCardUid(request.CardUid);
            if (cardUid is not null && await userRepository.GetByCardUidAsync(cardUid, cancellationToken) is not null)
                throw new ConflictException("Карта уже привязана к другому пользователю.");

            var user = new User(request.FullName.Trim(), role, cardUid);
            var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

            await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

            await userRepository.AddAsync(user, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (email is not null)
                await identityService.CreateIdentityUserAsync(email, request.Password!, user.Id);

            await transaction.CommitAsync(cancellationToken);

            return UserDTO.From(user, email);
        }
    }
}
