using DreamKnight.Interfaces;
using UnityEngine;

namespace Mv
{
	public partial class MvEnemyBase
	{
		public abstract class MvActState_Em : EnemyState
		{
			protected MvEnemyBase Em => Context.Owner;

			protected MvActState_Em(EnemyContext context) : base(context) { }
		}

		public abstract class AsEm_Idle_Base : MvActState_Em
		{
			public override byte StateId => (byte)AsCommon.Idle;
			public override string StateName => "AsEm_Idle_Base";

			protected AsEm_Idle_Base(EnemyContext context) : base(context) { }

			public override void Enter()
			{
				Em?.PlayIdleMotion();
			}
		}

		public class AsEm_IdleOnly : AsEm_Idle_Base
		{
			public override string StateName => "AsEm_IdleOnly";

			public AsEm_IdleOnly(EnemyContext context) : base(context) { }

			public override void Tick()
			{
				Em?.PlayIdleMotion();
			}
		}

		public class AsEm_IdleSearch : AsEm_Idle_Base
		{
			public override string StateName => "AsEm_IdleSearch";

			public AsEm_IdleSearch(EnemyContext context) : base(context) { }

			public override void Tick()
			{
				if (Em == null) return;

				Em.PlayIdleMotion();
				if (Em.ShouldUseRunState())
					Em.ChangeEnemyState(Em.RunStateId);
			}
		}

		public class AsEm_Idle : AsEm_Idle_Base
		{
			public override string StateName => "AsEm_Idle";

			public AsEm_Idle(EnemyContext context) : base(context) { }

			public override void Tick()
			{
				if (Em == null) return;

				if (Em.IsAttackAnimLocked)
				{
					Em.ChangeEnemyState(Em.AttackStateId);
					return;
				}

				if (Em.HasTarget && Em.IsTargetInAttackRange && Em.CanStartAttackNow)
				{
					Em.ChangeEnemyState(Em.AttackStateId);
					return;
				}

				Em.PlayIdleMotion();
				if (Em.ShouldUseRunState())
					Em.ChangeEnemyState(Em.RunStateId);
			}
		}

		public class AsEm_Common_AtkAfter : AsEm_Idle_Base
		{
			public override byte StateId => (byte)AsCommon.AtkAfter;
			public override string StateName => "AsEm_Common_AtkAfter";

			public AsEm_Common_AtkAfter(EnemyContext context) : base(context) { }

			public override void Tick()
			{
				if (Em == null) return;

				Em.PlayIdleMotion();
				if (Em.IsAttackAnimLocked)
					return;

				if (Em.HasTarget && Em.IsTargetInAttackRange && Em.CanStartAttackNow)
				{
					Em.ChangeEnemyState(Em.AttackStateId);
					return;
				}

				if (Em.ShouldUseRunState())
					Em.ChangeEnemyState(Em.RunStateId);
				else
					Em.ChangeEnemyState(Em.IdleStateId);
			}
		}

		public class AsEm_Common_Turn : AsEm_Idle_Base
		{
			public override byte StateId => (byte)AsCommon.Turn;
			public override string StateName => "AsEm_Common_Turn";

			public AsEm_Common_Turn(EnemyContext context) : base(context) { }

			public override void Tick()
			{
				if (Em == null) return;
				Em.FaceByDeltaX(Context.DeltaX);
				Em.ChangeEnemyState(Em.IdleStateId);
			}
		}

		public abstract class AsEm_Run_Base : MvActState_Em
		{
			public override byte StateId => (byte)AsCommon.Run;
			public override string StateName => "AsEm_Run_Base";

			protected AsEm_Run_Base(EnemyContext context) : base(context) { }

			public override void Tick()
			{
				if (Em == null) return;

				if (Em.IsAttackAnimLocked)
				{
					Em.ChangeEnemyState(Em.AttackStateId);
					return;
				}

				if (Em.HasTarget && Em.IsTargetInAttackRange && Em.CanStartAttackNow)
				{
					Em.ChangeEnemyState(Em.AttackStateId);
					return;
				}

				if (!Em.ShouldUseRunState())
				{
					Em.ChangeEnemyState(Em.IdleStateId);
					return;
				}

				Em.TickRunMotion(Context.DeltaX);
			}
		}

		public class AsEm_Run : AsEm_Run_Base
		{
			public override string StateName => "AsEm_Run";

			public AsEm_Run(EnemyContext context) : base(context) { }
		}

		/// <summary>Base chung cho mọi Attack state. Kế thừa trực tiếp từ MvActState_Em.</summary>
		public abstract class AsEm_Atk_Base : MvActState_Em
		{
			public override bool IsAttackState => true;
			protected AsEm_Atk_Base(EnemyContext context) : base(context) { }
		}

		public abstract class AsEm_AtkWithSign_Base : AsEm_Atk_Base
		{
			private bool startedAttack;

			public override string StateName => "AsEm_AtkWithSign_Base";

			protected AsEm_AtkWithSign_Base(EnemyContext context) : base(context) { }

			public override void Enter()
			{
				startedAttack = false;
				if (Em != null)
				{
					Em.FaceByDeltaX(Context.DeltaX);
					Em.BeginAttackSignIfNeeded();
				}
			}

			public override void Tick()
			{
				if (Em == null) return;

				if (!startedAttack)
				{
					Em.PlayAttackSignMotion(Context.DeltaX);

					if (!Em.IsAttackSignElapsed)
						return;

					Em.CancelAttackSign();
					startedAttack = Em.TryStartAttackAndTrigger();
					if (!startedAttack)
						Em.ChangeEnemyState(Em.AtkAfterStateId);

					return;
				}

				Em.PlayAttackMotion(Context.DeltaX);
				if (!Em.IsAttackAnimFinished())
					return;

				Em.ChangeEnemyState(Em.AtkAfterStateId);
			}
		}

		public class AsEm_DefaultAttack : AsEm_AtkWithSign_Base
		{
			public override byte StateId => (byte)AsCommon.Max;
			public override string StateName => "AsEm_DefaultAttack";

			public AsEm_DefaultAttack(EnemyContext context) : base(context) { }
		}

		public class AsEm_ShotBase : AsEm_AtkWithSign_Base
		{
			public override byte StateId => (byte)AsCommon.Max;
			public override string StateName => "AsEm_ShotBase";

			public AsEm_ShotBase(EnemyContext context) : base(context) { }
		}

		public class AsEm_SimpleShot : AsEm_ShotBase
		{
			public override string StateName => "AsEm_SimpleShot";

			public AsEm_SimpleShot(EnemyContext context) : base(context) { }
		}

		public class AsEm_TrgShot : AsEm_ShotBase
		{
			public override string StateName => "AsEm_TrgShot";

			public AsEm_TrgShot(EnemyContext context) : base(context) { }
		}

		public class AsEm_FlyChase : AsEm_Run_Base
		{
			public override string StateName => "AsEm_FlyChase";

			public AsEm_FlyChase(EnemyContext context) : base(context) { }
		}

		public class AsEm_Common_Hit : MvActState_Em
		{
			public override byte StateId => (byte)AsCommon.Hit;
			public override string StateName => "AsEm_Common_Hit";

			public AsEm_Common_Hit(EnemyContext context) : base(context) { }

			public override void Tick()
			{
				if (Em == null) return;

				Em.TickHitStunMotion();
				if (Em.IsHitStunActive)
					return;

				if (Em.HasTarget && Em.IsTargetInAttackRange && Em.CanStartAttackNow)
					Em.ChangeEnemyState(Em.AttackStateId);
				else if (Em.ShouldUseRunState())
					Em.ChangeEnemyState(Em.RunStateId);
				else
					Em.ChangeEnemyState(Em.IdleStateId);
			}
		}

		public class AsEm_Common_HitKnockBack : AsEm_Common_Hit
		{
			public override byte StateId => (byte)AsCommon.HitKnockBack;
			public override string StateName => "AsEm_Common_HitKnockBack";

			public AsEm_Common_HitKnockBack(EnemyContext context) : base(context) { }
		}

		public class AsEm_Common_HitKnockUp : AsEm_Common_Hit
		{
			public override byte StateId => (byte)AsCommon.HitKnockUp;
			public override string StateName => "AsEm_Common_HitKnockUp";

			public AsEm_Common_HitKnockUp(EnemyContext context) : base(context) { }
		}

		public class AsEm_Common_HitBounce : AsEm_Common_Hit
		{
			public override byte StateId => (byte)AsCommon.HitBounce;
			public override string StateName => "AsEm_Common_HitBounce";

			public AsEm_Common_HitBounce(EnemyContext context) : base(context) { }
		}

		public class AsEm_Common_HitNeedle : AsEm_Common_Hit
		{
			public override byte StateId => (byte)AsCommon.HitNeedle;
			public override string StateName => "AsEm_Common_HitNeedle";

			public AsEm_Common_HitNeedle(EnemyContext context) : base(context) { }
		}

		/// <summary>
		/// Base cho mọi state chỉ lock idle (Frozen, Stone, Stun, Cage, ...).
		/// Subclass chỉ cần override StateId và StateName.
		/// </summary>
		public abstract class AsEm_LockedIdle : MvActState_Em
		{
			protected AsEm_LockedIdle(EnemyContext context) : base(context) { }

			public override void Tick() => Em?.PlayIdleMotion();
		}

		public class AsEm_Common_Frozen : AsEm_LockedIdle
		{
			public override byte StateId => (byte)AsCommon.CondFrozen;
			public override string StateName => "AsEm_Common_Frozen";
			public AsEm_Common_Frozen(EnemyContext context) : base(context) { }
		}

		public class AsEm_Common_Stone : AsEm_LockedIdle
		{
			public override byte StateId => (byte)AsCommon.CondStone;
			public override string StateName => "AsEm_Common_Stone";
			public AsEm_Common_Stone(EnemyContext context) : base(context) { }
		}

		public class AsEm_Common_Stun : AsEm_LockedIdle
		{
			public override byte StateId => (byte)AsCommon.CondStun;
			public override string StateName => "AsEm_Common_Stun";
			public AsEm_Common_Stun(EnemyContext context) : base(context) { }
		}

		public class AsEm_Common_DeathPre : MvActState_Em
		{
			public override byte StateId => (byte)AsCommon.DeathPre;
			public override string StateName => "AsEm_Common_DeathPre";

			public AsEm_Common_DeathPre(EnemyContext context) : base(context) { }

			public override void Tick()
			{
				Em?.ChangeEnemyState(Em.DeadStateId);
			}
		}

		public class AsEm_Common_Death : MvActState_Em
		{
			public override byte StateId => (byte)AsCommon.Death;
			public override string StateName => "AsEm_Common_Death";

			public AsEm_Common_Death(EnemyContext context) : base(context) { }

			public override void Enter()
			{
				Em?.EnterDeadState();
			}

			public override void Tick()
			{
				Em?.TickDeadState();
			}
		}

		public class AsEm_Common_DeathMelt : AsEm_IdleOnly
		{
			public override string StateName => "AsEm_Common_DeathMelt";

			public AsEm_Common_DeathMelt(EnemyContext context) : base(context) { }
		}

		public class AsEm_BossDeathBase : AsEm_Common_Death
		{
			public override string StateName => "AsEm_BossDeathBase";

			public AsEm_BossDeathBase(EnemyContext context) : base(context) { }
		}

		public class AsEm_Common_Cage : AsEm_LockedIdle
		{
			public override byte StateId => (byte)AsCommon.Cage;
			public override string StateName => "AsEm_Common_Cage";
			public AsEm_Common_Cage(EnemyContext context) : base(context) { }
		}

		public class AsEm_Common_Inactive : MvActState_Em
		{
			public override byte StateId => (byte)AsCommon.Inactive;
			public override string StateName => "AsEm_Common_Inactive";
			public AsEm_Common_Inactive(EnemyContext context) : base(context) { }

			// Inactive dừng hẳn (không SetRun idle) nên không dùng AsEm_LockedIdle
			public override void Tick() => Em?.StopHorizontalMotion();
		}

		public class AsEmBoss_KnockOut : AsEm_LockedIdle
		{
			public override byte StateId => (byte)AsCommon.CondStun;
			public override string StateName => "AsEmBoss_KnockOut";
			public AsEmBoss_KnockOut(EnemyContext context) : base(context) { }
		}

		public class AsEm_Eat : AsEm_IdleOnly
		{
			public override string StateName => "AsEm_Eat";

			public AsEm_Eat(EnemyContext context) : base(context) { }
		}

		public class AsEm_Common_DeathPotDive : AsEm_LockedIdle
		{
			public override byte StateId => (byte)AsCommon.DeathPotDive;
			public override string StateName => "AsEm_Common_DeathPotDive";
			public AsEm_Common_DeathPotDive(EnemyContext context) : base(context) { }
		}

		/// <summary>Base cho JumpStart và Jump — cả hai đều chuyển ngay sang RunState khi Tick.</summary>
		public abstract class AsEm_JumpBase : MvActState_Em
		{
			public override byte StateId => (byte)AsCommon.Max;
			protected AsEm_JumpBase(EnemyContext context) : base(context) { }
			public override void Tick() => Em?.ChangeEnemyState(Em.RunStateId);
		}

		public abstract class AsEm_JumpStart : AsEm_JumpBase
		{
			public override string StateName => "AsEm_JumpStart";
			protected AsEm_JumpStart(EnemyContext context) : base(context) { }
		}

		public abstract class AsEm_Jump : AsEm_JumpBase
		{
			public override string StateName => "AsEm_Jump";
			protected AsEm_Jump(EnemyContext context) : base(context) { }
		}
	}
}
