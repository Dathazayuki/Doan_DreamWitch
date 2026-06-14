using System;
using System.Reflection;
using UnityEngine;

namespace DreamKnight.Player
{
    public class PooledVfxAutoRelease : MonoBehaviour
    {
        [SerializeField] private float minActiveTime = 0.05f;
        [SerializeField] private float maxActiveTime = 8f;

        private VfxPoolManager manager;
        private int prefabId;
        private float aliveTimer;
        private bool initialized;
        private bool releaseRequested;
        private bool releasingByPool;

        private static Type vfxType;
        private static PropertyInfo aliveParticleCountProperty;
        private static MethodInfo hasAnySystemAwakeMethod;

        public void Initialize(VfxPoolManager poolManager, int sourcePrefabId)
        {
            manager = poolManager;
            prefabId = sourcePrefabId;
            initialized = true;
            releaseRequested = false;
            releasingByPool = false;
            aliveTimer = 0f;

            CacheVfxReflection();
        }

        private void OnEnable()
        {
            releaseRequested = false;
            releasingByPool = false;
            aliveTimer = 0f;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            if (!initialized || manager == null)
                return;

            if (releaseRequested || releasingByPool)
                return;

            // Trường hợp VFX prefab tự SetActive(false): defer sang frame tiếp tránh SetParent trong deactivation callback
            releaseRequested = true;
            releasingByPool = true;
            manager.DeferRelease(gameObject, prefabId);
        }

        private void Update()
        {
            if (!initialized || releaseRequested || manager == null)
                return;

            aliveTimer += Time.deltaTime;
            if (aliveTimer < minActiveTime)
                return;

            if (aliveTimer >= maxActiveTime || IsFinished())
            {
                releaseRequested = true;
                releasingByPool = true;
                manager.Release(gameObject, prefabId);
            }
        }

        private bool IsFinished()
        {
            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            bool hasParticleSystem = particleSystems.Length > 0;
            bool anyParticleAlive = false;

            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i] != null && particleSystems[i].IsAlive(true))
                {
                    anyParticleAlive = true;
                    break;
                }
            }

            bool hasVfxComponent = false;
            bool anyVfxAlive = false;

            if (vfxType != null)
            {
                Component[] vfxComponents = GetComponentsInChildren(vfxType, true);
                hasVfxComponent = vfxComponents.Length > 0;

                for (int i = 0; i < vfxComponents.Length; i++)
                {
                    Component comp = vfxComponents[i];
                    if (comp == null) continue;

                    if (aliveParticleCountProperty != null)
                    {
                        object value = aliveParticleCountProperty.GetValue(comp);
                        if (value is uint count && count > 0)
                        {
                            anyVfxAlive = true;
                            break;
                        }
                    }

                    if (hasAnySystemAwakeMethod != null)
                    {
                        object value = hasAnySystemAwakeMethod.Invoke(comp, null);
                        if (value is bool awake && awake)
                        {
                            anyVfxAlive = true;
                            break;
                        }
                    }
                }
            }

            if (!hasParticleSystem && !hasVfxComponent)
                return aliveTimer >= minActiveTime;

            return !anyParticleAlive && !anyVfxAlive;
        }

        private static void CacheVfxReflection()
        {
            if (vfxType != null)
                return;

            vfxType = Type.GetType("UnityEngine.VFX.VisualEffect, Unity.VisualEffectGraph.Runtime");
            if (vfxType == null)
                return;

            aliveParticleCountProperty = vfxType.GetProperty("aliveParticleCount", BindingFlags.Instance | BindingFlags.Public);
            hasAnySystemAwakeMethod = vfxType.GetMethod("HasAnySystemAwake", BindingFlags.Instance | BindingFlags.Public);
        }
    }
}
