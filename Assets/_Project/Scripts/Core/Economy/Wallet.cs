using System;

namespace Bnhon.Core.Economy
{
    public sealed class Wallet
    {
        public int Money { get; private set; }

        public event Action<int> Changed;

        public Wallet(int startingMoney = 0)
        {
            Money = Math.Max(0, startingMoney);
        }

        /// <summary>Sets the balance from a save.</summary>
        public void Restore(int money)
        {
            Money = Math.Max(0, money);
            Changed?.Invoke(Money);
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Money += amount;
            Changed?.Invoke(Money);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > Money) return false;
            Money -= amount;
            Changed?.Invoke(Money);
            return true;
        }
    }
}
