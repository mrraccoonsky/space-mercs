using System;
using UnityEngine;
using Data.Actor;
using ECS.Components;
using ECS.Utils;
using Tools;

namespace Actor
{
    using Leopotam.EcsLite;
    using NaughtyAttributes;
    
    public class ARagdoll : MonoBehaviour, IActorModule
    {
        [Serializable]
        private class RagdollBody
        {
            public Rigidbody rigidbody;
            public float forceWeight = 1f;

            [Space]
            [ReadOnly, AllowNesting] public CharacterJoint joint;
            [ReadOnly, AllowNesting] public bool jointTransformStored;
            [ReadOnly, AllowNesting] public Quaternion defaultJointRot;

            [Space]
            [ReadOnly, AllowNesting] public Collider collider;
            [ReadOnly, AllowNesting] public bool colliderTransformStored;
            [ReadOnly, AllowNesting] public Quaternion defaultColliderRot;
        }
        
        [SerializeField] private float forceMult = 1f;          // mult of external push force
        [SerializeField] private float velocityMult = 1.25f;    // mult of movement velocity
        [SerializeField, AllowNesting] private RagdollBody[] ragdollBodies;

        [ReadOnly, SerializeField] private bool currentState = true;
        
        public bool IsEnabled { get; private set; }
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }
        
        public void Init(ActorConfig cfg, int entityId, EcsWorld world)
        {
            IsEnabled = enabled;
            if (!IsEnabled) return;
            
            if (ragdollBodies == null || ragdollBodies.Length == 0)
            {
                DebCon.Warn($"Ragdoll bodies are not set on {gameObject.name}", "ARagdoll", gameObject);
                return;
            }
            
            EntityId = entityId;
            World = world;
            
            // add component to pool
            var ragdollPool = World.GetPool<RagdollComponent>();
            ref var aRagdoll = ref ragdollPool.Add(EntityId);
            aRagdoll.Module = this;
            
            foreach (var r in ragdollBodies)
            {
                if (r.rigidbody == null)
                {
                    DebCon.Warn($"Rigidbody is null", "ARagdoll", gameObject);
                    continue;
                }
                
                // joint first time init
                if (!r.jointTransformStored && r.rigidbody.TryGetComponent(out r.joint))
                {
                    r.defaultJointRot = r.joint.transform.rotation;
                    r.jointTransformStored = true;
                    // DebCon.Log($"Storing default JOINT rotation of {r.Rigidbody.gameObject.name} = {r.DefaultJointRot.eulerAngles}", "ARagdoll", r.Rigidbody.gameObject);
                }

                // collider first time init
                if (!r.colliderTransformStored && r.rigidbody.TryGetComponent(out r.collider))
                {
                    r.defaultColliderRot = r.collider.transform.localRotation;
                    r.colliderTransformStored = true;
                    // DebCon.Log($"Storing default COLLIDER rotation of {r.Rigidbody.gameObject.name} = {r.DefaultColliderRot.eulerAngles}", "ARagdoll", r.Rigidbody.gameObject);
                }
            }
        }

        public void Reset()
        {
            if (!enabled) return;
            
            SetRagdollEnabled(false);
        }

        public void SyncEcsState()
        {
            if (ragdollBodies == null || ragdollBodies.Length == 0) return;
            
            if (EcsUtils.HasCompInPool<RagdollComponent>(World, EntityId, out var ragdollPool))
            {
                ref var aRagdoll = ref ragdollPool.Get(EntityId);
                aRagdoll.Module = this;
            }
        }

        public void Tick(float dt)
        {
            // ...
        }

        public void SetRagdollEnabled(bool state)
        {
            if (currentState == state) return;
            if (ragdollBodies == null || ragdollBodies.Length == 0) return;
            
            foreach (var r in ragdollBodies)
            {
                if (r.rigidbody != null)
                {
                    r.rigidbody.isKinematic = !state;
                    
                    if (state)
                    {
                        r.rigidbody.linearVelocity = Vector3.zero;
                        r.rigidbody.angularVelocity = Vector3.zero;
                    }
                    else
                    {
                        r.rigidbody.ResetCenterOfMass();
                        r.rigidbody.ResetInertiaTensor();
                    }
                }

                if (r.joint != null)
                {
                    if (!state)
                    {
                        r.joint.transform.localRotation = r.defaultJointRot;
                        // DebCon.Log($"Resetting JOINT rotation of {r.Rigidbody.gameObject.name} to default value = {r.DefaultJointRot.eulerAngles}", "ARagdoll", r.Rigidbody.gameObject);
                    }
                }

                if (r.collider != null)
                {
                    if (!state)
                    {
                        r.collider.transform.localRotation = r.defaultColliderRot;
                        // DebCon.Log($"Resetting COLLIDER rotation of {r.Rigidbody.gameObject.name} to default value = {r.DefaultColliderRot.eulerAngles}", "ARagdoll", r.Rigidbody.gameObject);
                    }

                    r.collider.enabled = state;
                }
            }

            currentState = state;
            DebCon.Log($"Setting ragdoll enabled to {state}", "ARagdoll", gameObject);
        }

        public void AddForce(Vector3 force, Vector3 velocity, Vector3 position)
        {
            if (ragdollBodies == null || ragdollBodies.Length == 0) return;
            
            foreach (var r in ragdollBodies)
            {
                if (r.rigidbody == null)
                {
                    DebCon.Warn($"Rigidbody is null", "ARagdoll", gameObject);
                    continue;
                }
                
                r.rigidbody.AddForce(velocity * velocityMult, ForceMode.Force);
                r.rigidbody.AddForceAtPosition(force * forceMult * r.forceWeight, position, ForceMode.Impulse);
            }
        }
    }
}