using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Models;
using ServerAutomatedStorage.Application.Features.Transactions.DTOs;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Transactions.Queries
{
    public sealed record GetTransactionsQuery(
        Guid? UserId = null,
        Guid? ItemId = null,
        TransactionType? Type = null,
        DateTime? FromUtc = null,
        DateTime? ToUtc = null,
        int Page = 1,
        int PageSize = 50) : IRequest<PagedList<TransactionDTO>>;

    public sealed class GetTransactionsValidator : AbstractValidator<GetTransactionsQuery>
    {
        public GetTransactionsValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
            RuleFor(x => x.Type).IsInEnum().When(x => x.Type is not null);
            RuleFor(x => x.ToUtc).GreaterThanOrEqualTo(x => x.FromUtc)
                .When(x => x.FromUtc is not null && x.ToUtc is not null);
        }
    }

    public sealed class GetTransactionsHandler(ITransactionRepository transactionRepository)
        : IRequestHandler<GetTransactionsQuery, PagedList<TransactionDTO>>
    {
        public async Task<PagedList<TransactionDTO>> Handle(
            GetTransactionsQuery request, CancellationToken cancellationToken)
        {
            var filter = new TransactionFilter(
                request.UserId, request.ItemId, request.Type,
                AsUtc(request.FromUtc), AsUtc(request.ToUtc));

            var (items, total) = await transactionRepository.GetPagedAsync(
                filter, (request.Page - 1) * request.PageSize, request.PageSize, cancellationToken);

            return new PagedList<TransactionDTO>(
                items.Select(TransactionDTO.From).ToList(), request.Page, request.PageSize, total);
        }

        private static DateTime? AsUtc(DateTime? value) => value switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } v => v,
            { Kind: DateTimeKind.Local } v => v.ToUniversalTime(),
            var v => DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
        };
    }
}
