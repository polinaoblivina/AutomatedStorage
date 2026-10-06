using ServerAutomatedStorage.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Entities
{
    public class Cabinet : Entity
    {
        public string Name { get; private set; } = null!;
        public string Location { get; private set; } = null!;
        private readonly List<Cell> _cells = new();
        public IReadOnlyCollection<Cell> Cells => _cells.AsReadOnly();

        private Cabinet() { }

        public Cabinet(string name, string location)
        {
            Name = Guard.AgainstNullOrWhiteSpace(name, nameof(Name));
            Location = Guard.AgainstNullOrWhiteSpace(location, nameof(Location));
        }

        public void UpdateInfo(string name, string location)
        {
            Name = Guard.AgainstNullOrWhiteSpace(name, nameof(Name));
            Location = Guard.AgainstNullOrWhiteSpace(location, nameof(Location));
        }

        public void AddCell(Cell cell)
        {
            Guard.AgainstNull(cell, nameof(cell));

            if (_cells.Any(c => c.Id == cell.Id || c.Name == cell.Name))
                throw new DomainException($"В шкафу уже есть ячейка «{cell.Name}».");

            cell.AttachToCabinet(this);
            _cells.Add(cell);
        }

        public void RenameCell(Guid cellId, string name)
        {
            var cell = FindCell(cellId);

            if (_cells.Any(c => c.Id != cellId && c.Name == name))
                throw new DomainException($"В шкафу уже есть ячейка «{name}».");

            cell.Rename(name);
        }

        public Cell RemoveCell(Guid cellId)
        {
            var cell = FindCell(cellId);

            if (cell.Item is not null)
                throw new DomainException($"Ячейка «{cell.Name}» не пуста. Сначала уберите из неё товар.");

            _cells.Remove(cell);
            return cell;
        }

        public void EnsureCanBeDeleted()
        {
            if (_cells.Any(c => c.Item is not null))
                throw new DomainException("В шкафу есть ячейки с товарами. Сначала освободите их.");
        }

        private Cell FindCell(Guid cellId) =>
            _cells.FirstOrDefault(c => c.Id == cellId)
            ?? throw new DomainException("Ячейка не найдена в этом шкафу.");

    }
}
