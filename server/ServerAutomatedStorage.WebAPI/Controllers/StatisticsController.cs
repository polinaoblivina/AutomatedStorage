using Microsoft.AspNetCore.Mvc;
using ServerAutomatedStorage.Application.Features.Transactions.DTOs;
using ServerAutomatedStorage.Application.Features.Transactions.Queries;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.WebAPI.Authorization;

namespace ServerAutomatedStorage.WebAPI.Controllers
{
    [HasPermission(Permission.ViewJournal)]
    public sealed class StatisticsController : ApiControllerBase
    {
        [HttpGet("dashboard")]
        [ProducesResponseType<DashboardDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DashboardDTO>> GetDashboard(
            [FromQuery] GetDashboardQuery query, CancellationToken ct)
            => Ok(await Sender.Send(query, ct));
    }
}
