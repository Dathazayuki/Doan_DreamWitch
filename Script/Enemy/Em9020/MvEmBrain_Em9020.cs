using UnityEngine;

namespace Mv
{
    /// <summary>
    /// Bộ não AI của Boss Golem (Em9020).
    ///
    /// Quản lý:
    ///   - Phân vùng sân đấu (Left / Center / Right) để chọn tay đánh phù hợp.
    ///   - Hệ thống Desire: Atk, AtkSuper, Laser, LaserSuper, Wander, SpawnEm, Step.
    ///   - Danh sách Combo cho từng hoạt động của Golem.
    ///   - Chuyển pha (Phase transition) khi HP giảm sâu.
    /// </summary>
    public partial class MvEmBrain_Em9020 : MvEmBrain
    {
        // ──────────────────────────────────────────────────────────────
        //  Enums
        // ──────────────────────────────────────────────────────────────

        /// <summary>Phân vùng sân đấu dựa trên vị trí Player.</summary>
        public enum eRegion
        {
            L = 0,  // Bên trái
            C = 1,  // Trung tâm
            R = 2   // Bên phải
        }

        /// <summary>Các loại Desire (Mong muốn) mà AI có thể chọn.</summary>
        public enum eDesire : byte
        {
            Atk = 0,        // Đập tay đơn / Quét tay
            AtkSuper = 1,   // Đập cả hai tay
            Laser = 2,      // Quét laser thường
            LaserSuper = 3, // Siêu laser quét ngang
            Wander = 4,     // Chờ đợi / Idle
            SpawnEm = 5,    // Triệu hồi quái con
            Step = 6        // Dịch chuyển thân hình
        }

        // ──────────────────────────────────────────────────────────────
        //  Reference
        // ──────────────────────────────────────────────────────────────

        private MvEm9020 _Em9020;

        // ──────────────────────────────────────────────────────────────
        //  Config (đọc từ MvEm9020 Inspector fields)
        // ──────────────────────────────────────────────────────────────

        private const int Region_HalfSizeX = 7;

        //  Phase flags
        // ──────────────────────────────────────────────────────────────

        private bool _ReachedHp50;
        private bool _ReachedHp30;
        private bool _ReachedHp10;

        // ──────────────────────────────────────────────────────────────
        //  Setup
        // ──────────────────────────────────────────────────────────────

        public override void Setup(MvEnemyBase em)
        {
            base.Setup(em);
            _Em9020 = em as MvEm9020;
            _ReachedHp50 = false;
            _ReachedHp30 = false;
            _ReachedHp10 = false;
        }

        // ──────────────────────────────────────────────────────────────
        //  HP Trigger Callbacks
        // ──────────────────────────────────────────────────────────────

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

        // ──────────────────────────────────────────────────────────────
        //  Abstract Implementations
        // ──────────────────────────────────────────────────────────────

        protected override void DoReqAct(int asId)
        {
            if (_Em9020 != null)
                _Em9020.ChangeEnemyState((byte)asId);
        }

        public override void InitDesire()
        {
            if (_DesireWeight == null)
                _DesireWeight = new System.Collections.Generic.Dictionary<byte, int>();

            _DesireWeight.Clear();

            // Trọng số mặc định ban đầu
            _DesireWeight[(byte)eDesire.Atk] = 0;
            _DesireWeight[(byte)eDesire.AtkSuper] = 0;
            _DesireWeight[(byte)eDesire.Laser] = 0;
            _DesireWeight[(byte)eDesire.LaserSuper] = 0;
            _DesireWeight[(byte)eDesire.Wander] = 0;
            _DesireWeight[(byte)eDesire.SpawnEm] = 0;
            _DesireWeight[(byte)eDesire.Step] = 0;
        }

        protected override void MoveKnockOutCore()
        {
            // Logic bổ sung khi Boss đang bị KnockOut (nếu cần)
        }

        public override int GetKnockOutLoopNum()
        {
            // Số loop KnockOut tăng dần theo số lần bị knock out
            // Lần đầu: 3 loop, lần 2: 2 loop, sau đó: 1 loop
            if (_KnockOut_Count <= 1) return 3;
            if (_KnockOut_Count <= 2) return 2;
            return 1;
        }

        /// <summary>
        /// Kích hoạt trạng thái KnockOut cho Boss.
        /// Gọi bởi MvEm9020 khi thanh giáp cạn kiệt.
        /// </summary>
        public void TriggerKnockOut()
        {
            ReqKnockOut(Combo_KnockOut);
        }

        /// <summary>
        /// Chọn vị trí xuất hiện lại sau khi Boss lặn sâu (HideEnd).
        /// </summary>
        public Vector3 ChoiceHideEndPos()
        {
            if (_Em9020 == null) return Vector3.zero;

            // Chọn ngẫu nhiên vị trí xuất hiện lại ở trung tâm sân đấu
            float offsetX = Random.Range(-Region_HalfSizeX * 0.5f, Region_HalfSizeX * 0.5f);
            Vector3 basePos = _Em9020.transform.position;
            return new Vector3(basePos.x + offsetX, basePos.y, basePos.z);
        }
    }
}
