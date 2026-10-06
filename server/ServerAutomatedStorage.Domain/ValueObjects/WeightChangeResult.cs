using ServerAutomatedStorage.Domain.Enums;

namespace ServerAutomatedStorage.Domain.ValueObjects
{
    public record WeightChangeResult(TransactionType Type, int Quantity)
    {
        public static WeightChangeResult NoChange() => new(TransactionType.None, 0);
        public static WeightChangeResult Withdrawn(int qty) => new(TransactionType.Withdrawal, qty);
        public static WeightChangeResult Added(int qty) => new(TransactionType.Replenishment, qty);

        public bool HasChange => Quantity > 0;
    }
}
