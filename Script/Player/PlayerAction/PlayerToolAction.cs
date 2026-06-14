using System.Collections;
using DreamKnight.Systems.Currency;
using DreamKnight.Systems.Inventory;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class PlayerToolAction : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private InventoryStateSO inventoryState;
        [SerializeField] private ToolEquipSO toolEquip;
        [SerializeField] private CurrencyWalletSO currencyWallet;

        [Header("Animation")]

        private PlayerAnimationController animationController;
        private PlayerStats playerStats;
        private bool isBusy;

        public bool IsBusy => isBusy;
        public PlayerController PlayerController => playerController;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
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
            {
                Debug.Log("[PlayerToolAction] TryUse blocked: action is busy.");
                return false;
            }

            if (toolEquip == null)
            {
                Debug.LogWarning("[PlayerToolAction] TryUse failed: ToolEquipSO is null.");
                return false;
            }

            if (!TryFindEquippedTool(out int slotIndex, out ItemDefinitionSO toolItem))
            {
                Debug.Log("[PlayerToolAction] TryUse failed: no tool equipped in any slot.");
                return false;
            }

            GameObject userObject = playerController != null ? playerController.gameObject : gameObject;
            ItemUseContext context = new ItemUseContext(userObject, inventoryState, currencyWallet, playerStats);
            bool used = TryUseToolItem(toolItem, context);
            if (!used)
            {
                Debug.LogWarning($"[PlayerToolAction] TryUse failed: tool.Use returned false for '{toolItem.name}'.");
                return false;
            }

            toolEquip.TryUnequipAt(slotIndex);

            StartCoroutine(PlayAnimationRoutine());
            return true;
        }

        private bool TryUseToolItem(ItemDefinitionSO toolItem, ItemUseContext context)
        {
            if (toolItem == null)
                return false;

            if (toolItem is ToolItemSO tool)
                return tool.Use(context, this);

            if (toolItem is OneTimeWeaponItemSO oneTimeWeapon)
                return oneTimeWeapon.Use(context);

            Debug.LogWarning($"[PlayerToolAction] TryUse failed: '{toolItem.name}' is not a supported tool item.");
            return false;
        }

        private bool TryFindEquippedTool(out int slotIndex, out ItemDefinitionSO item)
        {
            slotIndex = -1;
            item = null;

            if (toolEquip == null)
                return false;

            int slotCount = toolEquip.SlotCount;
            for (int i = 0; i < slotCount; i++)
            {
                ItemDefinitionSO slotItem = toolEquip.GetSlotItem(i);
                if (slotItem == null)
                    continue;

                slotIndex = i;
                item = slotItem;
                return true;
            }

            return false;
        }

        private IEnumerator PlayAnimationRoutine()
        {
            isBusy = true;
            animationController?.ForcePlayAnimation(PlayerAnimationController.SKILL_FASTSHOT);
            yield return WaitForAnimationFinished(PlayerAnimationController.SKILL_FASTSHOT, 1.0f);
            isBusy = false;
            ResumeMovementAnimation();
        }

        private void ResumeMovementAnimation()
        {
            if (animationController == null)
                return;

            if (playerController == null)
                return;

            PlayerMovement movement = playerController.Movement;
            PlayerInput input = playerController.Input;
            if (movement == null || input == null)
                return;

            if (!movement.IsGrounded)
            {
                string airAnim = movement.Velocity.y > 0.5f
                    ? PlayerAnimationController.JUMP
                    : PlayerAnimationController.FALL_LOOP;
                animationController.CrossFadeAnimation(airAnim, 0.05f);
                return;
            }

            if (Mathf.Abs(input.MoveInput.x) > 0.1f)
                animationController.CrossFadeAnimation(PlayerAnimationController.RUN, 0.05f);
            else
                animationController.CrossFadeAnimation(PlayerAnimationController.IDLE, 0.05f);
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
