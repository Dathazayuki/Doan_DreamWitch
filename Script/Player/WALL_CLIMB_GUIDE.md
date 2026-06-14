# 🧗 Wall Climb System - Dream Knight

## 📋 Tổng quan

Hệ thống trèo tường cho phép Player **bám vào tường** và **trèo lên/xuống** khi giữ tổ hợp phím.

### ⚠️ **QUAN TRỌNG - Logic Đúng:**
- **BẮT BUỘC GIỮ A/D** để bám tường (wall grab)
- **A + W** để trèo lên (tiêu stamina)
- **A + S** để trượt xuống
- **A + Space** để wall jump
- **Nhả A/D** → **RƠI NGAY LẬP TỨC!**

---

## ✅ Features Hoàn Thiện

### 📦 Code đã thêm:

#### 1. **PlayerStats.cs**
```csharp
✅ Wall Climb Speed (5.0)
✅ Wall Slide Speed (2.0)
✅ Wall Climb Stamina Cost (15.0)
```

#### 2. **PlayerMovement.cs**
```csharp
✅ IsWallClimbing property
✅ IsWallSliding property
✅ CanWallClimb() method
✅ IsHoldingIntoWall() method - LOGIC ĐÚNG!
✅ StartWallClimb() method
✅ StopWallClimb() method
✅ HandleWallClimb() logic - CHECK GIỮ A/D!
```

#### 3. **WallClimbState.cs**
```csharp
✅ Enter/Exit logic
✅ Transitions kiểm tra IsHoldingIntoWall()
✅ Wall jump support
✅ Auto exit khi nhả A/D
✅ Auto exit khi hết stamina
```

#### 4. **PlayerController.cs**
```csharp
✅ WallClimbState property
✅ Initialize WallClimbState
```

#### 5. **JumpState.cs**
```csharp
✅ Transition to WallClimbState khi IsHoldingIntoWall()
```

---

## 🎮 Cách hoạt động

### Flow Logic:
```
Player đang nhảy/rơi (JumpState)
    ↓
Chạm tường + GIỮ phím A (trái) hoặc D (phải)
    ↓
IsHoldingIntoWall() = true
    ↓
CanWallClimb() = true (có tường, giữ phím, đủ stamina)
    ↓
✅ Transition → WallClimbState (Wall Grab)
    ↓
Player bám tường (đứng yên, không tốn stamina)
    ↓
GIỮ A + nhấn W → Trèo lên (tiêu stamina)
GIỮ A + nhấn S → Trượt xuống
GIỮ A + Space → Wall jump
NHẢẢ A → RƠI XUỐNG!
```

### 🎯 Điều khiển Wall Climb:

#### 🧗 Bám tường (Wall Grab):
- **Player facing right** + **GIỮ D** + chạm tường → Bám tường phải
- **Player facing left** + **GIỮ A** + chạm tường → Bám tường trái
- Player sẽ bám vào tường và đứng yên (không tốn stamina)

#### ⬆️ Trèo lên:
- **GIỮ A/D + W** → Trèo lên từ từ
- ⚠️ **Tiêu tốn Stamina** (15/giây)

#### ⬇️ Trượt xuống:
- **GIỮ A/D + S** → Trượt xuống nhanh
- Không tiêu tốn stamina

#### 🦘 Wall Jump:
- **GIỮ A/D + Space** → Nhảy ra khỏi tường

#### 💨 Dash:
- **Shift** → Dash ra khỏi tường

### Điều kiện thoát WallClimbState:
- ❌ **Nhả phím A/D** → Rơi xuống ngay lập tức
- ❌ **Rời khỏi tường**
- ❌ **Chạm đất**
- ❌ **Hết stamina** → Tự động rơi
- ✅ **Jump hoặc Dash**

### 💡 Tips:
- **Chỉ giữ A/D**: Đứng yên trên tường - **Không tốn stamina**
- **A/D + W**: Trèo lên - **Tốn stamina**
- **A/D + S**: Trượt xuống - **Không tốn stamina**
- **Nhả A/D**: **RƠI NGAY!**

---

## 🔧 Setup trong Unity

### Bước 1: Cấu hình Wall Detection

Wall detection đã có sẵn trong PlayerMovement:

1. **Tạo Wall Check GameObject**:
   - Select Player → Right-click → Create Empty
   - Rename: "WallCheck"
   - Position: **(0.5, 0, 0)** - ở bên cạnh player
   
2. **PlayerMovement Inspector**:
   - Wall Check: Gán WallCheck GameObject
   - Wall Check Size: **(0.1, 0.5)**
   - Wall Layer: Chọn layer **"Ground"** hoặc **"Wall"**

### Bước 2: Setup Layers

Đảm bảo các walls/platforms có Layer phù hợp:
```
Edit → Project Settings → Tags and Layers
Create layer: "Wall" (if needed)
Assign to all wall objects
```

### Bước 3: Điều chỉnh Parameters (Optional)

#### PlayerStats Inspector:
```
Wall Climb Stats:
├─ Wall Climb Speed: 5.0        (tốc độ trèo lên)
├─ Wall Slide Speed: 2.0        (tốc độ trượt xuống)
└─ Wall Climb Stamina Cost: 15  (stamina/giây khi trèo)
```

**Recommendations:**
- **Fast Climb**: Wall Climb Speed = 6-8
- **Slow Climb**: Wall Climb Speed = 3-4
- **High Stamina Cost**: 20-25 (khó trèo lâu)
- **Low Stamina Cost**: 10-15 (dễ trèo)

### Bước 4: Setup Animator (Optional)

#### Thêm Parameters:
```
Animator Parameters:
├─ IsWallClimbing (Bool)       - True khi đang trèo
└─ WallClimbSpeed (Float)      - Velocity.y khi trèo
```

#### Thêm Animation:
1. Tạo animation clip: `player_wall_climb.anim`
2. Kéo vào Animator
3. Tạo transitions:
   ```
   Any State → WallClimb:
     Condition: IsWallClimbing = true
     Has Exit Time: ❌
     Duration: 0s
   
   WallClimb → Jump/Idle/Run:
     Condition: IsWallClimbing = false
     Has Exit Time: ❌
     Duration: 0.1s
   ```

---

## 🎯 Testing Checklist

### Test Scenarios:

#### ✅ Basic Wall Grab (Bám tường)
1. Player facing right, nhảy vào tường phải
2. **GIỮ phím D** (không nhả!)
3. ✅ Player nên bám vào tường và đứng yên
4. ✅ Không tốn stamina
5. ✅ Debug log: "Started Wall Climbing"

#### ✅ Wall Climb Up (Trèo lên)
1. Đang bám tường (giữ D)
2. **GIỮ D + nhấn W** cùng lúc
3. ✅ Player nên trèo lên từ từ
4. ✅ Stamina giảm dần
5. ✅ Khi nhả W → Player đứng yên trên tường

#### ✅ Wall Slide Down (Trượt xuống)
1. Đang bám tường (giữ D)
2. **GIỮ D + nhấn S** cùng lúc
3. ✅ Player nên trượt xuống nhanh
4. ✅ Không tốn stamina

#### ✅ Wall Jump
1. Đang bám tường (giữ D)
2. **GIỮ D + nhấn Space**
3. ✅ Player nhảy ra khỏi tường
4. ✅ Chuyển sang JumpState

#### ✅ Release Input (Nhả phím)
1. Đang bám tường (giữ D)
2. **Nhả phím D**
3. ✅ Player rơi xuống ngay lập tức
4. ✅ Debug log: "Nhả A/D → Rơi xuống!"

#### ✅ Hết Stamina
1. Giữ D + W để trèo cho đến khi hết stamina
2. ✅ Debug log: "Hết stamina → Rơi!"
3. ✅ Player rơi xuống (JumpState)

#### ✅ Bám tường bên trái
1. Player facing left, nhảy vào tường trái
2. **GIỮ phím A**
3. ✅ Player bám vào tường trái
4. **A + W** → Trèo lên
5. **A + Space** → Wall jump

---

## 🐛 Troubleshooting

### ❌ Problem: Không thể bám tường

**Checks:**
- ✅ Có **GIỮ phím A/D** không? (Phải GIỮ, không chỉ nhấn!)
- ✅ Player đang facing đúng hướng không?
  - Tường bên phải → Player facing right → Giữ D
  - Tường bên trái → Player facing left → Giữ A
- ✅ Wall Check GameObject có được gán trong Inspector?
- ✅ Wall Layer có đúng không?
- ✅ Wall có Collider2D không?
- ✅ Player có đủ stamina không? (cần ít nhất 15)

**Debug trong Console:**
```
Khi GIỮ D và chạm tường phải, Console phải hiện:
"Started Wall Climbing"
"Entered Wall Climb State - GIỮ A/D để bám tường!"
```

**Debug trong Code:**
```csharp
// Thêm vào PlayerMovement.IsHoldingIntoWall()
Debug.Log($"Touching Wall: {isTouchingWall}");
Debug.Log($"Facing Right: {facingRight}");
Debug.Log($"Horizontal Input: {playerInput.MoveInput.x}");
Debug.Log($"Holding Into Wall: {IsHoldingIntoWall()}");
```

### ✅ Expected Debug Output (Tường phải):
```
Touching Wall: True
Facing Right: True
Horizontal Input: 1 (giữ D)
Holding Into Wall: True ✅
```

### ❌ Problem: Bám tường nhưng không trèo được

**Nguyên nhân:** Có thể không giữ đủ chặt A/D hoặc input threshold vấn đề.

**Fix:**
1. **GIỮ chặt phím D** (đừng nhả!)
2. Tiếp tục **GIỮ D** + **Nhấn W**
3. Player sẽ trèo lên

**Test Input:**
- Tường phải: **D (giữ) + W (giữ)** = Trèo lên
- Tường trái: **A (giữ) + W (giữ)** = Trèo lên

### ❌ Problem: Player tự động rơi khi bám tường

**Nguyên nhân 1:** Không giữ phím D đủ lâu.

**Fix:**
- Test: GIỮ chặt phím D trong 2-3 giây → Player phải đứng yên
- Check Console: Nếu thấy "Nhả A/D → Rơi xuống!" → Đang nhả phím!

**Nguyên nhân 2:** Input threshold quá cao.

**Fix trong PlayerMovement.IsHoldingIntoWall():**
```csharp
// Line ~287 - Giảm threshold nếu cần
if (facingRight)
{
    return horizontalInput > 0.1f; // Đang là 0.1f - OK cho keyboard
}
```

### ❌ Problem: Wall Check không detect tường

**Fix:**
1. Select Player → Scene view
2. Xem Gizmos màu xanh dương (Wall Check box)
3. Wall Check phải **chạm vào wall** khi player ở sát tường
4. Nếu không chạm:
   - Adjust Wall Check Position X (thử 0.4 - 0.6)
   - Adjust Wall Check Size X (thử 0.15 - 0.2)

### ❌ Problem: Facing sai hướng

**Issue:** Player facing left nhưng tường ở bên phải → Không bám được!

**Explanation:**
```
Logic của IsHoldingIntoWall():
- if (facingRight) → wallCheck ở bên phải → cần giữ D (input > 0)
- if (!facingRight) → wallCheck ở bên trái → cần giữ A (input < 0)

VÌ wallCheck là CHILD của player, nó flip theo player!
```

**Solution:**
Player sẽ tự động flip khi di chuyển. Đảm bảo:
1. Di chuyển về phía tường trước
2. Player sẽ facing đúng hướng
3. Giữ phím theo hướng đó để bám

---

## 🔬 Code Logic Explained

### **IsHoldingIntoWall() - Core Logic:**

```csharp
public bool IsHoldingIntoWall()
{
    if (!isTouchingWall) return false;
    
    float horizontalInput = playerInput.MoveInput.x;
    
    // VÌ wallCheck là CHILD của player:
    // - Player facing right (facingRight = true)
    //   → transform.localScale.x > 0
    //   → wallCheck ở bên PHẢI player
    //   → tường ở bên PHẢI
    //   → cần giữ D (input.x > 0.1f)
    //
    // - Player facing left (facingRight = false)
    //   → transform.localScale.x < 0
    //   → wallCheck flip sang bên TRÁI
    //   → tường ở bên TRÁI
    //   → cần giữ A (input.x < -0.1f)
    
    if (facingRight)
    {
        return horizontalInput > 0.1f; // Giữ D
    }
    else
    {
        return horizontalInput < -0.1f; // Giữ A
    }
}
```

### **HandleWallClimb() - Continuous Check:**

```csharp
private void HandleWallClimb()
{
    // ✅ CHECK QUAN TRỌNG NHẤT: Còn giữ A/D không?
    if (!IsHoldingIntoWall())
    {
        // ❌ NHẢẢ A/D = RƠI NGAY!
        StopWallClimb();
        return; // Exit method
    }
    
    // ✅ Còn giữ A/D → Tiếp tục xử lý
    float verticalInput = playerInput.MoveInput.y;
    
    if (verticalInput > 0.1f)
    {
        // A/D + W → Trèo lên
    }
    else if (verticalInput < -0.1f)
    {
        // A/D + S → Trượt xuống
    }
    else
    {
        // Chỉ giữ A/D → Đứng yên (wall grab)
    }
}
```

### **WallClimbState.CheckTransitions() - Priority #1:**

```csharp
public override void CheckTransitions()
{
    // 🥇 PRIORITY #1: Check giữ phím
    if (!movement.IsHoldingIntoWall())
    {
        Debug.Log("Nhả A/D → Rơi xuống!");
        controller.StateMachine.ChangeState(controller.JumpState);
        return; // Rơi ngay!
    }
    
    // ... các checks khác (wall, ground, jump, dash, stamina)
}
```

---

## 📊 Settings Recommendations

### Metroidvania Style (Hollow Knight-like):
```
Wall Climb Speed: 4.5
Wall Slide Speed: 2.0
Stamina Cost: 12
Max Stamina: 100
```

### Fast-Paced Style (Celeste-like):
```
Wall Climb Speed: 7.0
Wall Slide Speed: 3.0
Stamina Cost: 20
Max Stamina: 150
```

### Exploration Style (Ori-like):
```
Wall Climb Speed: 5.5
Wall Slide Speed: 1.5
Stamina Cost: 8
Max Stamina: 120
```

---

## 🎯 Summary

✅ **Wall Climb System hoàn chỉnh!**

**Cách hoạt động:**
1. Player facing right/left
2. Jump vào tường
3. **GIỮ D/A** (theo hướng facing)
4. Player bám tường (wall grab)
5. **D/A + W** → Trèo lên
6. **D/A + S** → Trượt xuống
7. **D/A + Space** → Wall jump
8. **Nhả D/A** → RƠI NGAY!

**Chỉ cần:**
1. Setup Wall Check trong Unity ✅
2. Setup wall layers ✅
3. Test: Giữ chặt A/D! ✅

**Happy Wall Climbing!** 🧗✨
