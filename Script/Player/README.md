# 🎮 Player Controller System - Dream Knight

## 📋 Tổng quan
Hệ thống Player Controller hoàn chỉnh cho game Metroidvania sử dụng **State Machine Pattern**.

### ✨ Features đã hoàn thiện:
- ✅ **Basic Movement**: Walk, Run với smooth acceleration
- ✅ **Jump System**: Single jump, double jump, coyote time, jump buffering
- ✅ **Dash**: Dash theo 8 hướng với cooldown và stamina cost
- ✅ **Wall Climb**: Bám tường, trèo lên/xuống, wall jump (YÊU CẦU GIỮ A/D!)
- ✅ **State Machine**: Clean code với Idle, Move, Jump, Dash, WallClimb states
- ✅ ** Buffering**: Jump và Dash buffering cho responsive gameplay
- ✅ **Stats System**: HP, Stamina với regeneration

---

## 📦 Cấu trúc Files

```
Player/
├─ PlayerController.cs       # Main controller, orchestrates everything
├─ Player.cs            #  handling với buffering
├─ PlayerMovement.cs         # Movement/Physics logic (WALL CLIMB ĐÚNG!)
├─ PlayerStats.cs            # HP, Stamina, speeds
├─ PlayerStateMachine.cs     # State machine manager
├─ States/
│  ├─ PlayerState.cs        # Base state class
│  ├─ IdleState.cs          # Standing still
│  ├─ MoveState.cs          # Walking/Running
│  ├─ JumpState.cs          # Jump/Fall/Air control
│  ├─ DashState.cs          # Dashing
│  └─ WallClimbState.cs     # Wall climb (BẮT BUỘC GIỮ A/D!)
├─ README.md                 # This file
├─ ANIMATION_GUIDE.md        # Full animation setup guide
├─ ANIMATION_QUICK_SETUP.md  # Quick animation reference
└─ WALL_CLIMB_GUIDE.md       # Wall climb detailed guide
```

---

## 🚀 Quick Setup (5 phút)

### 1. Tạo Player GameObject
```
Player
├─ Rigidbody2D (Gravity Scale: 3, Constraints: Freeze Rotation Z)
├─ Collider2D (Capsule hoặc Box)
├─ PlayerController
├─ Player
├─ PlayerMovement
├─ PlayerStats
├─ Animator (optional)
├─ GroundCheck (Empty child, Position Y: -0.5)
└─ WallCheck (Empty child, Position X: 0.5)
```

### 2. Setup Components trong Inspector

#### PlayerMovement:
```
Ground Check: Gán GroundCheck GameObject
Ground Check Size: (0.5, 0.1)
Ground Layer: Chọn "Ground"

Wall Check: Gán WallCheck GameObject
Wall Check Size: (0.1, 0.5)
Wall Layer: Chọn "Ground" hoặc "Wall"

Max Air Jumps: 1 (double jump)
Fall Multiplier: 2.5
Low Jump Multiplier: 2.0
Coyote Time: 0.15
```

#### PlayerStats:
```
Max Health: 100
Max Stamina: 100
Stamina Regen Rate: 10

Move Speed: 8
Jump Force: 15
Dash Speed: 20
Dash Duration: 0.2
Dash Cooldown: 1.0
Dash Stamina Cost: 20

Wall Climb Speed: 5
Wall Slide Speed: 2
Wall Climb Stamina Cost: 15
```

### 3. Setup  Manager
```
Edit → Project Settings →  Manager

✅ Horizontal (A/D, Arrow Keys) - Mặc định OK
✅ Jump (Space) - Mặc định OK
✅ W/S được đọc trực tiếp, KHÔNG cần setup Vertical axis
```

**Lưu ý:** Code đã được cập nhật để đọc **W/S trực tiếp** từ keyboard, không cần config  Manager!

### 4. Setup Layers
```
Edit → Project Settings → Tags and Layers
Create layers:
- Ground (set cho platforms/walls)
- Player
```

### 5. Test!
- Press **Play**
- Use **A/D** or **Arrow Keys** to move
- Press **Space** to jump (double jump!)
- Press **Shift** to dash
- Touch wall + **Hold A/D** + **W/S** to climb!

---

## 🎮 Controls

|  | Action |
|-------|--------|
| **A / Left Arrow** | Di chuyển trái |
| **D / Right Arrow** | Di chuyển phải |
| **W / Up Arrow** | Di chuyển lên (khi wall climb) |
| **S / Down Arrow** | Di chuyển xuống (khi wall climb) |
| **Space** | Jump / Double Jump / Wall Jump |
| **Shift / Right Mouse** | Dash |
| **Left Mouse / J** | Attack (placeholder) |
| **E / F** | Interact (placeholder) |

### **Wall Climb Controls (QUAN TRỌNG!):**
- **Hold A** + Touch wall → Bám tường trái (wall grab)
- **Hold D** + Touch wall → Bám tường phải (wall grab)
- **A + W** → Trèo lên (tiêu stamina)
- **A + S** → Trượt xuống
- **A + Space** → Wall jump
- **Release A/D** → RƠI XUỐNG NGAY!

---

## 🔧 Configuration

### Movement Feel Adjustments:

#### **Fast & Responsive** (Hollow Knight-like):
```
Move Speed: 10
Jump Force: 16
Dash Speed: 25
Wall Climb Speed: 6
```

#### **Slow & Heavy** (Dark Souls-like):
```
Move Speed: 6
Jump Force: 12
Dash Speed: 15
Wall Climb Speed: 4
```

#### **Floaty Jump** (Mario-like):
```
Jump Force: 18
Fall Multiplier: 1.5
Low Jump Multiplier: 1.2
```

#### **Snappy Jump** (Celeste-like):
```
Jump Force: 14
Fall Multiplier: 3.0
Low Jump Multiplier: 2.5
Coyote Time: 0.1
```

---

## 🎨 Animation Setup

Xem chi tiết tại:
- **[ANIMATION_GUIDE.md](ANIMATION_GUIDE.md)** - Full guide với screenshots
- **[ANIMATION_QUICK_SETUP.md](ANIMATION_QUICK_SETUP.md)** - Quick reference

### Animator Parameters Cần Thiết:
```
Bool Parameters:
- IsGrounded
- IsWallClimbing

Float Parameters:
- Speed (horizontal velocity)
- YVelocity (vertical velocity)
- WallClimbSpeed (climb velocity)

Trigger Parameters:
- Jump
- Dash
- Death
```

---

## 🧗 Wall Climb System

Xem hướng dẫn chi tiết: **[WALL_CLIMB_GUIDE.md](WALL_CLIMB_GUIDE.md)**

### Key Points:
- **BẮT BUỘC GIỮ A/D** để bám tường
- Nhả A/D → Rơi ngay lập tức!
- A + W → Trèo lên (tiêu stamina)
- A + S → Trượt xuống (không tiêu stamina)
- A + Space → Wall jump

---

## 🐛 Troubleshooting

### Player không di chuyển được?
- ✅ Check Rigidbody2D có Body Type = Dynamic?
- ✅ Check Freeze Rotation Z đã bật?
- ✅ Check  Manager có Horizontal/Vertical axes?

### Không nhảy được?
- ✅ Check Jump  trong  Manager (Space)
- ✅ Check Ground Layer đã được gán?
- ✅ Check GroundCheck position (Y = -0.5 dưới player)

### Wall Climb không hoạt động?
- ✅ **Có GIỮ A/D không?** (Phải giữ, không chỉ nhấn!)
- ✅ Check Wall Check GameObject đã gán?
- ✅ Check Wall Layer đã setup?
- ✅ Check WallCheck position (X = 0.5 bên cạnh player)
- ✅ Check đủ stamina? (cần ít nhất 15)

### Player rơi xuyên qua platform?
- ✅ Platform có Collider2D?
- ✅ Platform layer có trong Ground Layer không?
- ✅ Check Collision Matrix (Edit → Project Settings → Physics 2D)

### Dash không hoạt động?
- ✅ Check đủ stamina? (cần 20)
- ✅ Check cooldown đã hết chưa? (1 giây)

---

## 📊 Performance Tips

### Optimization:
- Ground/Wall checks chỉ chạy trong Update (không phải FixedUpdate)
- State transitions check trước khi thay đổi
- Animator parameters chỉ update khi thay đổi

### Debug Mode:
```csharp
// Trong PlayerMovement.cs, thêm debug logs:
Debug.Log($"IsGrounded: {IsGrounded}, IsTouchingWall: {IsTouchingWall}");
Debug.Log($"Holding Into Wall: {IsHoldingIntoWall()}");
Debug.Log($"Velocity: {Velocity}");
```

---

## 🎯 Next Steps

### Combat System:
- [ ] Attack State (3-hit combo)
- [ ] Parry System
- [ ] Damage calculation
- [ ] Hit effects

### Advanced Movement:
- [ ] Ledge grab
- [ ] Corner climb
- [ ] Wall run (horizontal)
- [ ] Slide

### Systems:
- [ ] Character switching (team formation)
- [ ] Dialogue system
- [ ] Quest system
- [ ] Save/Load system

---

## 📚 Additional Resources

- **Unity Docs**: https://docs.unity3d.com/Manual/class-Rigidbody2D.html
- **State Machine Pattern**: https://gameprogrammingpatterns.com/state.html
- **Metroidvania Design**: [GDC Talks on Hollow Knight]

---

## 💬 FAQ

**Q: Tại sao phải giữ A/D để bám tường?**  
A: Giống Hollow Knight! Logic thực tế hơn - nhân vật phải chủ động ép vào tường, không tự động bám.

**Q: Làm sao để wall jump có hướng?**  
A: Xem WALL_CLIMB_GUIDE.md phần "Advanced Features"

**Q: Có thể dùng New  System không?**  
A: Có, nhưng cần refactor Player.cs. Hiện tại dùng Legacy  System.

**Q: Animation không smooth?**  
A: Check Animator transition duration, has exit time, và blend tree setup.

---

## 🎉 Credits

Created for **Dream Knight** - Metroidvania game project
Inspired by: Hollow Knight, Celeste, Ori and the Blind Forest

**Happy Coding!** ✨
