using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using ServerAutomatedStorage.Application.Features.Access.Commands;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;

namespace ServerAutomatedStorage.UnitTests.Application
{
    public class RecordSessionHandlerTests
    {
        private const string Card = "A1B2C3D4";

        private readonly IUserRepository _users = Substitute.For<IUserRepository>();
        private readonly ICellRepository _cells = Substitute.For<ICellRepository>();
        private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
        private readonly IDeviceSessionRepository _sessions = Substitute.For<IDeviceSessionRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero));
        private readonly RecordSessionHandler _handler;

        public RecordSessionHandlerTests()
        {
            _handler = new RecordSessionHandler(_users, _cells, _transactions, _sessions, _unitOfWork, _time);
        }

        private User GivenUser(Permission permission = Permission.OpenCell)
        {
            var user = new User("Иван", new Role("Роль", permission), Card);
            _users.GetByCardUidAsync(Card, Arg.Any<CancellationToken>()).Returns(user);
            return user;
        }

        private Cell GivenCellWithItem(int stock, double unitWeight = 10)
        {
            var cell = new Cell("A1");
            var item = new Item("Болт", unitWeight);
            cell.PlaceItem(item);
            if (stock > 0)
                item.AddStock(stock);
            _cells.GetWithItemAsync(cell.Id, Arg.Any<CancellationToken>()).Returns(cell);
            return cell;
        }

        private static RecordSessionCommand Session(Guid cellId, double before, double after, int age = 0, string id = "S1") =>
            new(id, Card, age, [new CellWeighing(cellId, before, after)]);

        [Fact]
        public async Task AlreadyRecordedSession_IsAcknowledgedWithoutChanges()
        {
            _sessions.ExistsAsync("S1", Arg.Any<CancellationToken>()).Returns(true);

            var result = await _handler.Handle(Session(Guid.NewGuid(), 10, 5), CancellationToken.None);

            Assert.True(result.AlreadyRecorded);
            Assert.True(result.Success);
            await _users.DidNotReceiveWithAnyArgs().GetByCardUidAsync(default!, default);
            await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        }

        [Fact]
        public async Task UnknownCard_IsRejected()
        {
            var result = await _handler.Handle(Session(Guid.NewGuid(), 10, 5), CancellationToken.None);

            Assert.False(result.Success);
            await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        }

        [Fact]
        public async Task Withdrawal_UpdatesStockWritesTransactionAndRemembersSession()
        {
            var user = GivenUser();
            var cell = GivenCellWithItem(stock: 10);

            var result = await _handler.Handle(Session(cell.Id, 100, 60), CancellationToken.None);

            var cellResult = Assert.Single(result.Cells);
            Assert.Equal(4, cellResult.Quantity);
            Assert.Equal(6, cellResult.StockQuantity);
            Assert.Equal(6, cell.Item!.StockQuantity);
            await _transactions.Received(1).AddAsync(
                Arg.Is<Transaction>(t => t.Type == TransactionType.Withdrawal && t.Quantity == 4 && t.UserId == user.Id),
                Arg.Any<CancellationToken>());
            await _sessions.Received(1).AddAsync(Arg.Is<DeviceSession>(s => s.Key == "S1" && s.UserId == user.Id), Arg.Any<CancellationToken>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task WithdrawalBeyondStock_ClampsStockAtZero()
        {
            GivenUser();
            var cell = GivenCellWithItem(stock: 2);

            await _handler.Handle(Session(cell.Id, 100, 0), CancellationToken.None);

            Assert.Equal(0, cell.Item!.StockQuantity);
        }

        [Fact]
        public async Task WeightChangeBelowNoise_WritesNothing()
        {
            GivenUser();
            var cell = GivenCellWithItem(stock: 10);

            var result = await _handler.Handle(Session(cell.Id, 100, 98), CancellationToken.None);

            Assert.True(result.Cells.Single().Success);
            Assert.Equal(10, cell.Item!.StockQuantity);
            await _transactions.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        }

        [Fact]
        public async Task UnknownCell_IsReportedAsFailedForThatCell()
        {
            GivenUser();

            var result = await _handler.Handle(Session(Guid.NewGuid(), 100, 60), CancellationToken.None);

            Assert.True(result.Success);
            Assert.False(result.Cells.Single().Success);
        }

        [Fact]
        public async Task SeveralCells_AreProcessedIndependently()
        {
            GivenUser();
            var first = GivenCellWithItem(stock: 10);
            var second = GivenCellWithItem(stock: 10, unitWeight: 20);
            var command = new RecordSessionCommand("S2", Card, 0,
                [new CellWeighing(first.Id, 100, 70), new CellWeighing(second.Id, 100, 40)]);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(2, result.Cells.Count);
            Assert.Equal(7, first.Item!.StockQuantity);
            Assert.Equal(7, second.Item!.StockQuantity);
        }
    }

    public class RecordSessionValidatorTests
    {
        private readonly RecordSessionValidator _validator = new();

        private static RecordSessionCommand Valid() =>
            new("S1", "CARD", 0, [new CellWeighing(Guid.NewGuid(), 10, 5)]);

        [Theory]
        [InlineData(0, true)]
        [InlineData(RecordSessionValidator.MaxAgeSeconds, true)]
        [InlineData(RecordSessionValidator.MaxAgeSeconds + 1, false)]
        [InlineData(-1, false)]
        public void AgeSeconds_Boundaries(int age, bool expected)
        {
            var command = Valid() with { AgeSeconds = age };

            Assert.Equal(expected, _validator.Validate(command).IsValid);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(RecordSessionValidator.MaxCells, true)]
        [InlineData(RecordSessionValidator.MaxCells + 1, false)]
        public void CellCount_Boundaries(int count, bool expected)
        {
            var cells = Enumerable.Range(0, count).Select(_ => new CellWeighing(Guid.NewGuid(), 1, 1)).ToList();

            Assert.Equal(expected, _validator.Validate(Valid() with { Cells = cells }).IsValid);
        }

        [Fact]
        public void DuplicateCells_Fail()
        {
            var id = Guid.NewGuid();
            var cells = new[] { new CellWeighing(id, 1, 1), new CellWeighing(id, 2, 2) };

            Assert.False(_validator.Validate(Valid() with { Cells = cells }).IsValid);
        }
    }

    public class ItemWeightAnalysisTests
    {
        [Theory]
        [InlineData(20, 200, 120, "Withdrawal", 4)]
        [InlineData(20, 100, 160, "Replenishment", 3)]
        [InlineData(20, 100, 97, "None", 0)]
        [InlineData(20, 100, 95, "None", 0)]
        [InlineData(20, 100, 89, "Withdrawal", 1)]
        [InlineData(20, 100, 100, "None", 0)]
        [InlineData(4, 100, 97.5, "Withdrawal", 1)]
        [InlineData(4, 100, 99, "None", 0)]
        [InlineData(1000, 5000, 3990, "Withdrawal", 1)]
        public void AnalyzeWeightChange_ReturnsExpectedOperation(
            double unitWeight, double before, double after, string expectedType, int expectedQuantity)
        {
            var item = new Item("Товар", unitWeight);

            var result = item.AnalyzeWeightChange(before, after, 5);

            Assert.Equal(Enum.Parse<TransactionType>(expectedType), result.Type);
            Assert.Equal(expectedQuantity, result.Quantity);
            Assert.Equal(expectedQuantity > 0, result.HasChange);
        }
    }
}
