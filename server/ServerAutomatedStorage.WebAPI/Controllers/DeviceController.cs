using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAutomatedStorage.Application.Features.Access.Commands;
using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.WebAPI.Authorization;
using ServerAutomatedStorage.WebAPI.Contracts;

namespace ServerAutomatedStorage.WebAPI.Controllers
{
    [Authorize(Policy = AuthorizationSetup.DevicePolicy)]
    public sealed class DeviceController(ILogger<DeviceController> logger) : ApiControllerBase
    {
        private string DeviceName => User.Identity?.Name ?? "unknown";

        [HttpPost("check-card")]
        [ProducesResponseType<CheckCardResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<CheckCardResponse>> CheckCard(CheckCardRequest request, CancellationToken ct)
        {
            var response = await Sender.Send(new CheckCardCommand(request.CardUid), ct);

            logger.LogInformation("Устройство {Device}: карта {CardUid} -> {Result}",
                DeviceName, request.CardUid, response.Result);

            return Ok(response);
        }

        [HttpPost("session")]
        [ProducesResponseType<RecordSessionResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RecordSessionResponse>> RecordSession(
            RecordSessionRequest request, CancellationToken ct)
        {
            var response = await Sender.Send(new RecordSessionCommand(
                request.CardUid, request.CellId, request.WeightBefore, request.WeightAfter), ct);

            logger.LogInformation("Устройство {Device}: карта {CardUid}, ячейка {CellId} -> {Message}",
                DeviceName, request.CardUid, request.CellId, response.Message);

            return Ok(response);
        }
    }
}
