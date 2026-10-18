using ServerAutomatedStorage.Domain.Common;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTests.Domain
{
    public class ItemTests
    {
        private const double Noise = 5;

        [Fact]
        public void AnalyzeWeightChange_WeightDecreased_ReturnsWithdrawal()
        {
            var item = new Item("Болт М8", unitWeightGrams: 10);
            var result = item.AnalyzeWeightChange(100, 70, Noise);

            Assert.Equal(TransactionType.Withdrawal, result.Type);
            Assert.Equal(3, result.Quantity);
        }

        [Fact]
        public void AnalyzeWeightChange_WeightIncreased_ReturnsReplenishment()
        {
            var item = new Item("Болт М8", unitWeightGrams: 10);
            var result = item.AnalyzeWeightChange(70, 100, Noise);

            Assert.Equal(TransactionType.Replenishment, result.Type);
            Assert.Equal(3, result.Quantity);
        }

        [Theory]
        [InlineData(100, 100)]
        [InlineData(100, 98)]
        [InlineData(100, 103)]
        public void AnalyzeWeightChange_DifferenceBelowNoise_ReturnsNoChange(double before, double after)
        {
            var item = new Item("Болт М8", unitWeightGrams: 10);
            var result = item.AnalyzeWeightChange(before, after, Noise);

            Assert.False(result.HasChange);
        }

        [Fact]
        public void AnalyzeWeightChange_RoundsToNearestUnit()
        {
            var item = new Item("Болт М8", unitWeightGrams: 10);
            var result = item.AnalyzeWeightChange(100, 71.5, Noise); 

            Assert.Equal(3, result.Quantity);
        }

        [Fact]
        public void RemoveStock_MoreThanAvailable_Throws()
        {
            var item = new Item("Болт М8", unitWeightGrams: 10);
            item.AddStock(2);
            var ex = Assert.Throws<DomainException>(() => item.RemoveStock(5));

            Assert.Contains("Недостаточно", ex.Message);
            Assert.Equal(2, item.StockQuantity);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_NonPositiveWeight_Throws(double weight)
        {
            Assert.Throws<DomainException>(() => new Item("Болт М8", weight));
        }
    }
}
