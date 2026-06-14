using UnityEngine;

namespace Mv
{
    public partial class MvEmBrain_Em9020
    {
        // ──────────────────────────────────────────────────────────────
        //  Combo Arrays
        // ──────────────────────────────────────────────────────────────

        // Đập tay đơn
        private int[] Combo_HandR_AtkD;
        private int[] Combo_HandL_AtkD;

        // Quét tay đơn
        private int[] Combo_HandR_Slap;
        private int[] Combo_HandL_Slap;

        // Combo đập tay + quét tay
        private int[] Combo_HandR_AtkD_Slap;
        private int[] Combo_HandL_AtkD_Slap;
        private int[] Combo_HandR_Slap_AtkD;
        private int[] Combo_HandL_Slap_AtkD;

        // Dịch chuyển
        private int[] Combo_StepL;
        private int[] Combo_StepR;
        private int[] Combo_StepRR;
        private int[] Combo_StepLL;
        private int[] Combo_StepLR;
        private int[] Combo_StepRL;

        // Đập cả hai tay
        private int[] Combo_HandLR;

        // Triệu hồi quái con
        private int[] Combo_SpawnEm;

        // Đập hai tay + Quét tay (Super Attack Combo)
        private int[] Combo_HandLR_SlapR;
        private int[] Combo_HandLR_SlapL;

        // Laser
        private int[] Combo_LaserCW;
        private int[] Combo_LaserCCW;

        // Siêu laser
        private int[] Combo_LaserSuperR;
        private int[] Combo_LaserSuperL;

        // KnockOut
        private int[] Combo_KnockOut;

        // ──────────────────────────────────────────────────────────────

        public override void InitCombo()
        {
            byte handL_AtkD = (byte)MvEm9020.As.HandL_AtkD;
            byte handR_AtkD = (byte)MvEm9020.As.HandR_AtkD;
            byte handL_Slap = (byte)MvEm9020.As.HandL_Slap;
            byte handR_Slap = (byte)MvEm9020.As.HandR_Slap;
            byte handLR_AtkD = (byte)MvEm9020.As.HandLR_AtkD;
            byte handLR_SpawnEm = (byte)MvEm9020.As.HandLR_AtkD_SpawnEm;
            byte laserCW = (byte)MvEm9020.As.LaserCW;
            byte laserCCW = (byte)MvEm9020.As.LaserCCW;
            byte laserSuperR = (byte)MvEm9020.As.LaserSuperR;
            byte laserSuperL = (byte)MvEm9020.As.LaserSuperL;
            byte knockOut = (byte)MvEm9020.As.KnockOut;
            byte stepL = (byte)MvEm9020.As.StepL;
            byte stepR = (byte)MvEm9020.As.StepR;

            // Đập tay đơn
            Combo_HandR_AtkD = new int[] { handR_AtkD };
            Combo_HandL_AtkD = new int[] { handL_AtkD };

            // Quét tay đơn
            Combo_HandR_Slap = new int[] { handR_Slap };
            Combo_HandL_Slap = new int[] { handL_Slap };

            // Combo đập tay → quét tay
            Combo_HandR_AtkD_Slap = new int[] { handR_AtkD, handR_Slap };
            Combo_HandL_AtkD_Slap = new int[] { handL_AtkD, handL_Slap };

            // Combo quét tay → đập tay
            Combo_HandR_Slap_AtkD = new int[] { handR_Slap, handR_AtkD };
            Combo_HandL_Slap_AtkD = new int[] { handL_Slap, handL_AtkD };

            // Dịch chuyển
            Combo_StepL = new int[] { stepL };
            Combo_StepR = new int[] { stepR };
            Combo_StepRR = new int[] { stepR, stepR };
            Combo_StepLL = new int[] { stepL, stepL };
            Combo_StepLR = new int[] { stepL, stepR };
            Combo_StepRL = new int[] { stepR, stepL };

            // Đập cả hai tay
            Combo_HandLR = new int[] { handLR_AtkD };

            // Triệu hồi quái con
            Combo_SpawnEm = new int[] { handLR_SpawnEm };

            // Đập hai tay + Quét tay
            Combo_HandLR_SlapR = new int[] { handLR_AtkD, handR_Slap };
            Combo_HandLR_SlapL = new int[] { handLR_AtkD, handL_Slap };

            // Laser
            Combo_LaserCW = new int[] { laserCW };
            Combo_LaserCCW = new int[] { laserCCW };

            // Siêu laser
            Combo_LaserSuperR = new int[] { laserSuperR };
            Combo_LaserSuperL = new int[] { laserSuperL };

            // KnockOut
            Combo_KnockOut = new int[] { knockOut };
        }

        // ──────────────────────────────────────────────────────────────
        //  Region Detection
        // ──────────────────────────────────────────────────────────────

        /// <summary>Kiểm tra vùng sân đấu mà Player đang đứng.</summary>
        public eRegion CheckRegionTgtPl()
        {
            if (_Em9020 == null || _Em9020.CurrentTarget == null)
                return eRegion.C;

            return CheckRegion(_Em9020.CurrentTarget.position.x);
        }

        /// <summary>Kiểm tra vùng sân đấu mà Boss đang đứng.</summary>
        public eRegion CheckRegion_Em9020()
        {
            if (_Em9020 == null)
                return eRegion.C;

            return CheckRegion(_Em9020.transform.position.x);
        }

        /// <summary>Trả về vùng sân đấu dựa trên tọa độ X.</summary>
        public eRegion CheckRegion(float posX)
        {
            if (_Em9020 == null)
                return eRegion.C;

            float centerX = _Em9020.SpawnPos.x;
            float delta = posX - centerX;

            if (delta < -Region_HalfSizeX)
                return eRegion.L;
            if (delta > Region_HalfSizeX)
                return eRegion.R;

            return eRegion.C;
        }

        // ──────────────────────────────────────────────────────────────
        //  Desire Calculation
        // ──────────────────────────────────────────────────────────────
        // ──────────────────────────────────────────────────────────────
        //  Combo Selection
        // ──────────────────────────────────────────────────────────────

        protected override void FinishComboTable(byte desireType)
        {
            eDesire desire = (eDesire)desireType;
            eRegion playerRegion = CheckRegionTgtPl();

            switch (desire)
            {
                case eDesire.Atk:
                    FinishCombo_Atk(playerRegion);
                    break;

                case eDesire.AtkSuper:
                    FinishCombo_AtkSuper(playerRegion);
                    break;

                case eDesire.Laser:
                    FinishCombo_Laser();
                    break;

                case eDesire.LaserSuper:
                    FinishCombo_LaserSuper(playerRegion);
                    break;

                case eDesire.Wander:
                    // Idle — không nạp combo, Boss sẽ đứng im rồi tự gọi ReqCombo lại
                    _ComboBuff = null;
                    break;

                case eDesire.SpawnEm:
                    _ComboBuff = Combo_SpawnEm;
                    break;

                case eDesire.Step:
                    FinishCombo_Step(playerRegion);
                    break;

                default:
                    _ComboBuff = null;
                    break;
            }
        }

        private void FinishCombo_Atk(eRegion playerRegion)
        {
            // Chọn combo dựa trên vùng Player đang đứng
            float roll = Random.value;

            if (playerRegion == eRegion.L)
            {
                // Player bên trái màn hình -> dùng tay phải của boss.
                if (roll < 0.3f)
                    _ComboBuff = Combo_HandR_AtkD;
                else if (roll < 0.5f)
                    _ComboBuff = Combo_HandR_Slap;
                else if (roll < 0.75f)
                    _ComboBuff = Combo_HandR_AtkD_Slap;
                else
                    _ComboBuff = Combo_HandR_Slap_AtkD;
            }
            else if (playerRegion == eRegion.R)
            {
                // Player bên phải màn hình -> dùng tay trái của boss.
                if (roll < 0.3f)
                    _ComboBuff = Combo_HandL_AtkD;
                else if (roll < 0.5f)
                    _ComboBuff = Combo_HandL_Slap;
                else if (roll < 0.75f)
                    _ComboBuff = Combo_HandL_AtkD_Slap;
                else
                    _ComboBuff = Combo_HandL_Slap_AtkD;
            }
            else
            {
                // Player ở trung tâm → chọn ngẫu nhiên tay
                if (roll < 0.5f)
                    _ComboBuff = Combo_HandR_AtkD_Slap;
                else
                    _ComboBuff = Combo_HandL_AtkD_Slap;
            }
        }

        private void FinishCombo_AtkSuper(eRegion playerRegion)
        {
            float roll = Random.value;

            if (playerRegion == eRegion.L)
            {
                _ComboBuff = roll < 0.5f ? Combo_HandLR : Combo_HandLR_SlapR;
            }
            else if (playerRegion == eRegion.R)
            {
                _ComboBuff = roll < 0.5f ? Combo_HandLR : Combo_HandLR_SlapL;
            }
            else
            {
                _ComboBuff = Combo_HandLR;
            }
        }

        private void FinishCombo_Laser()
        {
            // Chọn ngẫu nhiên hướng quét laser
            _ComboBuff = Random.value < 0.5f ? Combo_LaserCW : Combo_LaserCCW;
        }

        private void FinishCombo_LaserSuper(eRegion playerRegion)
        {
            // Quét siêu laser theo hướng Player
            if (playerRegion == eRegion.L)
                _ComboBuff = Combo_LaserSuperL;
            else
                _ComboBuff = Combo_LaserSuperR;
        }

        private void FinishCombo_Step(eRegion playerRegion)
        {
            eRegion emRegion = CheckRegion_Em9020();
            float roll = Random.value;

            // Nếu Boss ở rìa → di chuyển về trung tâm
            if (emRegion == eRegion.L)
            {
                _ComboBuff = roll < 0.5f ? Combo_StepR : Combo_StepRR;
            }
            else if (emRegion == eRegion.R)
            {
                _ComboBuff = roll < 0.5f ? Combo_StepL : Combo_StepLL;
            }
            else
            {
                // Boss ở trung tâm → di chuyển về hướng Player
                if (playerRegion == eRegion.L)
                {
                    _ComboBuff = roll < 0.7f ? Combo_StepL : Combo_StepLR;
                }
                else if (playerRegion == eRegion.R)
                {
                    _ComboBuff = roll < 0.7f ? Combo_StepR : Combo_StepRL;
                }
                else
                {
                    if (roll < 0.25f) _ComboBuff = Combo_StepL;
                    else if (roll < 0.5f) _ComboBuff = Combo_StepR;
                    else if (roll < 0.75f) _ComboBuff = Combo_StepLR;
                    else _ComboBuff = Combo_StepRL;
                }
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  KnockOut
        // ──────────────────────────────────────────────────────────────

    }
}
