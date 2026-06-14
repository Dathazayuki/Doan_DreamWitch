using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class PlayerActionHandler : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerHealAction healAction;
        [SerializeField] private PlayerToolAction toolAction;
        [SerializeField] private PlayerSpellAction spellAction;
        [SerializeField] private bool debugSpellFlow = true;

        private PlayerInput playerInput;
        private PlayerStats playerStats;

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

            if (playerInput == null)
                playerInput = playerController != null ? playerController.Input : GetComponentInParent<PlayerInput>();

            if (playerStats == null)
                playerStats = playerController != null ? playerController.Stats : GetComponentInParent<PlayerStats>();

            if (healAction == null)
                healAction = playerController != null ? playerController.GetComponentInChildren<PlayerHealAction>(true) : GetComponentInChildren<PlayerHealAction>(true);

            if (toolAction == null)
                toolAction = playerController != null ? playerController.GetComponentInChildren<PlayerToolAction>(true) : GetComponentInChildren<PlayerToolAction>(true);

            if (spellAction == null)
                spellAction = playerController != null ? playerController.GetComponentInChildren<PlayerSpellAction>(true) : GetComponentInChildren<PlayerSpellAction>(true);

            if (debugSpellFlow)
            {
                Debug.Log($"[PlayerActionHandler] ResolveReferences -> playerController={(playerController != null ? playerController.name : "null")}, playerInput={(playerInput != null ? "ok" : "null")}, playerStats={(playerStats != null ? "ok" : "null")}, healAction={(healAction != null ? healAction.name : "null")}, toolAction={(toolAction != null ? toolAction.name : "null")}, spellAction={(spellAction != null ? spellAction.name : "null")}");
            }
        }

        private void LateUpdate()
        {
            if (playerInput == null)
            {
                ResolveReferences();
                if (debugSpellFlow)
                    Debug.Log("[PlayerActionHandler] LateUpdate: playerInput was null, ResolveReferences() called.");
            }



            if (playerStats != null && !playerStats.IsAlive)
            {
                return;
            }

            if (playerController != null && playerController.IsTransformed)
            {
                return;
            }

            if (IsAnyBusy())
            {
                if (debugSpellFlow)
                    Debug.Log("[PlayerActionHandler] LateUpdate blocked: an action is busy.");
                return;
            }

            if (playerInput.UsePotionPressed && healAction != null)
            {
                if (debugSpellFlow)
                    Debug.Log("[PlayerActionHandler] UsePotionPressed -> healAction.TryUse()");
                healAction.TryUse();
                return;
            }

            if (playerInput.UseToolPressed && toolAction != null)
            {
                if (debugSpellFlow)
                    Debug.Log("[PlayerActionHandler] UseToolPressed -> toolAction.TryUse()");
                toolAction.TryUse();
                return;
            }

            if (playerInput.UseSpellPressed && spellAction != null)
            {
                if (debugSpellFlow)
                    Debug.Log("[PlayerActionHandler] UseSpellPressed -> spellAction.TryUse()");
                spellAction.TryUse();
                return;
            }

            if (debugSpellFlow && playerInput.UseSpellPressed)
                Debug.Log($"[PlayerActionHandler] UseSpellPressed was true but spellAction is {(spellAction != null ? "present" : "null")}, healBusy={(healAction != null && healAction.IsBusy)}, toolBusy={(toolAction != null && toolAction.IsBusy)}, spellBusy={(spellAction != null && spellAction.IsBusy)}");
        }

        private bool IsAnyBusy()
        {
            if (healAction != null && healAction.IsBusy)
                return true;

            if (toolAction != null && toolAction.IsBusy)
                return true;

            if (spellAction != null && spellAction.IsBusy)
                return true;

            return false;
        }
    }
}
