using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Application.Features.Users.DTOs;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Users.Queries
{
    public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDTO>;

    public sealed class GetUserByIdValidator : AbstractValidator<GetUserByIdQuery>
    {
        public GetUserByIdValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public sealed class GetUserByIdHandler(IUserRepository userRepository, IIdentityService identityService)
        : IRequestHandler<GetUserByIdQuery, UserDTO>
    {
        public async Task<UserDTO> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdWithRoleAsync(request.UserId, cancellationToken)
                ?? throw new NotFoundException("Пользователь не найден.");

            var emails = await identityService.GetEmailsAsync([user.Id], cancellationToken);

            return UserDTO.From(user, emails.GetValueOrDefault(user.Id));
        }
    }
}
