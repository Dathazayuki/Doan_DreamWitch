using DreamKnight.Enemy;
using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class MvEm0470 : MvEnemyBase
    {
        [Header("Em0470 FireBall Turret")]
        [SerializeField] private GameObject fireBallPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float fireBallDamage = 12f;
        [SerializeField] private float shootCooldown = 1.2f;

        protected override string IdleStateName => "Idle";
        protected override string RunStateName => "Idle";
        protected override string AttackSignStateName => "AtkSign";
        protected override string AttackStateName => "Atk";
        protected override string HitStateName => "Hit";
        protected override string DeadStateName => "Death";

        private float nextShootTime;

        protected override EnemyState CreateIdleState(EnemyContext context)
            => new Em0470IdleState(context, (byte)AsCommon.Idle, "Em0470Idle");

        protected override EnemyState CreateRunState(EnemyContext context)
            => new Em0470IdleState(context, (byte)AsCommon.Run, "Em0470RunAsIdle");

        protected override EnemyState CreateAttackState(EnemyContext context)
            => new Em0470ShootState(context);

        public void TickStationaryTurret()
        {
            PlayIdleMotion();

            if (CanStartShootSequence())
                ChangeEnemyState(AttackStateId);
        }

        public bool CanStartShootSequence()
        {
            if (!IsAlive)
                return false;

            if (Time.time < nextShootTime)
                return false;

            if (ActiveAttack == null || !ActiveAttack.HasAttackTrigger)
                return false;

            return ActiveAttack.IsPlayerInsideAttackTrigger();
        }

        public void BeginShootCooldown()
        {
            nextShootTime = Time.time + Mathf.Max(0.05f, shootCooldown);
        }

        public void ShootFireBall()
        {
            if (fireBallPrefab == null)
                return;

            Transform sp = spawnPoint != null ? spawnPoint : transform;
            Vector2 direction = ResolveShootDirection();

            FireBall fireBallComp = fireBallPrefab.GetComponent<FireBall>();
            if (fireBallComp != null)
            {
                FireBall fireBall = FireBallPoolManager.Instance.Spawn(fireBallComp, sp.position, Quaternion.identity);
                if (fireBall != null)
                    fireBall.Initialize(gameObject, fireBallDamage, direction);

                return;
            }

            GameObject go = Instantiate(fireBallPrefab, sp.position, Quaternion.identity);
            FireBall spawned = go.GetComponent<FireBall>();
            if (spawned != null)
                spawned.Initialize(gameObject, fireBallDamage, direction);
        }

        private Vector2 ResolveShootDirection()
        {
            return transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
        }
    }
}
