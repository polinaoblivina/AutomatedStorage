using Microsoft.AspNetCore.Mvc;
using ServerAutomatedStorage.Application.Features.Cabinets.Commands;
using ServerAutomatedStorage.Application.Features.Cabinets.DTOs;
using ServerAutomatedStorage.Application.Features.Cabinets.Queries;
using ServerAutomatedStorage.WebAPI.Contracts;

namespace ServerAutomatedStorage.WebAPI.Controllers
{
    public sealed class CabinetsController : ApiControllerBase
    {
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<CabinetDTO>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<CabinetDTO>>> GetAll(CancellationToken ct)
            => Ok(await Sender.Send(new GetCabinetsQuery(), ct));

        [HttpGet("{id:guid}")]
        [ProducesResponseType<CabinetDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CabinetDTO>> GetById(Guid id, CancellationToken ct)
            => Ok(await Sender.Send(new GetCabinetByIdQuery(id), ct));

        [HttpPost]
        [ProducesResponseType<CabinetDTO>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CabinetDTO>> Create(CreateCabinetCommand command, CancellationToken ct)
        {
            var cabinet = await Sender.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = cabinet.Id }, cabinet);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType<CabinetDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CabinetDTO>> Update(Guid id, UpdateCabinetRequest request, CancellationToken ct)
            => Ok(await Sender.Send(new UpdateCabinetCommand(id, request.Name, request.Location), ct));

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await Sender.Send(new DeleteCabinetCommand(id), ct);
            return NoContent();
        }

        [HttpPost("{id:guid}/cells")]
        [ProducesResponseType<CellDTO>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CellDTO>> AddCell(Guid id, AddCellRequest request, CancellationToken ct)
        {
            var cell = await Sender.Send(new AddCellCommand(id, request.Name), ct);
            return CreatedAtAction(nameof(GetById), new { id }, cell);
        }

        [HttpPut("{id:guid}/cells/{cellId:guid}")]
        [ProducesResponseType<CellDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CellDTO>> RenameCell(Guid id, Guid cellId, RenameCellRequest request, CancellationToken ct)
            => Ok(await Sender.Send(new RenameCellCommand(id, cellId, request.Name), ct));
     
        [HttpDelete("{id:guid}/cells/{cellId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveCell(Guid id, Guid cellId, CancellationToken ct)
        {
            await Sender.Send(new RemoveCellCommand(id, cellId), ct);
            return NoContent();
        }
    }
}
