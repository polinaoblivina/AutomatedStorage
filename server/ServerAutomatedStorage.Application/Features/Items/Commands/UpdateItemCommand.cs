using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Features.Items.DTOs;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Items.Commands
{
    public sealed record UpdateItemCommand(
        Guid ItemId,
        string Name,
        double UnitWeightGrams,
        int MinQuantity) : IRequest<ItemDTO>;

    public sealed class UpdateItemValidator : AbstractValidator<UpdateItemCommand>
    {
        public UpdateItemValidator()
        {
            RuleFor(x => x.ItemId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.UnitWeightGrams).GreaterThan(0);
            RuleFor(x => x.MinQuantity).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class UpdateItemHandler(IItemRepository itemRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateItemCommand, ItemDTO>
    {
        public async Task<ItemDTO> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
        {
            var item = await itemRepository.GetWithCellAsync(request.ItemId, cancellationToken)
                ?? throw new NotFoundException("Товар не найден.");

            item.UpdateDetails(request.Name, request.UnitWeightGrams, request.MinQuantity);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ItemDTO.From(item);
        }
    }
}
