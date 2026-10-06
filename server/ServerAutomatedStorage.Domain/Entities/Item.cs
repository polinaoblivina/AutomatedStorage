using ServerAutomatedStorage.Domain.Common;
using ServerAutomatedStorage.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;


namespace ServerAutomatedStorage.Domain.Entities
{
    public class Item : Entity
    {
        public string Name { get; private set; } = null!;
        public double UnitWeightGrams { get; private set; }
        public int StockQuantity { get; private set; }
        public int MinQuantity { get; private set; }

        public Guid? CellId { get; private set; }
        public Cell? Cell { get; private set; }

        public bool IsArchived => CellId is null;
        public bool IsLowStock => StockQuantity <= MinQuantity;

        private Item() { }

        public Item(string name, double unitWeightGrams, int minQuantity = 0)
        {
            Name = Guard.AgainstNullOrWhiteSpace(name, nameof(Name));
            UnitWeightGrams = Guard.AgainstNegativeOrZero(unitWeightGrams, nameof(UnitWeightGrams));
            MinQuantity = Guard.AgainstNegative(minQuantity, nameof(MinQuantity));
            StockQuantity = 0;
        }

        public void UpdateDetails(string name, double unitWeightGrams, int minQuantity)
        {
            Name = Guard.AgainstNullOrWhiteSpace(name, nameof(Name));
            UnitWeightGrams = Guard.AgainstNegativeOrZero(unitWeightGrams, nameof(UnitWeightGrams));
            MinQuantity = Guard.AgainstNegative(minQuantity, nameof(MinQuantity));
        }

        internal void AttachToCell(Cell cell)
        {
            Cell = Guard.AgainstNull(cell, nameof(cell));
            CellId = cell.Id;
        }

        internal void DetachFromCell()
        {
            Cell = null;
            CellId = null;
        }

        public void AddStock(int quantity)
        {
            Guard.AgainstNegativeOrZero(quantity, nameof(quantity));
            StockQuantity += quantity;
        }

        public void RemoveStock(int quantity)
        {
            Guard.AgainstNegativeOrZero(quantity, nameof(quantity));

            if (StockQuantity - quantity < 0)
                throw new DomainException($"Недостаточно товара «{Name}» на складе.");

            StockQuantity -= quantity;
        }

        public void CorrectStock(int quantity)
        {
            StockQuantity = Guard.AgainstNegative(quantity, nameof(quantity));
        }

        public WeightChangeResult AnalyzeWeightChange(double weightBefore, double weightAfter, double noiseThresholdGrams)
        {
            var diff = weightBefore - weightAfter;
            var threshold = Math.Min(noiseThresholdGrams, UnitWeightGrams / 2);

            if (Math.Abs(diff) < threshold)
                return WeightChangeResult.NoChange();

            var quantity = (int)Math.Round(Math.Abs(diff) / UnitWeightGrams);
            if (quantity == 0)
                return WeightChangeResult.NoChange();

            return diff > 0 ? WeightChangeResult.Withdrawn(quantity) : WeightChangeResult.Added(quantity);
        }
    
    }
}
