 Kiến Trúc Hệ Thống Enemy
Sơ Đồ Toàn Bộ Lớp Kế Thừa
MvUnit  (MonoBehaviour gốc)
  └── MvUnitAct  (physics, animator, flip, cond)
        └── MvEm  (base tất cả Enemy + hệ thống detect + state chung)
              ├── MvEm các class thường (Em0010~Em0470)
              │     └── MvEmLs  (subtype: Livestock/Động vật thuần hoá)
              │           ├── MvEm0010 (BoneDog)
              │           ├── MvEm0060 (ShieldKnight)
              │           └── MvEm5000~5040 (Frog, Chicken, Pig...)
              │
              └── MvEm các Boss (Em9000~Em9070)
                    ├── MvEm9000 (Dragon Boss)
                    ├── MvEm9020 (Golem Boss)
                    ├── MvEm9030 (Marionette Boss)
                    └── ...
3 Lớp Cấu Trúc Cốt Lõi
🔴 Lớp 1: 

MvEm.cs
 — Base Class Dùng Chung Cho TẤT CẢ Enemy
Đây là file quan trọng nhất — định nghĩa toàn bộ state class dùng chung mà mọi Enemy đều tái sử dụng (nested bên trong nó):

State Class (trong MvEm)	Dùng chung cho

AsEm_Idle_Base
Mọi enemy đứng yên

AsEm_Idle
Idle + timer chờ → scan

AsEm_IdleSearch
Idle + đang tích cực tìm Player

AsEm_IdleOnly
Idle thuần (không scan)

AsEm_Common_AtkAfter
Sau khi tấn công, nghỉ ngơi

AsEm_Common_Turn
Xoay hướng

AsEm_Run_Base
 / 

AsEm_Run
Chạy tuần tra / chạy có detect

AsEm_FlyChase
Bay đuổi Player (em bay)

AsEm_JumpStart
 / 

AsEm_Jump
Nhảy

AsEm_Atk_Base
 / 

AsEm_AtkWithSign_Base
Tấn công (có/không có telegraph)

AsEm_ShotBase
 / 

AsEm_SimpleShot
 / 

AsEm_TrgShot
Các kiểu bắn đạn

AsEm_Common_Hit
Trúng đòn thường

AsEm_Common_HitKnockBack
Bị đánh bật lui

AsEm_Common_HitKnockUp
Bị đánh tung lên

AsEm_Common_HitNeedle
Trúng gai
AsEm_Common_Frozen/Stone/Stun	Các status freeze

AsEm_Common_DeathPre
Chuẩn bị chết (animation + delay)

AsEm_Common_Death
Chết

AsEm_Common_DeathMelt
Tan biến sau chết

AsEm_BossDeathBase
Chết Boss (explosion, cinematics)

AsEm_Common_Cage
Bị nhốt trong cage

AsEm_Common_Inactive
Ẩn đi

AsEmBoss_KnockOut
Boss bị knock out (stagger)

AsEm_Eat
Ăn (Livestock)
State ID chung được khai báo trong MvEm.AsCommon:

csharp
public enum AsCommon : byte {
    Idle=0, Run=1, Turn=2, AtkAfter=3,
    Hit=4, HitKnockBack=5, HitKnockUp=6, HitBounce=7, HitNeedle=8,
    CondFrozen=9, CondStone=10, CondStun=11,
    DeathPotDive=12, DeathPre=13, Cage=14, Death=15, Inactive=16
}
🟡 Lớp 2: Mỗi MvEmXXXX.cs — Logic Riêng + State Riêng
Mỗi Enemy là một class riêng kế thừa 

MvEm
 (hoặc 

MvEmLs
), chứa:

Enum 

As
 — State IDs riêng bắt đầu từ 

17
 (sau các state chung 0–16):
csharp
// MvEm0010 (BoneDog)
public enum As : byte { AtkCharge=17, Atk=18, Chase=19, Jump=20, DeathMelt=21 }
// MvEm0040 (ShieldGuard)
public enum As : byte { Atk=17, Guard=18 }
// MvEm0080 (MonoEye)
public enum As : byte { Atk=17, Shot=18, Heal=19 }
// MvEm0120 (FlyingMob)
public enum As : byte { TurnWait=17 }
// MvEm9000 (Dragon)
public enum As : byte { Entrance=18, StepF=19, Breath=21, ..., Max=47 }
Nested State Classes — kế thừa từ state chung trong 

MvEm
:
csharp
// Extend state chung:
MvEm0010.AsEm0010_Chase  : AsEm_Run_Base         ← run đuổi
MvEm0010.AsEm0010_Atk    : AsEm_AtkWithSign_Base  ← tấn công có telegraph
MvEm0010.AsEm0010_Jump   : AsEm_Jump              ← nhảy
MvEm0040.AsEm0040_Run    : AsEm_Run_Base
MvEm0040.AsEm0040_Atk    : AsEm_AtkWithSign_Base
MvEm0040.AsEm0040_Guard  : MvActState_Em          ← state hoàn toàn riêng
MvEm0080.AsEm0080_Shot   : AsEm_SimpleShot        ← extend bắn đạn
MvEm0080.AsEm0080_Heal   : MvActState_Em          ← heal friendly hoàn toàn riêng
MvEm0120.AsEm0120_FlyChase : AsEm_FlyChase        ← extend fly
// Boss thêm thêm Base Layer riêng:
MvEm9000.MvActState_Em9000     : MvActState_Em    ← base class boss riêng
MvEm9000.AsEm9000_StepBase     : MvActState_Em9000 ← abstract dùng cho StepF/StepB
MvEm9000.AsEm9000_FireBall_Base : MvActState_Em9000 ← abstract dùng cho FireBall_Line1~4
MvEm9000.AsEm9000_AtkHeadBase  : MvActState_Em9000 ← abstract dùng cho AtkHeadB/C/F
MvEm9000.AsEm9000_MagicBase    : MvActState_Em9000 ← abstract dùng cho Tornado/Bomb
Các method phải override:
csharp
// Bắt buộc override trong từng Enemy:
abstract eEmType EmType             // Loại enemy
abstract string getAsEnumName_EmUnique(int actId)  // Dev debug
abstract void makeAsTable()         // Đăng ký state table
abstract void moveSearchPl()        // Logic detect player riêng
Config data từ ScriptableObject riêng (MvSo_EmXXXX):
csharp
// Cấu trúc hierarchy SO:
MvSo_Em (container tổng)
  ├── MvSo_Em0010 : MvSo_EmBase   // MaxHp, AtkDmg, RunSpeed, IdleInterval...
  ├── MvSo_Em0040 : MvSo_EmBase
  └── MvSo_Em9000 : MvSo_EmBase   // Boss SO
// MvSo_EmBase chứa:
MaxHp, MaxShield, AtkDmg, AtkAlways_Dmg
IdleInterval, RunInterval, AtkAfterInterval, TurnInterval
RunSpeed, RunSpeedCurve
DropItemType, DropItemRareRate
CageSoulPrice, CanHaunt
🔵 Lớp 3: MvEmBrain_Em9XXX.cs — AI Brain (chỉ dành cho Boss)
Chỉ Boss mới có Brain riêng. Brain quản lý combo pattern và phase transitions:

MvEmBrain  (abstract base)
  ├── MvEmBrain_Em9000  (Dragon)
  ├── MvEmBrain_Em9010  (BigFlower Hang)
  ├── MvEmBrain_Em9011  (BigFlower Main)
  ├── MvEmBrain_Em9012  (BigFlower Fly)
  ├── MvEmBrain_Em9020  (Golem)
  ├── MvEmBrain_Em9030  (Marionette)
  ├── MvEmBrain_Em9040  (OldWitch)
  ├── MvEmBrain_Em9050  (FinalWitch)
  ├── MvEmBrain_Em9052  (CauldronMonster)
  ├── MvEmBrain_Em9060  (TripleHand)
  └── MvEmBrain_Em9070  (Shopkeeper)
Brain có hệ thống Desire + Combo:

csharp
// Trong MvEmBrain:
Dictionary<byte, int> _DesireWeight   // xác suất chọn hành động
int[] _ComboBuff                       // chuỗi action IDs hiện tại
// Dragon Brain ví dụ:
int[] Combo_StepF1, Combo_StepF2  // combo bước đến
int[] Combo_Breath                 // nhả lửa
int[] Combo_Tackle                 // húc
int[] Combo_FireBall_A/B/C/D      // 4 pattern cầu lửa
int[] Combo_MagmaTornado          // xoáy magma
int[] Combo_KnockOut              // knock out transition