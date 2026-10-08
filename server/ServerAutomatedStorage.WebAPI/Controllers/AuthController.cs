using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Application.Features.Auth.Commands;
using ServerAutomatedStorage.Application.Features.Auth.DTO;
using ServerAutomatedStorage.Application.Features.Users.DTOs;
using ServerAutomatedStorage.Application.Features.Users.Queries;
using ServerAutomatedStorage.WebAPI.Contracts;

namespace ServerAutomatedStorage.WebAPI.Controllers
{
    [Authorize]
    public sealed class AuthController(ICurrentUser currentUser) : ApiControllerBase
    {
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Login(LoginCommand command, CancellationToken ct)
            => Ok(await Sender.Send(command, ct));

        [HttpGet("me")]
        [ProducesResponseType<UserDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDTO>> Me(CancellationToken ct)
            => Ok(await Sender.Send(new GetUserByIdQuery(currentUser.RequiredUserId), ct));

        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
        {
            await Sender.Send(new ChangePasswordCommand(
                currentUser.RequiredUserId, request.CurrentPassword, request.NewPassword), ct);
            return NoContent();
        }
    }
}
