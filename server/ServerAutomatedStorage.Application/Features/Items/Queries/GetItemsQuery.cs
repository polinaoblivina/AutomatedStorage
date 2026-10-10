using MediatR;
using ServerAutomatedStorage.Application.Features.Items.DTOs;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Items.Queries
{
    public sealed record GetItemsQuery(bool IncludeArchived = false) : IRequest<IReadOnlyList<ItemDTO>>;

    public sealed class GetItemsHandler(IItemRepository itemRepository)
        : IRequestHandler<GetItemsQuery, IReadOnlyList<ItemDTO>>
    {
        public async Task<IReadOnlyList<ItemDTO>> Handle(GetItemsQuery request, CancellationToken cancellationToken)
        {
            var items = await itemRepository.GetAllWithCellAsync(request.IncludeArchived, cancellationToken);
            return items.Select(ItemDTO.From).ToList();
        }
    }
}
