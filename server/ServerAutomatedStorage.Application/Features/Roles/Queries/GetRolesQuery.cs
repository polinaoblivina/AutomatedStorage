using MediatR;
using ServerAutomatedStorage.Application.Features.Roles.DTOs;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;

namespace ServerAutomatedStorage.Application.Features.Roles.Queries
{
    public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleDTO>>;

    public sealed class GetRolesHandler(IRepository<Role> roleRepository)
        : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDTO>>
    {
        public async Task<IReadOnlyList<RoleDTO>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await roleRepository.GetAllAsync(cancellationToken);
            return [.. roles.OrderBy(r => r.Name).Select(RoleDTO.From)];
        }
    }
}
