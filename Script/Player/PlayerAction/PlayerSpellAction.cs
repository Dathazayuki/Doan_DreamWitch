using System.Collections;
using DreamKnight.Systems.Skill;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class PlayerSpellAction : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private SpellManager spellManager;
        [SerializeField] private SpellEquipSO spellEquip;
        [SerializeField] private DreamKnight.Systems.Skill.SpellUseSystem spellUseSystem;

        [Header("Animation")]
        [SerializeField] private bool debugSpellFlow = true;

        private PlayerAnimationController animationController;
        private PlayerStats playerStats;
        private bool isBusy;

        public bool IsBusy => isBusy;

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

            if (spellManager == null)
                spellManager = GetComponentInParent<SpellManager>();

            if (spellManager == null)
                spellManager = FindAnyObjectByType<SpellManager>();

            if (playerStats == null)
                playerStats = GetComponentInParent<PlayerStats>();

            if (playerStats == null && playerController != null)
                playerStats = playerController.Stats;

            if (animationController == null && playerController != null)
                animationController = playerController.AnimationController;

            if (spellManager == null && playerController != null)
                spellManager = playerController.GetComponentInParent<SpellManager>();

            if (spellUseSystem == null)
            {
                Transform owner = playerController != null ? playerController.transform : transform.root;
                spellUseSystem = DreamKnight.Systems.Skill.SpellUseSystem.GetOrCreate(owner, spellManager, spellEquip, playerStats);
            }
            else
            {
                spellUseSystem.Configure(spellManager, spellEquip, playerStats);
            }

            if (debugSpellFlow)
            {
                Debug.Log($"[PlayerSpellAction] ResolveReferences -> playerController={(playerController != null ? playerController.name : "null")}, spellManager={(spellManager != null ? spellManager.name : "null")}, spellEquip={(spellEquip != null ? spellEquip.name : "null")}, playerStats={(playerStats != null ? "ok" : "null")}, spellUseSystem={(spellUseSystem != null ? spellUseSystem.name : "null")}");
            }
        }

        public bool TryUse()
        {
            ResolveReferences();
            if (isBusy)
            {
                if (debugSpellFlow)
                    Debug.Log("[PlayerSpellAction] TryUse blocked: action is busy.");
                return false;
            }

            if (spellManager == null || spellEquip == null || spellEquip.EquippedSpell == null)
            {
                if (debugSpellFlow)
                    Debug.Log($"[PlayerSpellAction] TryUse failed: spellManager={(spellManager != null ? "ok" : "null")}, spellEquip={(spellEquip != null ? spellEquip.name : "null")}, equippedSpell={(spellEquip != null && spellEquip.EquippedSpell != null ? spellEquip.EquippedSpell.name : "null")}");
                return false;
            }

            // Delegate usage and mana/cooldown handling to SpellUseSystem
            if (spellUseSystem == null)
            {
                ResolveReferences();
                if (spellUseSystem == null)
                {
                    if (debugSpellFlow)
                        Debug.Log("[PlayerSpellAction] TryUse failed: spellUseSystem is still null after ResolveReferences().");
                return false;
                }
            }

            bool used = spellUseSystem.TryUseEquippedSpell();
            if (debugSpellFlow)
                Debug.Log($"[PlayerSpellAction] TryUse -> spellUseSystem.TryUseEquippedSpell() returned {used}");

            if (!used)
                return false;

            StartCoroutine(PlayAnimationRoutine());
            if (debugSpellFlow)
                Debug.Log("[PlayerSpellAction] TryUse success: animation started.");
            return true;
        }

        private IEnumerator PlayAnimationRoutine()
        {
            isBusy = true;
            if (debugSpellFlow)
                Debug.Log("[PlayerSpellAction] PlayAnimationRoutine started.");
            animationController?.ForcePlayAnimation(PlayerAnimationController.SKILL_ONESHOT);
            yield return WaitForAnimationFinished(PlayerAnimationController.SKILL_ONESHOT, 1.2f);
            isBusy = false;
            ResumeMovementAnimation();
            if (debugSpellFlow)
                Debug.Log("[PlayerSpellAction] PlayAnimationRoutine finished.");
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
