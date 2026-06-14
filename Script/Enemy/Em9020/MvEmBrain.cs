using System.Collections.Generic;
using UnityEngine;

namespace Mv
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cho AI Brain của các Boss.
    /// Quản lý hệ thống Desire (Mong muốn) và Combo (Chuỗi hành động).
    ///
    /// Vòng lặp quyết định:
    ///   1. Boss hoàn thành combo hiện tại → gọi reqCombo().
    ///   2. addDesire() cập nhật trọng số dựa trên tình huống (HP, vị trí Player).
    ///   3. Chọn ngẫu nhiên một DesireType dựa trên phân phối trọng số.
    ///   4. finishComboTable(desireType) nạp chuỗi State IDs vào _ComboBuff.
    ///   5. Boss lần lượt thực hiện từng State trong combo qua doReqAct(asId).
    /// </summary>
    public abstract class MvEmBrain
    {
        // ──────────────────────────────────────────────────────────────
        //  Combo Queue
        // ──────────────────────────────────────────────────────────────

        /// <summary>Chỉ mục hiện tại trong chuỗi combo đang thực thi.</summary>
        protected int _ComboId;

        /// <summary>Mảng chứa chuỗi State IDs tạo thành combo hiện tại.</summary>
        protected int[] _ComboBuff;

        // ──────────────────────────────────────────────────────────────
        //  Desire Weights
        // ──────────────────────────────────────────────────────────────

        /// <summary>Loại Desire đang được chọn (byte cast từ enum cụ thể của subclass).</summary>
        protected byte _DesireType;

        /// <summary>
        /// Bảng trọng số mong muốn: Key = DesireType (byte), Value = trọng số (int).
        /// Trọng số càng cao thì xác suất chọn Desire đó càng lớn.
        /// </summary>
        protected Dictionary<byte, int> _DesireWeight;

        // ──────────────────────────────────────────────────────────────
        //  KnockOut
        // ──────────────────────────────────────────────────────────────

        /// <summary>Số lần Boss đã bị knock out trong trận đấu.</summary>
        protected int _KnockOut_Count;

        /// <summary>
        /// Cờ tạm bỏ qua KnockOut (ví dụ: trong lúc đang thực hiện SuperLaser
        /// thì không bị gục ngay).
        /// </summary>
        public bool IgnoreKnockOut { get; set; }

        // ──────────────────────────────────────────────────────────────
        //  Reference
        // ──────────────────────────────────────────────────────────────

        /// <summary>Tham chiếu ngược lại thực thể Boss (MvEnemyBase).</summary>
        protected MvEnemyBase _Em;

        // ──────────────────────────────────────────────────────────────
        //  Setup
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Khởi tạo Brain — gọi một lần khi Boss được spawn.
        /// Subclass override để lưu tham chiếu cụ thể (ép kiểu).
        /// </summary>
        public virtual void Setup(MvEnemyBase em)
        {
            _Em = em;
            _ComboId = 0;
            _ComboBuff = null;
            _KnockOut_Count = 0;
            IgnoreKnockOut = false;

            _DesireWeight = new Dictionary<byte, int>();

            InitDesire();
            InitCombo();
        }

        // ──────────────────────────────────────────────────────────────
        //  HP Trigger Callbacks
        // ──────────────────────────────────────────────────────────────

        /// <summary>Gọi khi HP Boss giảm xuống dưới 65%.</summary>
        public virtual void TrgHp_65Per() { }

        /// <summary>Gọi khi HP Boss giảm xuống dưới 50%.</summary>
        public virtual void TrgHp_50Per() { }

        /// <summary>Gọi khi HP Boss giảm xuống dưới 30%.</summary>
        public virtual void TrgHp_30Per() { }

        /// <summary>Gọi khi HP Boss giảm xuống dưới 10%.</summary>
        public virtual void TrgHp_10Per() { }

        // ──────────────────────────────────────────────────────────────
        //  Abstract — Subclass PHẢI override
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Yêu cầu Boss chuyển sang State có ID = asId.
        /// Subclass thực hiện bằng cách gọi Em.ChangeEnemyState((byte)asId).
        /// </summary>
        protected abstract void DoReqAct(int asId);

        /// <summary>Khởi tạo bảng trọng số mong muốn ban đầu.</summary>
        public abstract void InitDesire();

        /// <summary>Khởi tạo/khai báo các mảng Combo.</summary>
        public abstract void InitCombo();

        /// <summary>
        /// Tính toán lại trọng số mong muốn dựa trên tình huống hiện tại
        /// (HP, vị trí Player, vùng sân đấu, v.v.).
        /// </summary>
        protected abstract void AddDesire();

        /// <summary>
        /// Sau khi chọn được desireType, nạp chuỗi combo phù hợp vào _ComboBuff.
        /// </summary>
        protected abstract void FinishComboTable(byte desireType);

        /// <summary>Logic xử lý khi Boss đang trong trạng thái KnockOut.</summary>
        protected abstract void MoveKnockOutCore();

        /// <summary>Trả về số lần loop animation KnockOut ở lượt knock out hiện tại.</summary>
        public abstract int GetKnockOutLoopNum();

        // ──────────────────────────────────────────────────────────────
        //  Combo Execution
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Yêu cầu Brain chọn và bắt đầu thực thi combo tiếp theo.
        /// Gọi bởi Boss khi Boss rảnh (Idle sau khi hoàn thành combo cũ).
        /// </summary>
        public void ReqCombo()
        {
            // Nếu đang giữa một combo → thực hiện action tiếp theo
            if (_ComboBuff != null && _ComboId < _ComboBuff.Length)
            {
                int asId = _ComboBuff[_ComboId];
                _ComboId++;
                DoReqAct(asId);
                return;
            }

            // Combo cũ đã hết → chọn combo mới
            ChangeNextComboTable();
        }

        /// <summary>
        /// Yêu cầu Boss chuyển sang chuỗi KnockOut.
        /// </summary>
        protected void ReqKnockOut(int[] combo_KnockOutBuff)
        {
            _KnockOut_Count++;
            _ComboBuff = combo_KnockOutBuff;
            _ComboId = 0;

            if (_ComboBuff != null && _ComboBuff.Length > 0)
            {
                DoReqAct(_ComboBuff[0]);
                _ComboId = 1;
            }
        }

        /// <summary>
        /// Chọn combo mới dựa trên hệ thống Desire.
        /// </summary>
        private void ChangeNextComboTable()
        {
            // Bước 1: Tính toán lại trọng số
            AddDesire();

            // Bước 2: Chọn ngẫu nhiên theo trọng số
            SwitchComboTable();

            // Bước 3: Nạp combo tương ứng
            FinishComboTable(_DesireType);

            // Bước 4: Bắt đầu thực thi combo mới
            _ComboId = 0;
            if (_ComboBuff != null && _ComboBuff.Length > 0)
            {
                DoReqAct(_ComboBuff[0]);
                _ComboId = 1;
            }
        }

        /// <summary>
        /// Chọn ngẫu nhiên một DesireType dựa trên phân phối trọng số.
        /// </summary>
        protected virtual void SwitchComboTable()
        {
            if (_DesireWeight == null || _DesireWeight.Count == 0)
                return;

            int totalWeight = 0;
            foreach (var kvp in _DesireWeight)
            {
                if (kvp.Value > 0)
                    totalWeight += kvp.Value;
            }

            if (totalWeight <= 0)
                return;

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;

            foreach (var kvp in _DesireWeight)
            {
                if (kvp.Value <= 0) continue;

                cumulative += kvp.Value;
                if (roll < cumulative)
                {
                    _DesireType = kvp.Key;
                    return;
                }
            }
        }

        /// <summary>
        /// Tick logic KnockOut — gọi bởi Boss mỗi frame khi đang ở trạng thái KnockOut.
        /// </summary>
        public void MoveKnockOut()
        {
            MoveKnockOutCore();
        }

        // ──────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────

        /// <summary>Đặt trọng số cho một Desire cụ thể.</summary>
        protected void SetDesireWeight(byte desireType, int weight)
        {
            _DesireWeight[desireType] = Mathf.Max(0, weight);
        }

        /// <summary>Cộng thêm trọng số cho một Desire cụ thể.</summary>
        protected void AddDesireWeight(byte desireType, int addWeight)
        {
            if (!_DesireWeight.ContainsKey(desireType))
                _DesireWeight[desireType] = 0;

            _DesireWeight[desireType] = Mathf.Max(0, _DesireWeight[desireType] + addWeight);
        }

        /// <summary>Kiểm tra xem combo hiện tại đã hoàn thành chưa.</summary>
        public bool IsComboFinished => _ComboBuff == null || _ComboId >= _ComboBuff.Length;
    }
}
