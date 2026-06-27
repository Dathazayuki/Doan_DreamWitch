using System;
using UnityEngine;

namespace DreamKnight.Systems.Currency
{
    [CreateAssetMenu(fileName = "CurrencyWallet", menuName = "DreamKnight/Currency/Wallet")]
    public class CurrencyWalletSO : ScriptableObject
    {
        [SerializeField] private int initialBalance;

        [NonSerialized] private int runtimeBalance;
        [NonSerialized] private bool runtimeInitialized;

        public event Action<int> OnBalanceChanged;

        public int Balance
        {
            get
            {
                EnsureInitialized();
                return runtimeBalance;
            }
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            EnsureInitialized();
            runtimeBalance += amount;
            OnBalanceChanged?.Invoke(runtimeBalance);
        }

        public bool Spend(int amount)
        {
            if (amount <= 0)
                return true;

            EnsureInitialized();
            if (runtimeBalance < amount)
                return false;

            runtimeBalance -= amount;
            OnBalanceChanged?.Invoke(runtimeBalance);
            return true;
        }

        public void SetBalance(int value)
        {
            EnsureInitialized();
            runtimeBalance = Mathf.Max(0, value);
            OnBalanceChanged?.Invoke(runtimeBalance);
        }

        public void ResetToInitial()
        {
            runtimeBalance = Mathf.Max(0, initialBalance);
            runtimeInitialized = true;
            OnBalanceChanged?.Invoke(runtimeBalance);
        }

        public int CaptureSaveData()
        {
            EnsureInitialized();
            return runtimeBalance;
        }

        public void LoadFromSaveData(int balance)
        {
            SetBalance(balance);
        }

        private void EnsureInitialized()
        {
            if (runtimeInitialized)
                return;

            runtimeBalance = Mathf.Max(0, initialBalance);
            runtimeInitialized = true;
        }
    }
}
