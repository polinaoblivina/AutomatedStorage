using NSubstitute;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Features.Items.Commands;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;

namespace ServerAutomatedStorage.UnitTests.Application
{
    public class RemoveItemHandlerTests
    {
        private readonly IItemRepository _items = Substitute.For<IItemRepository>();
        private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly RemoveItemHandler _handler;

        public RemoveItemHandlerTests()
        {
            _handler = new RemoveItemHandler(_items, _transactions, _unitOfWork);
        }

        private Item GivenPlacedItem(bool hasJournal)
        {
            var cell = new Cell("A1");
            var item = new Item("Болт", 10);
            cell.PlaceItem(item);
            _items.GetWithCellAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
            _transactions.AnyForItemAsync(item.Id, Arg.Any<CancellationToken>()).Returns(hasJournal);
            return item;
        }

        [Fact]
        public async Task ItemWithoutJournal_IsRemovedPermanently()
        {
            var item = GivenPlacedItem(hasJournal: false);

            var result = await _handler.Handle(new RemoveItemCommand(item.Id), CancellationToken.None);

            Assert.False(result.Archived);
            _items.Received(1).Remove(item);
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ItemWithJournal_IsArchivedAndNotRemoved()
        {
            var item = GivenPlacedItem(hasJournal: true);

            var result = await _handler.Handle(new RemoveItemCommand(item.Id), CancellationToken.None);

            Assert.True(result.Archived);
            Assert.True(item.IsArchived);
            _items.DidNotReceive().Remove(Arg.Any<Item>());
        }

        [Fact]
        public async Task ArchivedItemWithJournal_ThrowsConflict()
        {
            var item = GivenPlacedItem(hasJournal: true);
            item.Cell!.RemoveItem();

            await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(new RemoveItemCommand(item.Id), CancellationToken.None));
        }

        [Fact]
        public async Task UnknownItem_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(new RemoveItemCommand(Guid.NewGuid()), CancellationToken.None));
        }
    }

    public class MoveItemHandlerTests
    {
        private readonly IItemRepository _items = Substitute.For<IItemRepository>();
        private readonly ICellRepository _cells = Substitute.For<ICellRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly MoveItemHandler _handler;

        public MoveItemHandlerTests()
        {
            _handler = new MoveItemHandler(_items, _cells, _unitOfWork);
        }

        private Item GivenPlacedItem(out Cell source)
        {
            source = new Cell("A1");
            var item = new Item("Болт", 10);
            source.PlaceItem(item);
            _items.GetWithCellAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
            return item;
        }

        private Cell GivenCell(Item? occupant = null)
        {
            var cell = new Cell("B1");
            if (occupant is not null)
                cell.PlaceItem(occupant);
            _cells.GetWithItemAsync(cell.Id, Arg.Any<CancellationToken>()).Returns(cell);
            return cell;
        }

        [Fact]
        public async Task MoveToSameCell_ReturnsWithoutTouchingStorage()
        {
            var item = GivenPlacedItem(out var source);

            var result = await _handler.Handle(new MoveItemCommand(item.Id, source.Id), CancellationToken.None);

            Assert.Equal(source.Id, result.CellId);
            await _cells.DidNotReceiveWithAnyArgs().GetWithItemAsync(default, default);
            await _unitOfWork.DidNotReceiveWithAnyArgs().BeginTransactionAsync(default);
        }

        [Fact]
        public async Task MoveToFreeCell_ChangesCellInsideTransaction()
        {
            var item = GivenPlacedItem(out var source);
            var target = GivenCell();

            var result = await _handler.Handle(new MoveItemCommand(item.Id, target.Id), CancellationToken.None);

            Assert.Equal(target.Id, result.CellId);
            Assert.Null(source.Item);
            await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task MoveToOccupiedCell_ThrowsConflict()
        {
            var item = GivenPlacedItem(out _);
            var target = GivenCell(occupant: new Item("Гайка", 5));

            await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(new MoveItemCommand(item.Id, target.Id), CancellationToken.None));
        }
    }

    public class StockHandlersTests
    {
        private readonly IItemRepository _items = Substitute.For<IItemRepository>();
        private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly Guid _userId = Guid.NewGuid();

        private Item GivenItem(bool placed, int stock = 0)
        {
            var item = new Item("Болт", 10);
            if (placed)
                new Cell("A1").PlaceItem(item);
            if (stock > 0)
                item.AddStock(stock);
            _items.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
            return item;
        }

        [Fact]
        public async Task Replenish_AddsStockAndRecordsTransaction()
        {
            var item = GivenItem(placed: true, stock: 5);
            var handler = new ReplenishStockHandler(_items, _transactions, _unitOfWork);

            await handler.Handle(new ReplenishStockCommand(item.Id, 7, _userId), CancellationToken.None);

            Assert.Equal(12, item.StockQuantity);
            await _transactions.Received(1).AddAsync(
                Arg.Is<Transaction>(t => t.Type == TransactionType.Replenishment && t.Quantity == 7
                                         && t.WeightBefore == 50 && t.WeightAfter == 120 && t.UserId == _userId),
                Arg.Any<CancellationToken>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Replenish_ArchivedItem_ThrowsConflict()
        {
            var item = GivenItem(placed: false);
            var handler = new ReplenishStockHandler(_items, _transactions, _unitOfWork);

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new ReplenishStockCommand(item.Id, 1, _userId), CancellationToken.None));
            await _transactions.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        }

        [Fact]
        public async Task Adjust_SetsStockAndRecordsAdjustment()
        {
            var item = GivenItem(placed: true, stock: 10);
            var handler = new AdjustStockHandler(_items, _transactions, _unitOfWork);

            await handler.Handle(new AdjustStockCommand(item.Id, 4, _userId), CancellationToken.None);

            Assert.Equal(4, item.StockQuantity);
            await _transactions.Received(1).AddAsync(
                Arg.Is<Transaction>(t => t.Type == TransactionType.Adjustment), Arg.Any<CancellationToken>());
        }
    }
}
