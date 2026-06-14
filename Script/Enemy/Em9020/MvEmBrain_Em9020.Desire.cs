using UnityEngine;

namespace Mv
{
    public partial class MvEmBrain_Em9020
    {
        protected override void AddDesire()
        {
            if (_Em9020 == null) return;

            // Reset trọng số
            InitDesire();

            // Cộng thêm trọng số dựa trên cấu hình Inspector
            AddDesireWeight((byte)eDesire.Atk, _Em9020.Desire_Add_Atk);
            AddDesireWeight((byte)eDesire.AtkSuper, _Em9020.Desire_Add_AtkSuper);
            AddDesireWeight((byte)eDesire.Laser, _Em9020.Desire_Add_Laser);
            AddDesireWeight((byte)eDesire.LaserSuper, _Em9020.Desire_Add_LaserSuper);
            AddDesireWeight((byte)eDesire.Wander, _Em9020.Desire_Add_Wander);
            AddDesireWeight((byte)eDesire.Step, _Em9020.Desire_Add_Step);

            // Điều chỉnh theo mốc HP
            if (_ReachedHp50)
            {
                AddDesireWeight((byte)eDesire.LaserSuper, _Em9020.Desire_Add_LaserSuper_Hp50Per);
            }

            if (_ReachedHp10)
            {
                AddDesireWeight((byte)eDesire.LaserSuper, _Em9020.Desire_Add_LaserSuper_Hp10Per);
            }

            // Kích hoạt SpawnEm khi đủ điều kiện HP
            if (_ReachedHp30)
            {
                AddDesireWeight((byte)eDesire.SpawnEm, _Em9020.Desire_Add_SpawnEm_HpTh);
            }

            // --- KIỂM TRA KHOẢNG CÁCH DI CHUYỂN ---
            if (_Em9020.CurrentTarget != null)
            {
                float distanceX = Mathf.Abs(_Em9020.transform.position.x - _Em9020.CurrentTarget.position.x);
                if (distanceX > 10f)
                {
                    // Tắt đòn cận chiến, tăng mạnh tỉ lệ di chuyển (Step) và bắn Laser
                    SetDesireWeight((byte)eDesire.Atk, 0);
                    SetDesireWeight((byte)eDesire.AtkSuper, 0);
                    AddDesireWeight((byte)eDesire.Step, 60);
                    AddDesireWeight((byte)eDesire.Laser, 30);
                }
            }
        }
    }
}
