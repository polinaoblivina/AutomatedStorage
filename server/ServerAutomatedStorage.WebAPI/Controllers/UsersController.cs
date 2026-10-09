using Microsoft.AspNetCore.Mvc;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Application.Features.Roles.Queries;
using ServerAutomatedStorage.Application.Features.Users.Commands;
using ServerAutomatedStorage.Application.Features.Users.DTOs;
using ServerAutomatedStorage.Application.Features.Users.Queries;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.WebAPI.Authorization;
using ServerAutomatedStorage.WebAPI.Contracts;

namespace ServerAutomatedStorage.WebAPI.Controllers
{
    [HasPermission(Permission.ManageUsers)]
    public sealed class UsersController(ICurrentUser currentUser) : ApiControllerBase
    {
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<UserDTO>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<UserDTO>>> GetAll(CancellationToken ct)
            => Ok(await Sender.Send(new GetUsersQuery(), ct));

        [HttpGet("{id:guid}")]
        [ProducesResponseType<UserDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDTO>> GetById(Guid id, CancellationToken ct)
            => Ok(await Sender.Send(new GetUserByIdQuery(id), ct));

        [HttpPost]
        [ProducesResponseType<UserDTO>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserDTO>> Create(CreateUserCommand command, CancellationToken ct)
        {
            var user = await Sender.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, UpdateUserRequest request, CancellationToken ct)
        {
            await Sender.Send(new UpdateUserCommand(id, request.FullName), ct);
            return NoContent();
        }

        [HttpPut("{id:guid}/credentials")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SetCredentials(Guid id, SetCredentialsRequest request, CancellationToken ct)
        {
            await Sender.Send(new SetUserCredentialsCommand(id, request.Email, request.Password), ct);
            return NoContent();
        }

        [HttpPut("{id:guid}/card")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignCard(Guid id, AssignCardRequest request, CancellationToken ct)
        {
            await Sender.Send(new AssignCardCommand(id, request.CardUid), ct);
            return NoContent();
        }

        [HttpDelete("{id:guid}/card")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveCard(Guid id, CancellationToken ct)
        {
            await Sender.Send(new RemoveCardCommand(id), ct);
            return NoContent();
        }

        [HttpPut("{id:guid}/role")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeRole(Guid id, ChangeRoleRequest request, CancellationToken ct)
        {
            await Sender.Send(new ChangeUserRoleCommand(id, request.RoleId, currentUser.RequiredUserId), ct);
            return NoContent();
        }

        [HttpPost("{id:guid}/activate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        {
            await Sender.Send(new ActivateUserCommand(id), ct);
            return NoContent();
        }

        [HttpPost("{id:guid}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        {
            await Sender.Send(new DeactivateUserCommand(id, currentUser.RequiredUserId), ct);
            return NoContent();
        }
    }
}
