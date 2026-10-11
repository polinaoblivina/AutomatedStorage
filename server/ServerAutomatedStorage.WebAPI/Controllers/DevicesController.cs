using Microsoft.AspNetCore.Mvc;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.WebAPI.Authorization;
using ServerAutomatedStorage.WebAPI.Services;
using static ServerAutomatedStorage.WebAPI.Contracts.DeviceMonitorResponses;

namespace ServerAutomatedStorage.WebAPI.Controllers
{
    [HasPermission(Permission.ViewJournal)]
    public sealed class DevicesController(DeviceMonitor monitor) : ApiControllerBase
    {
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<DeviceStatusDTO>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult<IReadOnlyList<DeviceStatusDTO>> GetStatuses()
            => Ok(monitor.GetStatuses());

        [HttpGet("events")]
        [ProducesResponseType<IReadOnlyList<DeviceEventDTO>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult<IReadOnlyList<DeviceEventDTO>> GetEvents(
            [FromQuery] DateTime? sinceUtc, [FromQuery] DeviceEventType? type, [FromQuery] int take = 20)
            => Ok(monitor.GetEvents(sinceUtc, type, Math.Clamp(take, 1, 100)));
    }
}
