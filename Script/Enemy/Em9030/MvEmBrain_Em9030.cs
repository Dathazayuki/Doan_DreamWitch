using UnityEngine;

namespace Mv
{
    public class MvEmBrain_Em9030 : MvEmBrain
    {
        public enum eDesire : byte
        {
            Run = 0,
            Jump = 1,
            Atk = 2,
            SlashVH = 3,
            Provoke = 4,
            ShotA = 5,
            Scythe = 6,
            EmSpawn = 7,
            Wait = 8
        }

        private MvEm9030 _Em9030;
        private bool _ReachedHp50;
        private bool _ReachedHp30;
        private bool _ReachedHp10;

        private int[] Combo_Wait;
        private int[] Combo_JmpAtk;
        private int[] Combo_SpritShot;
        private int[] Combo_JmpShot3;
        private int[] Combo_FlySwordBig;
        private int[] Combo_RunSlash;
        private int[] Combo_ShotAir_SpritShot;
        private int[] Combo_ShotSide_Laser;
        private int[] Combo_ShotSide_Eruption;
        private int[] Combo_Scythe;
        private int[] Combo_EmSpawn;
        private int[] Combo_Provoke;
        private int[] Combo_Counter;
        private int[] Combo_KnockOut;

        public override void Setup(MvEnemyBase em)
        {
            base.Setup(em);
            _Em9030 = em as MvEm9030;
            _ReachedHp50 = false;
            _ReachedHp30 = false;
            _ReachedHp10 = false;
        }

        public override void TrgHp_50Per()
        {
            _ReachedHp50 = true;
        }

        public override void TrgHp_30Per()
        {
            _ReachedHp30 = true;
        }

        public override void TrgHp_10Per()
        {
            _ReachedHp10 = true;
        }

        protected override void DoReqAct(int asId)
        {
            if (_Em9030 != null)
                _Em9030.ChangeEnemyState((byte)asId);
        }

        public override void InitDesire()
        {
            if (_DesireWeight == null)
                _DesireWeight = new System.Collections.Generic.Dictionary<byte, int>();

            _DesireWeight.Clear();
            _DesireWeight[(byte)eDesire.Run] = 0;
            _DesireWeight[(byte)eDesire.Jump] = 0;
            _DesireWeight[(byte)eDesire.Atk] = 0;
            _DesireWeight[(byte)eDesire.SlashVH] = 0;
            _DesireWeight[(byte)eDesire.Provoke] = 0;
            _DesireWeight[(byte)eDesire.ShotA] = 0;
            _DesireWeight[(byte)eDesire.Scythe] = 0;
            _DesireWeight[(byte)eDesire.EmSpawn] = 0;
            _DesireWeight[(byte)eDesire.Wait] = 0;
        }

        public override void InitCombo()
        {
            byte wait = (byte)MvEm9030.As.Wait;
            byte jump = (byte)MvEm9030.As.Jump;
            byte jumpMini = (byte)MvEm9030.As.JumpMini;
            byte approachMelee = (byte)MvEm9030.As.ApproachMelee;
            byte atk1 = (byte)MvEm9030.As.Atk1;
            byte atk2 = (byte)MvEm9030.As.Atk2;
            byte atk3 = (byte)MvEm9030.As.Atk3;
            byte slashV = (byte)MvEm9030.As.SlashV;
            byte slashH = (byte)MvEm9030.As.SlashH;
            byte provoke = (byte)MvEm9030.As.Provoke;
            byte counter = (byte)MvEm9030.As.Counter;
            byte magicSprit = (byte)MvEm9030.As.MagicShotSpritShot;
            byte magicSword = (byte)MvEm9030.As.MagicShotFlySwordBig;
            byte airSprit = (byte)MvEm9030.As.MagicShotAirSpritShot;
            byte sideLaser = (byte)MvEm9030.As.MagicShotSideLaser;
            byte sideEruption = (byte)MvEm9030.As.MagicShotSideEruption;
            byte scythe = (byte)MvEm9030.As.Scythe;
            byte emSpawn = (byte)MvEm9030.As.EmSpawn;
            byte knockOut = (byte)MvEm9030.As.KnockOut;

            Combo_Wait = new int[] { wait };
            Combo_JmpAtk = new int[] { jumpMini, approachMelee, atk1, atk2, atk3 };
            Combo_SpritShot = new int[] { magicSprit };
            Combo_JmpShot3 = new int[] { jump, airSprit, airSprit, airSprit };
            Combo_FlySwordBig = new int[] { magicSword };
            Combo_RunSlash = new int[] { approachMelee, slashV, slashH };
            Combo_ShotAir_SpritShot = new int[] { jump, airSprit };
            Combo_ShotSide_Laser = new int[] { sideLaser };
            Combo_ShotSide_Eruption = new int[] { sideEruption };
            Combo_Scythe = new int[] { scythe };
            Combo_EmSpawn = new int[] { emSpawn };
            Combo_Provoke = new int[] { provoke };
            Combo_Counter = new int[] { approachMelee, counter };
            Combo_KnockOut = new int[] { knockOut };
        }

        protected override void AddDesire()
        {
            if (_Em9030 == null)
                return;

            InitDesire();
            AddDesireWeight((byte)eDesire.Run, _Em9030.Desire_Add_Run);
            AddDesireWeight((byte)eDesire.Jump, _Em9030.Desire_Add_Jump);
            AddDesireWeight((byte)eDesire.Atk, _Em9030.Desire_Add_Atk);
            AddDesireWeight((byte)eDesire.SlashVH, _Em9030.Desire_Add_SlashVH);
            AddDesireWeight((byte)eDesire.Provoke, _Em9030.Desire_Add_Provoke);
            AddDesireWeight((byte)eDesire.ShotA, _Em9030.Desire_Add_ShotA);
            AddDesireWeight((byte)eDesire.Wait, 5);

            if (_ReachedHp50)
                AddDesireWeight((byte)eDesire.Scythe, _Em9030.Desire_Add_Scythe_Hp50);

            if (_ReachedHp30)
                AddDesireWeight((byte)eDesire.EmSpawn, _Em9030.Desire_Add_EmSpawn_Hp30);

            if (_ReachedHp10)
            {
                AddDesireWeight((byte)eDesire.Scythe, _Em9030.Desire_Add_Scythe_Hp50);
                AddDesireWeight((byte)eDesire.ShotA, 10);
            }

            if (_Em9030.CurrentTarget != null)
            {
                float distanceX = Mathf.Abs(_Em9030.CurrentTarget.position.x - _Em9030.transform.position.x);
                if (distanceX > 8f)
                {
                    AddDesireWeight((byte)eDesire.ShotA, 20);
                    AddDesireWeight((byte)eDesire.Jump, 15);
                }
            }
        }

        protected override void FinishComboTable(byte desireType)
        {
            switch ((eDesire)desireType)
            {
                case eDesire.Run:
                    _ComboBuff = Combo_RunSlash;
                    break;
                case eDesire.Jump:
                    _ComboBuff = Random.value < 0.5f ? Combo_JmpAtk : Combo_JmpShot3;
                    break;
                case eDesire.Atk:
                    _ComboBuff = ChooseMeleeCombo();
                    break;
                case eDesire.SlashVH:
                    _ComboBuff = Combo_RunSlash;
                    break;
                case eDesire.Provoke:
                    _ComboBuff = Random.value < 0.2f ? Combo_Counter : Combo_Provoke;
                    break;
                case eDesire.ShotA:
                    _ComboBuff = ChooseShotCombo();
                    break;
                case eDesire.Scythe:
                    _ComboBuff = Combo_Scythe;
                    break;
                case eDesire.EmSpawn:
                    _ComboBuff = Combo_EmSpawn;
                    break;
                case eDesire.Wait:
                default:
                    _ComboBuff = Combo_Wait;
                    break;
            }
        }

        protected override void MoveKnockOutCore()
        {
        }

        public override int GetKnockOutLoopNum()
        {
            if (_Em9030 == null || _Em9030.KnockOut_LoopNum == null || _Em9030.KnockOut_LoopNum.Length == 0)
                return 1;

            int idx = Mathf.Clamp(_KnockOut_Count - 1, 0, _Em9030.KnockOut_LoopNum.Length - 1);
            return Mathf.Max(1, _Em9030.KnockOut_LoopNum[idx]);
        }

        public void TriggerKnockOut()
        {
            ReqKnockOut(Combo_KnockOut);
        }

        public bool IsNextComboActionAirborneAttack()
        {
            if (_ComboBuff == null || _ComboId >= _ComboBuff.Length)
                return false;

            return IsAirborneAttack(_ComboBuff[_ComboId]);
        }

        private static bool IsAirborneAttack(int asId)
        {
            return asId == (byte)MvEm9030.As.MagicShotAirSpritShot;
        }

        private int[] ChooseMeleeCombo()
        {
            float roll = Random.value;
            byte approachMelee = (byte)MvEm9030.As.ApproachMelee;
            if (roll < 0.35f) return new int[] { approachMelee, (byte)MvEm9030.As.Atk1, (byte)MvEm9030.As.Atk2, (byte)MvEm9030.As.Atk3 };
            if (roll < 0.55f) return new int[] { approachMelee, (byte)MvEm9030.As.Atk1, (byte)MvEm9030.As.SlashH };
            if (roll < 0.75f) return new int[] { approachMelee, (byte)MvEm9030.As.SlashV, (byte)MvEm9030.As.SlashH };
            return Combo_Counter;
        }

        private int[] ChooseShotCombo()
        {
            float roll = Random.value;
            if (roll < 0.2f) return Combo_SpritShot;
            if (roll < 0.4f) return Combo_FlySwordBig;
            if (roll < 0.6f) return Combo_ShotAir_SpritShot;
            if (roll < 0.8f) return Combo_ShotSide_Laser;
            return Combo_ShotSide_Eruption;
        }
    }
}
