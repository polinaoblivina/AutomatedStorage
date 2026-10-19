using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAutomatedStorage.Application.Features.Access.Commands;
using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.WebAPI.Authorization;
using ServerAutomatedStorage.WebAPI.Contracts;
using ServerAutomatedStorage.WebAPI.Services;
using Microsoft.AspNetCore.SignalR;
using ServerAutomatedStorage.WebAPI.Hubs;

using static ServerAutomatedStorage.WebAPI.Contracts.DeviceMonitorResponses;

namespace ServerAutomatedStorage.WebAPI.Controllers
{
    [Authorize(Policy = AuthorizationSetup.DevicePolicy)]
    public sealed class DeviceController(
           DeviceMonitor monitor,
           IHubContext<DevicesHub, IDevicesClient> hub,
           ILogger<DeviceController> logger) : ApiControllerBase
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
            var deviceEvent = monitor.RecordEvent(DeviceName, DeviceEventType.CardScan,
                   NormalizeCardUid(request.CardUid), response.UserName, response.AccessGranted, response.Message);
            await PushAsync(deviceEvent);

            return Ok(response);
        }

        [HttpPost("sessions")]
        [ProducesResponseType<RecordSessionResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<RecordSessionResponse>> RecordSession(RecordSessionCommand command, CancellationToken ct)
        {
            var response = await Sender.Send(command, ct);

            logger.LogInformation("Устройство {Device}: сессия {SessionId}, карта {CardUid}, ячеек {Count} -> {Message}",
                DeviceName, command.SessionId, command.CardUid, command.Cells.Count, response.Message);

            if (!response.AlreadyRecorded)
            {
                var deviceEvent = monitor.RecordEvent(DeviceName, DeviceEventType.Session,
                    NormalizeCardUid(command.CardUid), null, response.Success, Describe(response));
                await PushAsync(deviceEvent);
            }

            return Ok(response);
        }

        [HttpPost("heartbeat")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Heartbeat(HeartbeatRequest request)
        {
            monitor.RecordHeartbeat(DeviceName, request, HttpContext.Connection.RemoteIpAddress?.ToString());
            await PushAsync();
            return NoContent();
        }

        private async Task PushAsync(DeviceEventDTO? deviceEvent = null)
        {
            if (deviceEvent is not null)
                await hub.Clients.All.DeviceEventRecorded(deviceEvent);

            await hub.Clients.All.DeviceStatusChanged(monitor.GetStatus(DeviceName));
        }

        private static string NormalizeCardUid(string cardUid) =>
            Domain.Entities.User.NormalizeCardUid(cardUid) ?? cardUid;

        private static string Describe(RecordSessionResponse response)
        {
            var changes = response.Cells
                .Where(c => c.Quantity > 0)
                .Select(Describe)
                .ToList();

            return changes.Count > 0 ? string.Join("; ", changes) : response.Message;
        }

        private static string Describe(CellSessionResult cell)
        {
            var action = cell.TransactionType == nameof(TransactionType.Withdrawal) ? "взято" : "положено";
            return $"{cell.ItemName}: {action} {cell.Quantity} шт.";
        }
    }
}
