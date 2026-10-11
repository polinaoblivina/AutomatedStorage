using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Application.Features.Auth.Commands;
using ServerAutomatedStorage.Application.Features.Auth.DTOs;
using ServerAutomatedStorage.Application.Features.Users.DTOs;
using ServerAutomatedStorage.Application.Features.Users.Queries;
using ServerAutomatedStorage.WebAPI.Contracts;

namespace ServerAutomatedStorage.WebAPI.Controllers
{
    [Authorize]
    [Route("api/auth")]
    public sealed class AuthController(ICurrentUser currentUser) : ApiControllerBase
    {
        private const string RefreshCookieName = "refresh_token";
        private const string RefreshCookiePath = "/api/auth";

        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Login(LoginCommand command, CancellationToken ct)
        {
            var result = await Sender.Send(command, ct);
            SetRefreshCookie(result.RefreshToken);
            return Ok(result.Response);
        }

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

        private void SetRefreshCookie(NewRefreshToken token) =>
               Response.Cookies.Append(RefreshCookieName, token.Token, new CookieOptions
               {
                   HttpOnly = true,
                   Secure = true,
                   SameSite = SameSiteMode.Strict,
                   Path = RefreshCookiePath,
                   Expires = token.ExpiresAtUtc
               });
    }
}
