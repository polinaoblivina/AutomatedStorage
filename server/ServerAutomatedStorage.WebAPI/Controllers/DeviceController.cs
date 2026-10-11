using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAutomatedStorage.Application.Features.Access.Commands;
using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.WebAPI.Authorization;
using ServerAutomatedStorage.WebAPI.Contracts;
using ServerAutomatedStorage.WebAPI.Services;
using static ServerAutomatedStorage.WebAPI.Contracts.DeviceMonitorResponses;

namespace ServerAutomatedStorage.WebAPI.Controllers
{
    [Authorize(Policy = AuthorizationSetup.DevicePolicy)]
    public sealed class DeviceController(DeviceMonitor monitor, ILogger<DeviceController> logger) : ApiControllerBase
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
            monitor.RecordEvent(DeviceName, DeviceEventType.CardScan, NormalizeCardUid(request.CardUid),
                response.UserName, response.AccessGranted, response.Message);

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
            monitor.RecordEvent(DeviceName, DeviceEventType.Session, NormalizeCardUid(request.CardUid),
                null, response.Success, Describe(response));

            return Ok(response);
        }

        [HttpPost("heartbeat")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Heartbeat(HeartbeatRequest request)
        {
            monitor.RecordHeartbeat(DeviceName, request, HttpContext.Connection.RemoteIpAddress?.ToString());
            return NoContent();
        }

        private static string NormalizeCardUid(string cardUid) =>
            Domain.Entities.User.NormalizeCardUid(cardUid) ?? cardUid;

        private static string Describe(RecordSessionResponse response)
        {
            if (response.Quantity == 0)
                return response.Message;

            var action = response.TransactionType == nameof(TransactionType.Withdrawal) ? "взято" : "положено";
            return $"{response.ItemName}: {action} {response.Quantity} шт.";
        }
    }
}
