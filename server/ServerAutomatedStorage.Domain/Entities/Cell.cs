using ServerAutomatedStorage.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Entities
{
    public class Cell : Entity
    {
        public string Name { get; private set; } = null!;
        public Guid CabinetId { get; private set; }
        public Cabinet? Cabinet { get; private set; }
        public Item? Item { get; private set; }

        private Cell() { }

        public Cell(string name)
        {
            Name = Guard.AgainstNullOrWhiteSpace(name, nameof(Name));
        }

        public void Rename(string name) => Name = Guard.AgainstNullOrWhiteSpace(name, nameof(Name));

        internal void AttachToCabinet(Cabinet cabinet)
        {
            Cabinet = Guard.AgainstNull(cabinet, nameof(cabinet));
            CabinetId = cabinet.Id;
        }

        public void PlaceItem(Item item)
        {
            Guard.AgainstNull(item, nameof(item));

            if (Item is not null)
                throw new DomainException($"В ячейке «{Name}» уже лежит товар «{Item.Name}».");

            item.AttachToCell(this);
            Item = item;
        }

        public Item RemoveItem()
        {
            var item = Item ?? throw new DomainException($"Ячейка «{Name}» уже пуста.");

            item.DetachFromCell();
            Item = null;
            return item;
        }
    }
}
