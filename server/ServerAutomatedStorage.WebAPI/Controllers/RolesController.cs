using Microsoft.AspNetCore.Mvc;
using ServerAutomatedStorage.Application.Features.Roles.DTOs;
using ServerAutomatedStorage.Application.Features.Roles.Queries;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.WebAPI.Authorization;

namespace ServerAutomatedStorage.WebAPI.Controllers
{
    [HasPermission(Permission.ManageUsers)]
    public sealed class RolesController : ApiControllerBase
    {
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<RoleDTO>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<RoleDTO>>> GetAll(CancellationToken ct)
            => Ok(await Sender.Send(new GetRolesQuery(), ct));
    }
}
