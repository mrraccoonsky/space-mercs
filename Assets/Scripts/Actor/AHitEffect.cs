using System;
using UnityEngine;
using Data.Actor;
using DI.Services;
using ECS.Components;
using ECS.Utils;
using Tools;

namespace Actor
{
    using Leopotam.EcsLite;
    using NaughtyAttributes;
    using Zenject;
    
    public class AHitEffect : MonoBehaviour, IActorModule
    {
        [Serializable]
        private class HitEffectData
        {
            public bool isSkinned;
            [HideIf("isSkinned"), AllowNesting] public MeshRenderer mesh;
            [ShowIf("isSkinned"), AllowNesting] public SkinnedMeshRenderer skinnedMesh;
            
            public MaterialPropertyBlock MatPropBlock;
        }
        
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        
        [Header("Visuals")]
        [SerializeField] private GameObject hitFxPrefab;

        [Header("Material Effects")]
        [SerializeField, ColorUsage(false, true)] private Color color = Color.white;
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private HitEffectData[] effects;
        
        private IFxService _fxService;
        private float _timer = -1f;
        
        public bool IsEnabled { get; private set; }
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set;  }
        
        [Inject]
        public void Construct(IFxService fxService)
        {
            _fxService = fxService;
        }
        
        public void Init(ActorConfig cfg, int entityId, EcsWorld world)
        {
            IsEnabled = enabled;
            if (!IsEnabled) return;
            
            EntityId = entityId;
            World = world;
            
            if (effects == null || effects.Length == 0)
            {
                DebCon.Warn($"Hit effects are not set on {gameObject.name}", "ARagdoll", gameObject);
                return;
            }
            
            foreach (var e in effects)
            {
                if (e == null)
                {
                    DebCon.Err("Hit effect is null!", "AHitEffect", gameObject);
                    continue;
                }

                if (e.MatPropBlock == null)
                {
                    e.MatPropBlock = new MaterialPropertyBlock();
                    DebCon.Log("Creating new material block...", "AHitEffect", gameObject);
                }
            }

            Reset();
        }

        public void Reset()
        {
            if (!enabled) return;
            if (effects == null || effects.Length == 0) return;
            
            foreach (var e in effects)
            {
                if (e == null)
                {
                    DebCon.Err("Hit effect is null!", "AHitEffect", gameObject);
                    continue;
                }

                if (e.MatPropBlock == null)
                {
                    DebCon.Err("Material property block is null!", "AHitEffect", gameObject);
                    continue;
                }
                
                e.MatPropBlock.SetColor(EmissionColor, Color.black);
                
                if (e.isSkinned)
                {
                    if (e.skinnedMesh == null || !e.skinnedMesh.HasPropertyBlock()) continue;
                    e.skinnedMesh.SetPropertyBlock(null);
                }
                else
                {
                    if (e.mesh == null || !e.mesh.HasPropertyBlock()) continue;
                    e.mesh.SetPropertyBlock(null);
                }
            }
            
            _timer = -1f;
        }

        public void SyncEcsState()
        {
            // ...
        }

        public void Tick(float dt)
        {
            if (!enabled) return;
            if (effects == null || effects.Length == 0) return;
            if (!EcsUtils.HasCompInPool<HealthComponent>(World, EntityId, out var healthPool)) return;
            
            ref var aHealth = ref healthPool.Get(EntityId);
            var isHit = aHealth.IsHit;
                    
            if (isHit)
            {
                PlayHitFx();
                _timer = duration;
            }
            else
            {
                if (_timer <= 0f)
                {
                    Reset();
                    return;
                }
                
                _timer -= dt;
            }

            UpdateHitEffects();
        }
        
        private void PlayHitFx()
        {
            if (hitFxPrefab == null) return;
            if (!EcsUtils.HasCompInPool<HealthComponent>(World, EntityId, out var healthPool)) return;

            var aHealth = healthPool.Get(EntityId);
            if (aHealth.LastHitIgnoreFx) return;
            
            var lookDir = aHealth.LastHitDir;
            lookDir.y = 0f;
                
            _fxService.Spawn(hitFxPrefab, aHealth.LastHitPos, Quaternion.LookRotation(lookDir), Vector3.one);
        }

        private void UpdateHitEffects()
        {
            var value = _timer / duration;
            foreach (var e in effects)
            {
                if (e?.MatPropBlock == null) continue;
                e.MatPropBlock.SetColor(EmissionColor, color * value);
                
                if (e.isSkinned)
                {
                    if (e.skinnedMesh == null) continue;
                    e.skinnedMesh.SetPropertyBlock(e.MatPropBlock);
                }
                else
                {
                    if (e.mesh == null) continue;
                    e.mesh.SetPropertyBlock(e.MatPropBlock);
                }
            }
        }
    }
}