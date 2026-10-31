using ServerAutomatedStorage.Domain.Common;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;

namespace ServerAutomatedStorage.UnitTests.Domain
{
    public class CabinetTests
    {
        private static Cabinet NewCabinet() => new("Шкаф", "Склад");

        [Fact]
        public void AddCell_WithNewName_AttachesCellToCabinet()
        {
            var cabinet = NewCabinet();
            var cell = new Cell("A1");

            cabinet.AddCell(cell);

            Assert.Single(cabinet.Cells);
            Assert.Equal(cabinet.Id, cell.CabinetId);
        }

        [Fact]
        public void AddCell_WithDuplicateName_Throws()
        {
            var cabinet = NewCabinet();
            cabinet.AddCell(new Cell("A1"));

            Assert.Throws<DomainException>(() => cabinet.AddCell(new Cell("A1")));
        }

        [Fact]
        public void RemoveCell_WithItem_Throws()
        {
            var cabinet = NewCabinet();
            var cell = new Cell("A1");
            cabinet.AddCell(cell);
            cell.PlaceItem(new Item("Болт", 10));

            Assert.Throws<DomainException>(() => cabinet.RemoveCell(cell.Id));
        }

        [Fact]
        public void EnsureCanBeDeleted_WithItems_Throws()
        {
            var cabinet = NewCabinet();
            var cell = new Cell("A1");
            cabinet.AddCell(cell);
            cell.PlaceItem(new Item("Болт", 10));

            Assert.Throws<DomainException>(cabinet.EnsureCanBeDeleted);
        }
    }

    public class CellTests
    {
        [Fact]
        public void PlaceItem_InEmptyCell_LinksItemAndCell()
        {
            var cell = new Cell("A1");
            var item = new Item("Болт", 10);

            cell.PlaceItem(item);

            Assert.Same(item, cell.Item);
            Assert.Equal(cell.Id, item.CellId);
            Assert.False(item.IsArchived);
        }

        [Fact]
        public void PlaceItem_InOccupiedCell_Throws()
        {
            var cell = new Cell("A1");
            cell.PlaceItem(new Item("Болт", 10));

            Assert.Throws<DomainException>(() => cell.PlaceItem(new Item("Гайка", 5)));
        }

        [Fact]
        public void RemoveItem_ArchivesItem()
        {
            var cell = new Cell("A1");
            var item = new Item("Болт", 10);
            cell.PlaceItem(item);

            var removed = cell.RemoveItem();

            Assert.Same(item, removed);
            Assert.Null(cell.Item);
            Assert.True(item.IsArchived);
        }

        [Fact]
        public void RemoveItem_FromEmptyCell_Throws()
        {
            Assert.Throws<DomainException>(() => new Cell("A1").RemoveItem());
        }
    }

    public class UserTests
    {
        private static Role RoleWith(Permission permission) => new("Тестовая", permission);

        [Theory]
        [InlineData("a1b2c3", "A1B2C3")]
        [InlineData("  a1b2c3  ", "A1B2C3")]
        [InlineData("", null)]
        [InlineData("   ", null)]
        [InlineData(null, null)]
        public void Create_NormalizesCardUid(string? input, string? expected)
        {
            var user = new User("Иван", RoleWith(Permission.OpenCell), input);

            Assert.Equal(expected, user.CardUid);
        }

        [Theory]
        [InlineData(Permission.OpenCell, Permission.OpenCell, true)]
        [InlineData(Permission.OpenCell, Permission.ManageUsers, false)]
        [InlineData(Permission.OpenCell | Permission.ViewJournal, Permission.ViewJournal, true)]
        [InlineData(Permission.None, Permission.OpenCell, false)]
        public void Can_DependsOnRolePermissions(Permission granted, Permission requested, bool expected)
        {
            var user = new User("Иван", RoleWith(granted));

            Assert.Equal(expected, user.Can(requested));
        }

        [Fact]
        public void Can_InactiveUser_IsAlwaysFalse()
        {
            var user = new User("Иван", RoleWith(Permission.OpenCell));

            user.Deactivate();

            Assert.False(user.Can(Permission.OpenCell));
            user.Activate();
            Assert.True(user.Can(Permission.OpenCell));
        }
    }

}
