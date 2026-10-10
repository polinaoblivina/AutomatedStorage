using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Application.Features.Items.Commands;
using ServerAutomatedStorage.Application.Features.Items.DTOs;
using ServerAutomatedStorage.Application.Features.Items.Queries;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.WebAPI.Authorization;
using ServerAutomatedStorage.WebAPI.Contracts;

namespace ServerAutomatedStorage.WebAPI.Controllers
{
    [Authorize]
    public sealed class ItemsController(ICurrentUser currentUser) : ApiControllerBase
    {
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<ItemDTO>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ItemDTO>>> GetAll(
            [FromQuery] bool includeArchived, CancellationToken ct)
            => Ok(await Sender.Send(new GetItemsQuery(includeArchived), ct));

        [HttpPost]
        [HasPermission(Permission.ManageStock)]
        [ProducesResponseType<ItemDTO>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ItemDTO>> Create(CreateItemCommand command, CancellationToken ct)
        {
            var item = await Sender.Send(command, ct);
            return CreatedAtAction(nameof(GetAll), null, item);
        }

        [HttpPut("{id:guid}")]
        [HasPermission(Permission.ManageStock)]
        [ProducesResponseType<ItemDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ItemDTO>> Update(Guid id, UpdateItemRequest request, CancellationToken ct)
            => Ok(await Sender.Send(
                new UpdateItemCommand(id, request.Name, request.UnitWeightGrams, request.MinQuantity), ct));

        [HttpPost("{id:guid}/move")]
        [HasPermission(Permission.ManageStock)]
        [ProducesResponseType<ItemDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ItemDTO>> Move(Guid id, MoveItemRequest request, CancellationToken ct)
            => Ok(await Sender.Send(new MoveItemCommand(id, request.CellId), ct));

        [HttpPost("{id:guid}/replenish")]
        [HasPermission(Permission.ManageStock)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Replenish(Guid id, ReplenishStockRequest request, CancellationToken ct)
        {
            await Sender.Send(new ReplenishStockCommand(id, request.Quantity, currentUser.RequiredUserId), ct);
            return NoContent();
        }

        [HttpPost("{id:guid}/adjust")]
        [HasPermission(Permission.ManageStock)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Adjust(Guid id, AdjustStockRequest request, CancellationToken ct)
        {
            await Sender.Send(new AdjustStockCommand(id, request.ActualQuantity, currentUser.RequiredUserId), ct);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [HasPermission(Permission.ManageStock)]
        [ProducesResponseType<RemoveItemResult>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<RemoveItemResult>> Remove(Guid id, CancellationToken ct)
            => Ok(await Sender.Send(new RemoveItemCommand(id), ct));
    }
}
