# 📁 Project Structure - Dream Knight

## 🎯 Overview

Dream Knight là game **Metroidvania** với các cải tiến:
- ✅ **Cơ chế đội hình** (Character Switching)
- ✅ **Parry System** (Combat mechanic)
- ✅ **Wall Climb Mechanics** (Exploration)

---

## 📂 Directory Structure

```
Assets/Project/Script/
│
├─ Interfaces/
│  ├─ IDamageable.cs           # Interface cho entities nhận damage
│  └─ IInteractable.cs         # Interface cho objects tương tác
│
├─ Player/
│  ├─ PlayerController.cs      # Main controller, orchestrator
│  ├─ PlayerInput.cs           # Input handling với buffering
│  ├─ PlayerMovement.cs        # Movement + Physics + Wall Climb
│  ├─ PlayerStats.cs           # HP, Stamina, speeds
│  ├─ PlayerStateMachine.cs    # State machine manager
│  │
│  ├─ States/
│  │  ├─ PlayerState.cs       # Base class cho tất cả states
│  │  ├─ IdleState.cs         # Đứng yên
│  │  ├─ MoveState.cs         # Di chuyển
│  │  ├─ JumpState.cs         # Nhảy/Rơi
│  │  ├─ DashState.cs         # Dash
│  │  └─ WallClimbState.cs    # Wall Climb (BẮT BUỘC GIỮ A/D!)
│  │
│  ├─ README.md                      # Setup guide & overview
│  ├─ ANIMATION_GUIDE.md             # Animation system chi tiết
│  ├─ ANIMATION_QUICK_SETUP.md       # Quick reference
│  └─ WALL_CLIMB_GUIDE.md            # Wall climb hướng dẫn
│
├─ Enemy/                      # [TODO] Enemy AI system
│  └─ (chưa implement)
│
├─ Combat/                     # [TODO] Combat system
│  └─ (chưa implement)
│
├─ Dialogue/                   # [TODO] Dialogue system
│  └─ (chưa implement)
│
├─ Quest/                      # [TODO] Quest system
│  └─ (chưa implement)
│
├─ SaveLoad/                   # [TODO] Save/Load system
│  └─ (chưa implement)
│
├─ UI/                         # [TODO] UI/HUD system
│  └─ (chưa implement)
│
└─ PROJECT_STRUCTURE.md        # This file
```

---

## 🎮 Player System (Hoàn Thiện)

### Core Components:

#### **PlayerController.cs**
```
Vai trò: Main orchestrator
- Kết nối tất cả components
- Quản lý State Machine
- Implement IDamageable interface
- Handle events (OnDeath, OnHealthChanged)
```

#### **PlayerInput.cs**
```
Vai trò: Input handling
- Read keyboard/mouse input
- Jump buffering (0.15s)
- Dash buffering (0.1s)
- Enable/Disable input control
```

#### **PlayerMovement.cs**
```
Vai trò: Movement & Physics
Features:
- Basic movement với smooth acceleration
- Jump system (single, double, coyote time)
- Dash theo 8 hướng
- Wall Climb (YÊU CẦU GIỮ A/D!)
- Ground/Wall detection
```

#### **PlayerStats.cs**
```
Vai trò: Stats management
Stats:
- Health: Current/Max HP
- Stamina: Current/Max, Regeneration
- Movement: MoveSpeed, JumpForce
- Dash: Speed, Duration, Cooldown, Cost
- Wall Climb: ClimbSpeed, SlideSpeed, Cost
Events: OnDeath, OnHealthChanged, OnStaminaChanged
```

### State Machine:

#### **States Implemented:**

1. **IdleState** - Đứng yên
   - Transitions: → Move, Jump, Dash

2. **MoveState** - Di chuyển
   - Smooth movement
   - Transitions: → Idle, Jump, Dash

3. **JumpState** - Nhảy/Rơi
   - Single jump, Double jump
   - Coyote time, Jump buffering
   - Transitions: → Wall Climb, Dash, Ground

4. **DashState** - Dash
   - 8 directional dash
   - Stamina cost, Cooldown
   - Transitions: → Ground, Air

5. **WallClimbState** - Wall Climb ⭐
   - **BẮT BUỘC GIỮ A/D!**
   - Trèo lên (A/D + W)
   - Trượt xuống (A/D + S)
   - Wall jump (A/D + Space)
   - Nhả A/D → RƠI!
   - Transitions: → Jump, Dash, Ground

---

## 🎨 Animation System

### Animator Parameters:

| Parameter | Type | Usage |
|-----------|------|-------|
| IsGrounded | Bool | Tracking ground state |
| IsWallClimbing | Bool | Tracking wall climb |
| Speed | Float | Horizontal velocity |
| YVelocity | Float | Vertical velocity |
| WallClimbSpeed | Float | Climb velocity |
| Jump | Trigger | Trigger jump animation |
| Dash | Trigger | Trigger dash animation |
| Death | Trigger | Trigger death animation |

### Animation States:

```
Animator Controller: PlayerAnimatorController
├─ Idle          (IsGrounded + Speed = 0)
├─ Run           (IsGrounded + Speed > 0)
├─ Jump          (Jump trigger)
├─ Fall          (YVelocity < 0)
├─ Dash          (Dash trigger, Any State)
├─ WallClimb     (IsWallClimbing = true, Any State)
└─ Death         (Death trigger, Any State)
```

**Docs:**
- [ANIMATION_GUIDE.md](Player/ANIMATION_GUIDE.md) - Chi tiết
- [ANIMATION_QUICK_SETUP.md](Player/ANIMATION_QUICK_SETUP.md) - Quick setup

---

## 🧗 Wall Climb System

### Logic:

```
IsHoldingIntoWall() {
    if facing right:
        return input.x > 0.1f  // Giữ D
    else:
        return input.x < -0.1f // Giữ A
}

HandleWallClimb() {
    if (!IsHoldingIntoWall()) {
        StopWallClimb()  // RƠI NGAY!
        return
    }
    // Xử lý trèo lên/xuống
}
```

### Controls:

| Input | Action |
|-------|--------|
| Hold A/D | Bám tường (wall grab) |
| A/D + W | Trèo lên (tiêu stamina) |
| A/D + S | Trượt xuống |
| A/D + Space | Wall jump |
| Release A/D | RƠI XUỐNG! |

**Docs:** [WALL_CLIMB_GUIDE.md](Player/WALL_CLIMB_GUIDE.md)

---

## 🔮 Features Đã Hoàn Thành

### ✅ Player Controller System (100%)
- [x] Basic Movement (Walk, Run)
- [x] Jump System (Single, Double, Coyote Time)
- [x] Dash System (8 directions, Cooldown, Stamina)
- [x] Wall Climb System (Grab, Climb, Slide, Wall Jump)
- [x] State Machine (5 states)
- [x] Input Buffering (Jump, Dash)
- [x] Stats System (HP, Stamina)
- [x] Animation Integration

---

## 📋 TODO - Các Features Sắp Tới

### 🎯 Priority 1: Combat System

#### Attack System:
```
[ ] Base Attack State
[ ] 3-hit Combo System
[ ] Attack animation events
[ ] Hitbox detection (Collider2D)
[ ] Damage calculation
[ ] Hit effects (screen shake, freeze frame)
```

#### Parry System:
```
[ ] Parry State
[ ] Timing window detection
[ ] Perfect parry vs normal parry
[ ] Counter-attack mechanic
[ ] Parry animation & VFX
```

### 🎯 Priority 2: Enemy System

```
Enemy/
├─ EnemyController.cs       # Base enemy class
├─ EnemyAI.cs              # AI behavior
├─ EnemyStats.cs           # Enemy stats
└─ States/
   ├─ EnemyIdleState.cs
   ├─ EnemyPatrolState.cs
   ├─ EnemyChaseState.cs
   └─ EnemyAttackState.cs
```

### 🎯 Priority 3: Character Switching

```
[ ] Party System (3 characters max)
[ ] Character Switch mechanic
[ ] Switch animation & transition
[ ] Character-specific abilities
[ ] Formation system
```

### 🎯 Priority 4: Progression Systems

#### Dialogue System:
```
Dialogue/
├─ DialogueManager.cs
├─ DialogueUI.cs
├─ DialogueData.cs (ScriptableObject)
└─ DialogueNode.cs
```

#### Quest System:
```
Quest/
├─ QuestManager.cs
├─ Quest.cs (ScriptableObject)
├─ QuestUI.cs
└─ QuestObjective.cs
```

#### Save/Load:
```
SaveLoad/
├─ SaveManager.cs
├─ SaveData.cs
├─ GameData.cs
└─ JsonDataService.cs
```

### 🎯 Priority 5: UI/UX

```
UI/
├─ MainMenu/
├─ HUD/
│  ├─ HealthBar.cs
│  ├─ StaminaBar.cs
│  └─ HUDController.cs
├─ PauseMenu/
└─ SettingsMenu/
```

---

## 🛠️ Development Guidelines

### Code Style:
```csharp
// Naming Convention:
- Classes: PascalCase (PlayerController)
- Methods: PascalCase (HandleMovement)
- Variables: camelCase (isGrounded)
- Private fields: camelCase (playerInput)
- Constants: UPPER_SNAKE_CASE (MAX_HEALTH)

// Namespaces:
namespace DreamKnight.Player { }
namespace DreamKnight.Enemy { }
namespace DreamKnight.Combat { }
```

### Design Patterns:
- **State Machine**: cho Player/Enemy behavior
- **Observer**: cho Events (OnDeath, OnHealthChanged)
- **Singleton**: cho Managers (GameManager, SaveManager)
- **Object Pool**: cho projectiles, particles
- **ScriptableObject**: cho data (Quests, Dialogues, Stats)

### Performance:
- Cache component references trong Awake()
- Use object pooling cho frequently spawned objects
- Optimize collision checks với layers
- Use FixedUpdate cho physics
- Use Update cho input/logic

---

## 📚 Documentation Guide

### Player System Docs:
1. **[Player/README.md](Player/README.md)**
   - Quick setup guide
   - Controls reference
   - Troubleshooting
   
2. **[Player/ANIMATION_GUIDE.md](Player/ANIMATION_GUIDE.md)**
   - Full animation setup
   - Animator parameters
   - Transitions guide

3. **[Player/ANIMATION_QUICK_SETUP.md](Player/ANIMATION_QUICK_SETUP.md)**
   - Checklist format
   - Quick reference

4. **[Player/WALL_CLIMB_GUIDE.md](Player/WALL_CLIMB_GUIDE.md)**
   - Wall climb logic explained
   - Setup instructions
   - Debugging tips

---

## 🎯 Current Status: Phase 1 Complete

### ✅ Completed:
- Player Controller System (Full)
- Movement Mechanics (Full)
- State Machine (Full)
- Wall Climb System (Full)
- Documentation (Complete)

### 🚧 Next Phase: Combat System
- Attack System
- Parry System
- Enemy AI
- Damage System

---

## 🔗 Quick Links

- **Player Setup**: [Player/README.md](Player/README.md)
- **Animation Setup**: [Player/ANIMATION_GUIDE.md](Player/ANIMATION_GUIDE.md)
- **Wall Climb Guide**: [Player/WALL_CLIMB_GUIDE.md](Player/WALL_CLIMB_GUIDE.md)

---

**Project Status: Phase 1 - Player Controller ✅ Complete**

Ready to proceed to Phase 2: Combat System! 🎮✨
