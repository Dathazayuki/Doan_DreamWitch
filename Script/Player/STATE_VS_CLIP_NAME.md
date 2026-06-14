# 🎯 State Name vs Animation Clip Name - Giải Thích

## ❓ Câu Hỏi: Tên Nào Phải Dùng?

Khi gọi:
```csharp
animationController.PlayAnimation("???");
```

Bạn dùng:
- ✅ **Tên của STATE** trong Animator Controller
- ❌ KHÔNG phải tên file Animation Clip (.anim)

---

## 📊 Sự Khác Biệt

### **Animation Clip (File .anim)**
```
File trong Project:
Assets/Animations/Player/
  ├─ player_idle_v2.anim          ← Tên file
  ├─ player_run_final.anim        ← Tên file  
  ├─ knight_jump_animation.anim   ← Tên file
  └─ ...
```

### **State (Trong Animator Controller)**
```
Animator Controller window:
Base Layer:
  ├─ Idle        ← STATE name (dùng cái này!)
  │   └─ Motion: player_idle_v2
  ├─ Run         ← STATE name
  │   └─ Motion: player_run_final
  ├─ Jump        ← STATE name
  │   └─ Motion: knight_jump_animation
  └─ ...
```

---

## 🎮 Ví Dụ Thực Tế

### **Setup trong Animator:**

```
1. Kéo file "knight_idle_animation_v3.anim" vào Animator
2. Unity tự tạo state với tên: "knight_idle_animation_v3" (từ file name)
3. Bạn rename state thành: "Idle"
```

**Kết quả:**
- State Name: `Idle` ← Dùng cái này trong code!
- Animation Clip: `knight_idle_animation_v3.anim`

### **Trong Code:**

```csharp
// ✅ ĐÚNG - Dùng STATE name
animationController.PlayAnimation("Idle");

// ❌ SAI - Không dùng clip name
animationController.PlayAnimation("knight_idle_animation_v3");
```

---

## 🔍 Cách Xem State Name Trong Unity

### **Method 1: Animator Window**
```
1. Window → Animation → Animator
2. Select Player GameObject
3. Nhìn vào các box trong Animator
4. Tên trên box = STATE name
```

**Ví dụ:**
```
╔═══════════════╗
║     Idle      ║  ← Tên này = "Idle"
╚═══════════════╝
```

### **Method 2: Inspector**
```
1. Click vào State trong Animator window
2. Inspector (góc phải):
   - Name: Idle          ← STATE name (dùng cái này!)
   - Motion: player_idle ← Animation Clip
```

### **Method 3: Debug Code**

Thêm vào `Start()` của PlayerController:
```csharp
void Start()
{
    // ...existing code...
    
    // Debug: List tất cả states
    AnimationController.LogAvailableStates();
}
```

Console sẽ hiện:
```
=== Available Animator STATES ===
Layer: Base Layer
  STATE: 'Idle' → Clip: 'player_idle_v2'
  STATE: 'Run' → Clip: 'player_run'
  STATE: 'Jump' → Clip: 'knight_jump_anim'
```

**→ Dùng tên trong 'STATE: ...'**

---

## 🛠️ Setup Đúng Cách

### **Bước 1: Tạo States với tên ngắn gọn**

```
Trong Animator:
- Tạo state, đặt tên: "Idle" (không phải "player_idle_animation")
- Tạo state, đặt tên: "Run"
- Tạo state, đặt tên: "Jump"
- ...
```

### **Bước 2: Gán Animation Clips vào States**

```
1. Click state "Idle"
2. Inspector → Motion: Drag file animation vào
   (Có thể là "player_idle.anim", "knight_idle_v2.anim", bất kỳ)
```

### **Bước 3: Dùng State Names trong Code**

```csharp
// Constants trong PlayerAnimationController.cs:
public const string IDLE = "Idle";    ← Khớp với STATE name
public const string RUN = "Run";      ← Khớp với STATE name
public const string JUMP = "Jump";    ← Khớp với STATE name
```

---

## ⚠️ Common Mistakes

### **Mistake 1: Dùng tên file .anim**
```csharp
❌ animationController.PlayAnimation("player_idle_animation_v2");
✅ animationController.PlayAnimation("Idle");
```

### **Mistake 2: State name không khớp constants**
```
Animator:
  State name: "idle" (lowercase)

Code:
  public const string IDLE = "Idle"; (capital I)

❌ Không khớp → Animation không play!
✅ Đổi state name thành "Idle" hoặc constant thành "idle"
```

### **Mistake 3: Quên rename state sau khi kéo animation vào**
```
1. Kéo "player_jump_final_v3.anim" vào Animator
2. State tự động đặt tên: "player_jump_final_v3"
3. ❌ Quên rename → Code gọi "Jump" → Không tìm thấy!
4. ✅ Rename state thành "Jump"
```

---

## 🎯 Best Practices

### **State Naming Convention:**
```
✅ Tên ngắn, rõ ràng, PascalCase:
   - Idle
   - Run
   - Jump
   - Fall
   - Dash
   - Attack1
   - WallGrab

❌ Tên dài, mô tả file:
   - player_idle_animation_v3
   - knight_run_sprite_final
   - jump_anim_2023_updated
```

### **Animation Clip Naming:**
```
Animation clips có thể đặt tên chi tiết:
   - player_knight_idle_v3.anim
   - run_with_sword_final.anim
   - jump_animation_sprite_32x32.anim

Nhưng STATE phải tên ngắn: Idle, Run, Jump
```

---

## 📝 Checklist

Khi gặp lỗi "State could not be found":

- [ ] Mở Animator window
- [ ] Kiểm tra STATE name (không phải clip name)
- [ ] So sánh với constants trong `PlayerAnimationController.cs`
- [ ] Verify khớp CHÍNH XÁC (case-sensitive!)
- [ ] Test: `AnimationController.LogAvailableStates()` để xem tất cả states

---

## 🔧 Quick Debug

Thêm code này để test:
```csharp
void Update()
{
  if (Input.GetKeyDown(KeyCode.F1))
  {
    Debug.Log("Testing Idle animation...");
    AnimationController.PlayAnimation("Idle");
  }
    
  if (Input.GetKeyDown(KeyCode.F2))
  {
    AnimationController.LogAvailableStates();
  }
}
```

Press F1: Test play Idle  
Press F2: List tất cả states

---

## 📚 Summary

| Aspect | Animation Clip | State |
|--------|---------------|-------|
| **Là gì?** | File .anim | Box trong Animator |
| **Tên ví dụ** | `player_idle_v2.anim` | `Idle` |
| **Đặt tên ở đâu?** | Project window | Animator window |
| **Dùng trong code?** | ❌ NO | ✅ YES |
| **API** | - | `animator.Play("Idle")` |

**→ Luôn dùng STATE name!** ✨

---

**TL;DR:** Dùng tên box trong Animator, không phải tên file animation! 🎯
