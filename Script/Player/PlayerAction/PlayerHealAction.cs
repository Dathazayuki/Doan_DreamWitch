using System.Collections;
using DreamKnight.Systems.Currency;
using DreamKnight.Systems.Inventory;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class PlayerHealAction : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private InventoryStateSO inventoryState;
        [SerializeField] private HealingPotionEquipSO equipState;
        [SerializeField] private CurrencyWalletSO currencyWallet;

        [Header("Animation")]
        [SerializeField] private float takeTimeout = 1.5f;
        [SerializeField] private float drinkTimeout = 1.2f;

        private PlayerAnimationController animationController;
        private PlayerStats playerStats;
        private bool isBusy;
        private bool cancelRequested;
        private bool inTakePhase;

        public bool IsBusy => isBusy;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (playerStats != null)
                playerStats.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (playerStats != null)
                playerStats.OnDamaged -= HandleDamaged;
        }

        private void ResolveReferences()
        {
            if (playerController == null)
                playerController = GetComponentInParent<PlayerController>();

            if (playerStats == null)
                playerStats = GetComponentInParent<PlayerStats>();

            if (playerStats == null && playerController != null)
                playerStats = playerController.Stats;

            if (animationController == null && playerController != null)
                animationController = playerController.AnimationController;
        }

        public bool TryUse()
        {
            ResolveReferences();
            if (isBusy)
                return false;

            if (playerStats == null)
            {
                return false;
            }

            if (!TryFindEquippedPotion(out int slotIndex, out ItemDefinitionSO item))
            {
                return false;
            }
                Debug.Log($"[PlayerHealAction] Using potion from slot {slotIndex}.");

            StartCoroutine(UseRoutine(slotIndex, item));
            return true;
        }

        private void HandleDamaged(float damage)
        {
            if (!isBusy || !inTakePhase)
                return;

            cancelRequested = true;
        }

        private bool TryFindEquippedPotion(out int slotIndex, out ItemDefinitionSO item)
        {
            slotIndex = -1;
            item = null;

            if (equipState == null)
                return false;

            int slotCount = equipState.SlotCount;
            for (int i = slotCount-1; i >=0 ; i--)
            {
                ItemDefinitionSO slotItem = equipState.GetSlotItem(i);
                if (slotItem == null)
                    continue;

                slotIndex = i;
                item = slotItem;
                return true;
            }

            return false;
        }

        private IEnumerator UseRoutine(int slotIndex, ItemDefinitionSO item)
        {
            isBusy = true;
            cancelRequested = false;
            inTakePhase = true;

            animationController?.ForcePlayAnimation(PlayerAnimationController.TAKE);
            yield return WaitForAnimationFinished(PlayerAnimationController.TAKE, takeTimeout);

            if (cancelRequested)
            {
                animationController?.CrossFadeAnimation(PlayerAnimationController.IDLE, 0.05f);
                isBusy = false;
                inTakePhase = false;
                yield break;
            }

            inTakePhase = false;
            animationController?.ForcePlayAnimation(PlayerAnimationController.DRINK);
            ConsumePotion(slotIndex, item);
            yield return WaitForAnimationFinished(PlayerAnimationController.DRINK, drinkTimeout);
            animationController?.CrossFadeAnimation(PlayerAnimationController.IDLE,0.02f);
            isBusy = false;
        }

        private void ConsumePotion(int slotIndex, ItemDefinitionSO item)
        {
            if (item == null || equipState == null)
                return;

            ResolveReferences();
            if (playerStats == null)
            {
                return;
            }

            ItemUseContext context = new ItemUseContext(gameObject, inventoryState, currencyWallet, playerStats);
            bool used = item.Use(context);
            if (used)
                equipState.TryUnequipAt(slotIndex);
        }

        private IEnumerator WaitForAnimationFinished(string animationName, float timeout)
        {
            if (animationController == null)
                yield break;

            float limit = Mathf.Max(0.05f, timeout);
            float timer = 0f;
            bool hasStarted = false;

            while (timer < limit)
            {
                if (animationController.IsPlaying(animationName))
                {
                    hasStarted = true;
                    if (animationController.HasAnimationFinished())
                        yield break;
                }
                else if (hasStarted)
                {
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }
        }
    }
}
