using System.Collections.Generic;
using DreamKnight.Interfaces;
using UnityEngine;

namespace DreamKnight.Systems.Skill
{
    [DisallowMultipleComponent]
    public class FlySwordSpell : MonoBehaviour, ISpellCast
    {
        private enum SwordState
        {
            AttachedToPlayer,
            ChasingEnemy
        }

        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private SpriteRenderer swordVisual;
        [SerializeField] private Collider2D hitCollider;

        [Header("Attached State")]
        [SerializeField] private int maxSwords = 5;
        [SerializeField] private Vector2[] multiOffsets = new Vector2[]
        {
            new Vector2(0.75f, 0.9f),
            new Vector2(-0.75f, 0.9f),
            new Vector2(1.25f, 0.4f),
            new Vector2(-1.25f, 0.4f),
            new Vector2(0f, 1.4f)
        };

        [Header("Targeting")]
        [SerializeField] private LayerMask enemyLayer = ~0;
        [SerializeField] private float searchRadius = 5f;
        [SerializeField] private float searchInterval = 0.2f;

        [Header("Attack State")]
        [SerializeField] private float lungeSpeed = 20f;
        [SerializeField] private float hitDistance = 0.35f;
        [SerializeField] private float activeDuration = 0f;

        private static readonly Dictionary<int, List<FlySwordSpell>> activeByOwner = new Dictionary<int, List<FlySwordSpell>>();
        private static readonly Collider2D[] searchResults = new Collider2D[16];

        private GameObject caster;
        private Transform casterTransform;
        private IDamageable currentTarget;
        private Collider2D currentTargetCollider;
        
        private SwordState state = SwordState.AttachedToPlayer;
        private float timer;
        private float searchTimer;
        private float damage;
        private int ownerInstanceId = -1;
        private int slotIndex = 0;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = GetComponentInChildren<Rigidbody2D>();

            if (swordVisual == null) swordVisual = GetComponentInChildren<SpriteRenderer>();

            if (hitCollider == null) hitCollider = GetComponent<Collider2D>();
            if (hitCollider == null) hitCollider = GetComponentInChildren<Collider2D>();

            if (hitCollider != null && !hitCollider.isTrigger)
                hitCollider.isTrigger = true;

            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
            }
        }

        private void OnDisable()
        {
            UnregisterOwner();
            currentTarget = null;
            currentTargetCollider = null;
            caster = null;
            casterTransform = null;
            state = SwordState.AttachedToPlayer;
            transform.SetParent(null);
        }

        public void Cast(GameObject casterObject, SpellLevelData levelData)
        {
            if (casterObject == null)
            {
                Destroy(gameObject);
                return;
            }

            int newOwnerId = casterObject.GetInstanceID();
            if (!activeByOwner.TryGetValue(newOwnerId, out List<FlySwordSpell> list))
            {
                list = new List<FlySwordSpell>();
                activeByOwner[newOwnerId] = list;
            }

            list.RemoveAll(s => s == null);

            if (list.Count >= maxSwords)
            {
                // Nếu vượt quá giới hạn (maxSwords), hủy luôn thanh gươm mới và kết thúc
                Destroy(gameObject);
                return;
            }

            HashSet<int> usedSlots = new HashSet<int>();
            foreach (var s in list)
            {
                if (s.state == SwordState.AttachedToPlayer)
                    usedSlots.Add(s.slotIndex);
            }

            slotIndex = 0;
            while (usedSlots.Contains(slotIndex)) slotIndex++;

            RegisterOwner(casterObject);
            Refresh(casterObject, levelData);
            gameObject.SetActive(true);
        }

        private void Refresh(GameObject casterObject, SpellLevelData levelData)
        {
            caster = casterObject;
            casterTransform = casterObject.transform;
            damage = levelData != null ? Mathf.Max(0f, levelData.damage) : 0f;
            
            timer = 0f;
            searchTimer = 0f;
            currentTarget = null;
            currentTargetCollider = null;
            
            EnterAttachedState();
        }

        private void EnterAttachedState()
        {
            state = SwordState.AttachedToPlayer;
            transform.SetParent(casterTransform);
            
            Vector2 offset = multiOffsets.Length > 0 ? multiOffsets[slotIndex % multiOffsets.Length] : Vector2.zero;
            transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            transform.localRotation = Quaternion.identity;
        }

        private void EnterChasingState(IDamageable target, Collider2D targetCollider)
        {
            state = SwordState.ChasingEnemy;
            currentTarget = target;
            currentTargetCollider = targetCollider;
            
            transform.SetParent(null);
             // Quan trọng: Sau khi tách khỏi Player, ép cứng LocalScale về 1,1,1 
            // để đảm bảo hàm tính góc quay (Atan2) không bị lật ngược hình ảnh (đuôi kiếm quay về trước).
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (casterTransform == null && state == SwordState.AttachedToPlayer)
            {
                Destroy(gameObject);
                return;
            }

            timer += Time.deltaTime;
            if (activeDuration > 0f && timer >= activeDuration)
            {
                Destroy(gameObject);
                return;
            }

            if (state == SwordState.AttachedToPlayer)
            {
                UpdateAttached();
            }
            else if (state == SwordState.ChasingEnemy)
            {
                UpdateChasing();
                
                // Di chuyển ngay trong Update để đảm bảo chắc chắn FlySword sẽ bay
                // Bỏ qua Rigidbody2D.MovePosition vì nếu RB bị lỗi (Static, Freeze, Sleep) nó sẽ đứng im
                if (currentTarget != null && currentTarget.IsAlive)
                {
                    Vector2 targetPos = GetTargetPosition(currentTarget, currentTargetCollider);
                    transform.position = Vector2.MoveTowards(transform.position, targetPos, lungeSpeed * Time.deltaTime);
                }
            }
        }

        private void UpdateAttached()
        {
            // Ép cứng Local Scale về 1,1,1 và Global Rotation về 0 để không bị xoay/lật theo Player
            transform.localScale = new Vector3(1, 1, 1);
            transform.localRotation = Quaternion.Euler(0, 0, -15);

            searchTimer += Time.deltaTime;
            if (searchTimer >= searchInterval)
            {
                searchTimer = 0f;
                if (FindNearestEnemy(out IDamageable enemy, out Collider2D col))
                {
                    EnterChasingState(enemy, col);
                }
            }
        }

        private void UpdateChasing()
        {
            if (currentTarget == null || !currentTarget.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 currentPosition = transform.position;
            Vector2 targetPosition = GetTargetPosition(currentTarget, currentTargetCollider);
            Vector2 direction = targetPosition - currentPosition;

            if (direction.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (direction.sqrMagnitude <= hitDistance * hitDistance)
            {
                currentTarget.TakeDamage(damage, caster);
                Destroy(gameObject);
            }
        }

        private Vector2 GetTargetPosition(IDamageable damageable, Collider2D preferredCollider)
        {
            if (preferredCollider != null) return preferredCollider.transform.position;
            
            MonoBehaviour behaviour = damageable as MonoBehaviour;
            if (behaviour != null) return behaviour.transform.position;

            return transform.position;
        }

        private bool FindNearestEnemy(out IDamageable nearest, out Collider2D nearestCollider)
        {
            nearest = null;
            nearestCollider = null;

            if (casterTransform == null) return false;

            Vector2 center = transform.position;
            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.layerMask = enemyLayer;
            filter.useTriggers = true; // Bắt buộc bật để nhận diện được các Collider isTrigger = true
            
            int count = Physics2D.OverlapCircle(center, searchRadius, filter, searchResults);
            
            if (count == 0) return false;

            float nearestDistanceSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = searchResults[i];
                if (hit == null) continue;

                if (caster != null && hit.transform.root == caster.transform.root) continue;

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                float distSqr = ((Vector2)transform.position - (Vector2)hit.transform.position).sqrMagnitude;
                if (distSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distSqr;
                    nearest = damageable;
                    nearestCollider = hit;
                }
            }

            return nearest != null;
        }

        private void RegisterOwner(GameObject casterObject)
        {
            UnregisterOwner();
            if (casterObject == null) return;
            ownerInstanceId = casterObject.GetInstanceID();
            
            if (!activeByOwner.TryGetValue(ownerInstanceId, out List<FlySwordSpell> list))
            {
                list = new List<FlySwordSpell>();
                activeByOwner[ownerInstanceId] = list;
            }
            if (!list.Contains(this))
                list.Add(this);
        }

        private void UnregisterOwner()
        {
            if (ownerInstanceId >= 0 && activeByOwner.TryGetValue(ownerInstanceId, out List<FlySwordSpell> list))
            {
                list.Remove(this);
                if (list.Count == 0) activeByOwner.Remove(ownerInstanceId);
            }
            ownerInstanceId = -1;
        }
    }
}