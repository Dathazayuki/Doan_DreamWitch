using DreamKnight.Interfaces;
using UnityEngine;

namespace DreamKnight.Systems.Culling
{
    [DisallowMultipleComponent]
    public class DistanceCullingTarget : MonoBehaviour, ICullable
    {
        [Header("Culling Type")]
        [SerializeField] private CullingTargetType targetType = CullingTargetType.Enemy;

        [Header("Hysteresis Distance Override (0 = use CullingManager defaults)")]
        [SerializeField] private float overrideEnableDistance = 0f;
        [SerializeField] private float overrideDisableDistance = 0f;

        [Header("Components disabled while culled (empty = auto detect)")]
        [SerializeField] private MonoBehaviour[] componentsToCull;

        private bool isCulled;
        private bool isRoomSleeping;
        private Rigidbody2D rb;
        private RigidbodyConstraints2D originalConstraints;
        private Animator[] cachedAnimators;
        private ParticleSystem[] cachedParticleSystems;
        private bool[] componentEnabledBeforeCull;
        private bool[] animatorEnabledBeforeCull;
        private bool[] particleWasPlayingBeforeCull;

        public bool IsCulled => isCulled;
        public bool IsRoomSleeping => isRoomSleeping;
        public float OverrideEnableDistance => overrideEnableDistance;
        public float OverrideDisableDistance => overrideDisableDistance;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
                originalConstraints = rb.constraints;

            cachedAnimators = GetComponentsInChildren<Animator>(true);
            cachedParticleSystems = GetComponentsInChildren<ParticleSystem>(true);

            if (componentsToCull == null || componentsToCull.Length == 0)
                AutoDetectComponents();
            else if (targetType == CullingTargetType.Enemy)
                AddCoreEnemyComponentsToConfiguredList();
        }

        private void OnEnable() => CullingManager.Instance?.Register(this);
        private void OnDisable() => CullingManager.Instance?.Unregister(this);

        public void Cull()
        {
            if (isCulled) return;
            isCulled = true;
            ApplyCull();
        }

        public void UnCull()
        {
            if (!isCulled) return;
            if (isRoomSleeping) return;

            isCulled = false;
            ApplyUnCull();
        }

        public void SetRoomSleep(bool sleeping)
        {
            isRoomSleeping = sleeping;
            if (sleeping)
            {
                isCulled = true;
                ApplyCull();
                return;
            }

            isCulled = false;
            ApplyUnCull();
        }

        private void ApplyCull()
        {
            DisableConfiguredComponents();

            if (targetType == CullingTargetType.Enemy && rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            if (targetType == CullingTargetType.Enemy)
                DisableAnimators();

            if (targetType == CullingTargetType.Enemy || targetType == CullingTargetType.Vfx)
                PauseParticles();
        }

        private void ApplyUnCull()
        {
            RestoreConfiguredComponents();

            if (targetType == CullingTargetType.Enemy && rb != null)
                rb.constraints = originalConstraints;

            if (targetType == CullingTargetType.Enemy)
                RestoreAnimators();

            if (targetType == CullingTargetType.Enemy || targetType == CullingTargetType.Vfx)
                RestoreParticles();
        }

        private void DisableConfiguredComponents()
        {
            if (componentsToCull == null)
                return;

            if (componentEnabledBeforeCull == null || componentEnabledBeforeCull.Length != componentsToCull.Length)
                componentEnabledBeforeCull = new bool[componentsToCull.Length];

            for (int i = 0; i < componentsToCull.Length; i++)
            {
                MonoBehaviour component = componentsToCull[i];
                if (component == null)
                    continue;

                componentEnabledBeforeCull[i] = component.enabled;
                component.enabled = false;
            }
        }

        private void RestoreConfiguredComponents()
        {
            if (componentsToCull == null)
                return;

            for (int i = 0; i < componentsToCull.Length; i++)
            {
                MonoBehaviour component = componentsToCull[i];
                if (component == null)
                    continue;

                bool shouldEnable = componentEnabledBeforeCull == null
                    || i >= componentEnabledBeforeCull.Length
                    || componentEnabledBeforeCull[i];
                component.enabled = shouldEnable;
            }
        }

        private void DisableAnimators()
        {
            if (cachedAnimators == null)
                return;

            if (animatorEnabledBeforeCull == null || animatorEnabledBeforeCull.Length != cachedAnimators.Length)
                animatorEnabledBeforeCull = new bool[cachedAnimators.Length];

            for (int i = 0; i < cachedAnimators.Length; i++)
            {
                Animator animator = cachedAnimators[i];
                if (animator == null)
                    continue;

                animatorEnabledBeforeCull[i] = animator.enabled;
                animator.enabled = false;
            }
        }

        private void RestoreAnimators()
        {
            if (cachedAnimators == null)
                return;

            for (int i = 0; i < cachedAnimators.Length; i++)
            {
                Animator animator = cachedAnimators[i];
                if (animator == null)
                    continue;

                bool shouldEnable = animatorEnabledBeforeCull == null
                    || i >= animatorEnabledBeforeCull.Length
                    || animatorEnabledBeforeCull[i];
                animator.enabled = shouldEnable;
            }
        }

        private void PauseParticles()
        {
            if (cachedParticleSystems == null)
                return;

            if (particleWasPlayingBeforeCull == null || particleWasPlayingBeforeCull.Length != cachedParticleSystems.Length)
                particleWasPlayingBeforeCull = new bool[cachedParticleSystems.Length];

            for (int i = 0; i < cachedParticleSystems.Length; i++)
            {
                ParticleSystem particle = cachedParticleSystems[i];
                if (particle == null)
                    continue;

                particleWasPlayingBeforeCull[i] = particle.isPlaying;
                if (particle.isPlaying)
                    particle.Pause();
            }
        }

        private void RestoreParticles()
        {
            if (cachedParticleSystems == null)
                return;

            for (int i = 0; i < cachedParticleSystems.Length; i++)
            {
                ParticleSystem particle = cachedParticleSystems[i];
                if (particle == null)
                    continue;

                bool shouldResume = particleWasPlayingBeforeCull != null
                    && i < particleWasPlayingBeforeCull.Length
                    && particleWasPlayingBeforeCull[i];
                if (shouldResume && particle.isPaused)
                    particle.Play();
            }
        }

        private void AutoDetectComponents()
        {
            switch (targetType)
            {
                case CullingTargetType.Enemy:
                    System.Collections.Generic.List<MonoBehaviour> enemyComponents =
                        new System.Collections.Generic.List<MonoBehaviour>();

                    AddComponentIfFound(enemyComponents, GetComponent<Mv.MvEnemyBase>());
                    AddComponentIfFound(enemyComponents, GetComponent<Mv.MvAttack>());
                    AddComponentIfFound(enemyComponents, GetComponentInChildren<Mv.MvAttack>(true));
                    AddComponentIfFound(enemyComponents, GetComponentInChildren<MvAnimEventLite>(true));
                    AddComponentIfFound(enemyComponents, GetComponentInChildren<Mv.EnemyVfxEvents>(true));

                    componentsToCull = enemyComponents.ToArray();
                    break;

                case CullingTargetType.Projectile:
                    MonoBehaviour[] all = GetComponents<MonoBehaviour>();
                    System.Collections.Generic.List<MonoBehaviour> list =
                        new System.Collections.Generic.List<MonoBehaviour>();

                    for (int i = 0; i < all.Length; i++)
                    {
                        MonoBehaviour component = all[i];
                        if (component != null && component != this)
                            list.Add(component);
                    }

                    componentsToCull = list.ToArray();
                    break;

                case CullingTargetType.Vfx:
                    componentsToCull = new MonoBehaviour[0];
                    break;
            }
        }

        private void AddCoreEnemyComponentsToConfiguredList()
        {
            System.Collections.Generic.List<MonoBehaviour> list =
                new System.Collections.Generic.List<MonoBehaviour>();

            if (componentsToCull != null)
            {
                for (int i = 0; i < componentsToCull.Length; i++)
                    AddComponentIfFound(list, componentsToCull[i]);
            }

            AddComponentIfFound(list, GetComponent<Mv.MvEnemyBase>());
            AddComponentIfFound(list, GetComponent<Mv.MvAttack>());
            AddComponentIfFound(list, GetComponentInChildren<Mv.MvAttack>(true));
            AddComponentIfFound(list, GetComponentInChildren<MvAnimEventLite>(true));
            AddComponentIfFound(list, GetComponentInChildren<Mv.EnemyVfxEvents>(true));

            componentsToCull = list.ToArray();
        }

        private void AddComponentIfFound(System.Collections.Generic.List<MonoBehaviour> list, MonoBehaviour component)
        {
            if (component == null || component == this || list.Contains(component))
                return;

            list.Add(component);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            float enableDist = overrideEnableDistance > 0f ? overrideEnableDistance : 18f;
            float disableDist = overrideDisableDistance > 0f ? overrideDisableDistance : 22f;

            UnityEditor.Handles.color = new Color(0.2f, 0.9f, 0.3f, 0.4f);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, enableDist);

            UnityEditor.Handles.color = new Color(0.9f, 0.2f, 0.2f, 0.4f);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, disableDist);
        }
#endif
    }
}
