using MediatR;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Application.Features.Users.DTOs;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Roles.Queries
{
    public sealed record GetUsersQuery : IRequest<IReadOnlyList<UserDTO>>;

    public sealed class GetUsersHandler(IUserRepository userRepository, IIdentityService identityService)
        : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDTO>>
    {
        public async Task<IReadOnlyList<UserDTO>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await userRepository.GetAllWithRolesAsync(cancellationToken);
            var emails = await identityService.GetEmailsAsync(users.Select(u => u.Id), cancellationToken);

            return users
                .Select(u => UserDTO.From(u, emails.GetValueOrDefault(u.Id)))
                .ToList();
        }
    }
}
