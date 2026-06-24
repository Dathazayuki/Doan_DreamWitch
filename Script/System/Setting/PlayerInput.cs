using UnityEngine;
using DreamKnight.Systems.Map;
using Project.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System;
using System.IO;
using System.Collections.Generic;

namespace DreamKnight.Player
{
    /// <summary>
    /// Xử lý tất cả input của Player với buffering
    /// </summary>
    public class PlayerInput : MonoBehaviour
    {
        [Serializable]
        private class InputBindingEntry
        {
            public string action;
            public string controlPath;
        }

        [Serializable]
        private class InputBindingProfile
        {
            public List<InputBindingEntry> bindings = new List<InputBindingEntry>();
        }

        public enum BindableAction
        {
            MoveUp = 0,
            MoveDown = 1,
            MoveLeft = 2,
            MoveRight = 3,
            NormalAttack = 4,
            Jump = 5,
            Dodge = 6,
            Interact = 11,
            CameraLookUp = 7,
            CameraLookDown = 8,
            CameraLookLeft = 9,
            CameraLookRight = 10,
            UsePotion = 12,
            UseTool = 13,
            UseSpell = 14,
            Transform = 15
        }

        [Header("Input Buffer Settings")]
        [SerializeField] private float jumpBufferTime = 0.15f;
        [SerializeField] private float dashBufferTime = 0.1f;
        [SerializeField] private bool blockGameplayInputWhenFullMapOpen = true;

        [Header("Input Manager Bindings")]
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private string jumpButton = "Jump";
        [SerializeField] private string dashButton = "Dash";
        [SerializeField] private string attackButton = "Attack";
        [SerializeField] private string interactButton = "Interact";
        [SerializeField] private string usePotionButton = "UsePotion";
        [SerializeField] private string useToolButton = "UseTool";
        [SerializeField] private string useSpellButton = "UseSpell";
        [SerializeField] private string transformButton = "Transform";
        [SerializeField] private bool debugSpellInput = true;
        [SerializeField] private KeyCode cameraLookUpKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode cameraLookDownKey = KeyCode.DownArrow;
        [SerializeField] private KeyCode cameraLookLeftKey = KeyCode.LeftArrow;
        [SerializeField] private KeyCode cameraLookRightKey = KeyCode.RightArrow;
        [SerializeField] private KeyCode transformKey = KeyCode.Tab;

    #if ENABLE_INPUT_SYSTEM
        [Header("New Input System")]
        [SerializeField] private bool preferNewInputSystem = true;

        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction dashAction;
        private InputAction attackAction;
        private InputAction interactAction;
        private InputAction usePotionAction;
        private InputAction useToolAction;
        private InputAction useSpellAction;
        private InputAction transformAction;
        private InputAction lookUpAction;
        private InputAction lookDownAction;
        private InputAction lookLeftAction;
        private InputAction lookRightAction;
    #endif

        // Input state
        private Vector2 moveInput;
        private bool jumpPressed;
        private bool jumpHeld;
        private bool dashPressed;
        private bool dashHeld;
        private bool attackPressed;
        private bool attackHeld;
        private bool interactPressed;
        private bool usePotionPressed;
        private bool useToolPressed;
        private bool useSpellPressed;
        private bool transformPressed;
        private bool cameraLookUpHeld;
        private bool cameraLookDownHeld;
        private bool cameraLookLeftHeld;
        private bool cameraLookRightHeld;


        // Input buffering
        private float jumpBufferCounter;
        private float dashBufferCounter;

        // Input enabled
        private bool inputEnabled = true;

    #if ENABLE_INPUT_SYSTEM
        private bool usingNewInputSystem;
    #endif

        private MapRenderTextureController mapController;

        public event Action<BindableAction, string, string> OnBindingChanged;

        private const string SharedBindingFolderName = "DreamKnight";
        private const string SharedBindingFileName = "input_profile.json";

        private UIInput uiInput;

        // Properties
        public Vector2 MoveInput => inputEnabled ? moveInput : Vector2.zero;
        public bool JumpPressed => inputEnabled && jumpPressed;
        public bool JumpHeld => inputEnabled && jumpHeld;
        public bool DashPressed => inputEnabled && dashPressed;
        public bool DashHeld => inputEnabled && dashHeld;
        public bool AttackPressed => inputEnabled && attackPressed;
        public bool AttackHeld => inputEnabled && attackHeld;
        public bool InteractPressed => inputEnabled && interactPressed;
        public bool UsePotionPressed => inputEnabled && usePotionPressed;
        public bool UseToolPressed => inputEnabled && useToolPressed;
        public bool UseSpellPressed => inputEnabled && useSpellPressed;
        public bool TransformPressed => inputEnabled && transformPressed;

        public Vector2 CameraLookInput
        {
            get
            {
                if (!inputEnabled)
                    return Vector2.zero;

                float x = 0f;
                float y = 0f;
                if (cameraLookLeftHeld) x -= 1f;
                if (cameraLookRightHeld) x += 1f;
                if (cameraLookDownHeld) y -= 1f;
                if (cameraLookUpHeld) y += 1f;
                Vector2 look = new Vector2(x, y);
                return look.sqrMagnitude > 1f ? look.normalized : look;
            }
        }

        private void Update()
        {
            if (!inputEnabled) return;

            if (ShouldBlockByFullMap())
            {
                ClearRuntimeInputState();
                return;
            }

            ReadInput();
            UpdateBuffers();
        }

        private void Awake()
        {
            mapController = FindAnyObjectByType<MapRenderTextureController>();
            uiInput = FindAnyObjectByType<UIInput>();

    #if ENABLE_INPUT_SYSTEM
            SetupNewInputSystem();
            Debug.Log($"[PlayerInput] Backend = {(usingNewInputSystem ? "New Input System" : "Legacy Input Manager")}, preferNewInputSystem={preferNewInputSystem}");
    #endif
        }

        private void OnEnable()
        {
    #if ENABLE_INPUT_SYSTEM
            if (usingNewInputSystem)
            {
            moveAction?.Enable();
            jumpAction?.Enable();
            dashAction?.Enable();
            attackAction?.Enable();
            interactAction?.Enable();
            usePotionAction?.Enable();
            useToolAction?.Enable();
            useSpellAction?.Enable();
            transformAction?.Enable();
            lookUpAction?.Enable();
            lookDownAction?.Enable();
            lookLeftAction?.Enable();
            lookRightAction?.Enable();
            }
    #endif
        }

        private void OnDisable()
        {
    #if ENABLE_INPUT_SYSTEM
            if (usingNewInputSystem)
            {
            moveAction?.Disable();
            jumpAction?.Disable();
            dashAction?.Disable();
            attackAction?.Disable();
            interactAction?.Disable();
            usePotionAction?.Disable();
            useToolAction?.Disable();
            useSpellAction?.Disable();
            transformAction?.Disable();
            lookUpAction?.Disable();
            lookDownAction?.Disable();
            lookLeftAction?.Disable();
            lookRightAction?.Disable();
            }
    #endif
        }

        private void ReadInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingNewInputSystem)
            {
                moveInput = moveAction.ReadValue<Vector2>();
                jumpPressed = jumpAction.WasPressedThisFrame();
                jumpHeld = jumpAction.IsPressed();
                dashPressed = dashAction.WasPressedThisFrame();
                dashHeld = dashAction.IsPressed();
                attackPressed = attackAction.WasPressedThisFrame();
                attackHeld = attackAction.IsPressed();
                interactPressed = interactAction.WasPressedThisFrame();
                usePotionPressed = usePotionAction != null && usePotionAction.WasPressedThisFrame();
                useToolPressed = useToolAction != null && useToolAction.WasPressedThisFrame();
                useSpellPressed = useSpellAction != null && useSpellAction.WasPressedThisFrame();
                transformPressed = transformAction != null && transformAction.WasPressedThisFrame();
                cameraLookUpHeld = lookUpAction.IsPressed();
                cameraLookDownHeld = lookDownAction.IsPressed();
                cameraLookLeftHeld = lookLeftAction.IsPressed();
                cameraLookRightHeld = lookRightAction.IsPressed();

                return;
            }
#endif

            float horizontal = SafeGetAxisRaw(horizontalAxis);
            float vertical = SafeGetAxisRaw(verticalAxis);
            
            moveInput = new Vector2(horizontal, vertical);

            jumpPressed = SafeGetButtonDown(jumpButton);
            jumpHeld = SafeGetButton(jumpButton);

            dashPressed = SafeGetButtonDown(dashButton);
            dashHeld = SafeGetButton(dashButton);

            attackPressed = SafeGetButtonDown(attackButton);
            attackHeld = SafeGetButton(attackButton);

            interactPressed = SafeGetButtonDown(interactButton) || Input.GetKeyDown(KeyCode.F);

            usePotionPressed = SafeGetButtonDown(usePotionButton) || Input.GetKeyDown(KeyCode.R);

            useToolPressed = SafeGetButtonDown(useToolButton) || Input.GetKeyDown(KeyCode.E);

            useSpellPressed = SafeGetButtonDown(useSpellButton) || Input.GetKeyDown(KeyCode.Q);
            if (debugSpellInput && useSpellPressed)
                Debug.Log($"[PlayerInput] UseSpellPressed = true (Q detected). inputEnabled={inputEnabled}, useSpellButton='{useSpellButton}'");

            transformPressed = SafeGetButtonDown(transformButton) || Input.GetKeyDown(transformKey);

            cameraLookUpHeld = Input.GetKey(cameraLookUpKey);
            cameraLookDownHeld = Input.GetKey(cameraLookDownKey);
            cameraLookLeftHeld = Input.GetKey(cameraLookLeftKey);
            cameraLookRightHeld = Input.GetKey(cameraLookRightKey);


        }

#if ENABLE_INPUT_SYSTEM
        private void SetupNewInputSystem()
        {
            usingNewInputSystem = true;
            if (!usingNewInputSystem) return;

            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector(mode=2)")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddBinding("<Gamepad>/dpad");

            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            dashAction = new InputAction("Dash", InputActionType.Button);
            dashAction.AddBinding("<Mouse>/rightButton");
            dashAction.AddBinding("<Gamepad>/rightShoulder");

            attackAction = new InputAction("Attack", InputActionType.Button);
            attackAction.AddBinding("<Mouse>/leftButton");
            attackAction.AddBinding("<Gamepad>/buttonWest");

            interactAction = new InputAction("Interact", InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/f");
            interactAction.AddBinding("<Gamepad>/buttonNorth");

            usePotionAction = new InputAction("UsePotion", InputActionType.Button);
            usePotionAction.AddBinding("<Keyboard>/r");
            usePotionAction.AddBinding("<Gamepad>/dpad/up");

            useToolAction = new InputAction("UseTool", InputActionType.Button);
            useToolAction.AddBinding("<Keyboard>/e");
            useToolAction.AddBinding("<Gamepad>/dpad/right");

            useSpellAction = new InputAction("UseSpell", InputActionType.Button);
            useSpellAction.AddBinding("<Keyboard>/q");
            useSpellAction.AddBinding("<Gamepad>/dpad/left");

            transformAction = new InputAction("Transform", InputActionType.Button);
            transformAction.AddBinding("<Keyboard>/tab");
            transformAction.AddBinding("<Gamepad>/leftStickPress");

            lookUpAction = new InputAction("CameraLookUp", InputActionType.Button);
            lookUpAction.AddBinding("<Keyboard>/upArrow");

            lookDownAction = new InputAction("CameraLookDown", InputActionType.Button);
            lookDownAction.AddBinding("<Keyboard>/downArrow");

            lookLeftAction = new InputAction("CameraLookLeft", InputActionType.Button);
            lookLeftAction.AddBinding("<Keyboard>/leftArrow");

            lookRightAction = new InputAction("CameraLookRight", InputActionType.Button);
            lookRightAction.AddBinding("<Keyboard>/rightArrow");

            LoadSavedBindings();
        }

        public InputAction GetInputAction(string actionName)
        {
            if (!usingNewInputSystem) return null;

            switch (actionName)
            {
                case "Move": return moveAction;
                case "Jump": return jumpAction;
                case "Dash": return dashAction;
                case "Attack": return attackAction;
                case "Interact": return interactAction;
                case "UsePotion": return usePotionAction;
                case "UseTool": return useToolAction;
                case "UseSpell": return useSpellAction;
                case "Transform": return transformAction;
                case "CameraLookUp": return lookUpAction;
                case "CameraLookDown": return lookDownAction;
                case "CameraLookLeft": return lookLeftAction;
                case "CameraLookRight": return lookRightAction;
                default: return null;
            }
        }

        public bool RebindByKeyName(BindableAction action, string keyName)
        {
            if (!usingNewInputSystem)
                return false;

            if (!KeyboardIconMapper.TryGetControlPathFromKeyName(keyName, out string controlPath))
                return false;

            return RebindByControlPath(action, controlPath);
        }

        public bool RebindByControlPath(BindableAction action, string controlPath)
        {
            if (!usingNewInputSystem)
                return false;

            if (string.IsNullOrWhiteSpace(controlPath))
                return false;

            if (!TryResolveBinding(action, out InputAction targetAction, out int bindingIndex))
                return false;

            if (bindingIndex < 0 || bindingIndex >= targetAction.bindings.Count)
                return false;

            targetAction.ApplyBindingOverride(bindingIndex, controlPath);
            SaveBinding(action, controlPath);

            string keyName = KeyboardIconMapper.GetKeyNameFromControlPath(controlPath);
            string iconTag = KeyboardIconMapper.GetSpriteTag(keyName);
            OnBindingChanged?.Invoke(action, keyName, iconTag);
            return true;
        }

        public string GetBindingKeyName(BindableAction action)
        {
            if (!usingNewInputSystem)
                return string.Empty;

            if (!TryResolveBinding(action, out InputAction targetAction, out int bindingIndex))
                return string.Empty;

            if (bindingIndex < 0 || bindingIndex >= targetAction.bindings.Count)
                return string.Empty;

            string path = targetAction.bindings[bindingIndex].effectivePath;
            if (string.IsNullOrWhiteSpace(path))
                path = targetAction.bindings[bindingIndex].path;

            return KeyboardIconMapper.GetKeyNameFromControlPath(path);
        }

        public string GetBindingIconTag(BindableAction action)
        {
            string keyName = GetBindingKeyName(action);
            if (string.IsNullOrWhiteSpace(keyName))
                return string.Empty;

            return KeyboardIconMapper.GetSpriteTag(keyName);
        }

        public void ResetBindingsToDefault()
        {
            if (!usingNewInputSystem)
                return;

            ClearBindingOverride(BindableAction.MoveUp);
            ClearBindingOverride(BindableAction.MoveDown);
            ClearBindingOverride(BindableAction.MoveLeft);
            ClearBindingOverride(BindableAction.MoveRight);
            ClearBindingOverride(BindableAction.NormalAttack);
            ClearBindingOverride(BindableAction.Jump);
            ClearBindingOverride(BindableAction.Dodge);
            ClearBindingOverride(BindableAction.Interact);
            ClearBindingOverride(BindableAction.UsePotion);
            ClearBindingOverride(BindableAction.UseTool);
            ClearBindingOverride(BindableAction.UseSpell);
            ClearBindingOverride(BindableAction.Transform);
            ClearBindingOverride(BindableAction.CameraLookUp);
            ClearBindingOverride(BindableAction.CameraLookDown);
            ClearBindingOverride(BindableAction.CameraLookLeft);
            ClearBindingOverride(BindableAction.CameraLookRight);
        }

        private bool TryResolveBinding(BindableAction action, out InputAction targetAction, out int bindingIndex)
        {
            targetAction = null;
            bindingIndex = -1;

            switch (action)
            {
                case BindableAction.MoveUp:
                    targetAction = moveAction;
                    bindingIndex = 1;
                    return true;

                case BindableAction.MoveDown:
                    targetAction = moveAction;
                    bindingIndex = 2;
                    return true;

                case BindableAction.MoveLeft:
                    targetAction = moveAction;
                    bindingIndex = 3;
                    return true;

                case BindableAction.MoveRight:
                    targetAction = moveAction;
                    bindingIndex = 4;
                    return true;

                case BindableAction.Jump:
                    targetAction = jumpAction;
                    bindingIndex = 0;
                    return true;

                case BindableAction.Dodge:
                    targetAction = dashAction;
                    bindingIndex = 0;
                    return true;

                case BindableAction.NormalAttack:
                    targetAction = attackAction;
                    bindingIndex = 0;
                    return true;

                case BindableAction.Interact:
                    targetAction = interactAction;
                    bindingIndex = 0;
                    return true;

                case BindableAction.UsePotion:
                    targetAction = usePotionAction;
                    bindingIndex = 0;
                    return true;

                case BindableAction.UseTool:
                    targetAction = useToolAction;
                    bindingIndex = 0;
                    return true;

                case BindableAction.UseSpell:
                    targetAction = useSpellAction;
                    bindingIndex = 0;
                    return true;

                case BindableAction.Transform:
                    targetAction = transformAction;
                    bindingIndex = 0;
                    return true;

                case BindableAction.CameraLookUp:
                    targetAction = lookUpAction;
                    bindingIndex = 0;
                    return true;

                case BindableAction.CameraLookDown:
                    targetAction = lookDownAction;
                    bindingIndex = 0;
                    return true;

                case BindableAction.CameraLookLeft:
                    targetAction = lookLeftAction;
                    bindingIndex = 0;
                    return true;

                case BindableAction.CameraLookRight:
                    targetAction = lookRightAction;
                    bindingIndex = 0;
                    return true;
            }

            return false;
        }

        private void SaveBinding(BindableAction action, string controlPath)
        {
            SaveBindingToSharedFile(action, controlPath);
        }

        private void LoadSavedBindings()
        {
            LoadBindingsFromSharedFile();
        }

        private void ClearBindingOverride(BindableAction action)
        {
            if (!TryResolveBinding(action, out InputAction targetAction, out int bindingIndex))
                return;

            if (bindingIndex < 0 || bindingIndex >= targetAction.bindings.Count)
                return;

            targetAction.RemoveBindingOverride(bindingIndex);
            RemoveBindingFromSharedFile(action);

            string keyName = GetBindingKeyName(action);
            string iconTag = KeyboardIconMapper.GetSpriteTag(keyName);
            OnBindingChanged?.Invoke(action, keyName, iconTag);
        }

        private string GetSharedBindingFilePath()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderPath = Path.Combine(documents, SharedBindingFolderName);
            return Path.Combine(folderPath, SharedBindingFileName);
        }

        private InputBindingProfile LoadSharedBindingProfile()
        {
            try
            {
                string filePath = GetSharedBindingFilePath();
                if (!File.Exists(filePath))
                    return null;

                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonUtility.FromJson<InputBindingProfile>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerInput] Failed to read shared input profile: {e.Message}");
                return null;
            }
        }

        private void SaveSharedBindingProfile(InputBindingProfile profile)
        {
            if (profile == null)
                return;

            try
            {
                string filePath = GetSharedBindingFilePath();
                string folderPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(folderPath) && !Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string json = JsonUtility.ToJson(profile, true);
                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerInput] Failed to save shared input profile: {e.Message}");
            }
        }

        private void SaveBindingToSharedFile(BindableAction action, string controlPath)
        {
            if (string.IsNullOrWhiteSpace(controlPath))
                return;

            InputBindingProfile profile = LoadSharedBindingProfile() ?? new InputBindingProfile();
            string actionName = action.ToString();

            InputBindingEntry found = null;
            for (int i = 0; i < profile.bindings.Count; i++)
            {
                if (string.Equals(profile.bindings[i].action, actionName, StringComparison.Ordinal))
                {
                    found = profile.bindings[i];
                    break;
                }
            }

            if (found == null)
            {
                found = new InputBindingEntry { action = actionName, controlPath = controlPath };
                profile.bindings.Add(found);
            }
            else
            {
                found.controlPath = controlPath;
            }

            SaveSharedBindingProfile(profile);
        }

        private void RemoveBindingFromSharedFile(BindableAction action)
        {
            InputBindingProfile profile = LoadSharedBindingProfile();
            if (profile == null || profile.bindings == null || profile.bindings.Count == 0)
                return;

            string actionName = action.ToString();
            profile.bindings.RemoveAll(entry => string.Equals(entry.action, actionName, StringComparison.Ordinal));
            SaveSharedBindingProfile(profile);
        }

        private bool LoadBindingsFromSharedFile()
        {
            InputBindingProfile profile = LoadSharedBindingProfile();
            if (profile == null || profile.bindings == null || profile.bindings.Count == 0)
                return false;

            bool hasAnyApplied = false;
            for (int i = 0; i < profile.bindings.Count; i++)
            {
                InputBindingEntry entry = profile.bindings[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.action) || string.IsNullOrWhiteSpace(entry.controlPath))
                    continue;

                if (!Enum.TryParse(entry.action, out BindableAction action))
                    continue;

                if (!TryResolveBinding(action, out InputAction targetAction, out int bindingIndex))
                    continue;

                if (bindingIndex < 0 || bindingIndex >= targetAction.bindings.Count)
                    continue;

                targetAction.ApplyBindingOverride(bindingIndex, entry.controlPath);
                hasAnyApplied = true;
            }

            return hasAnyApplied;
        }
#endif

        private float SafeGetAxisRaw(string axisName)
        {
            try
            {
                return Input.GetAxisRaw(axisName);
            }
            catch (UnityException)
            {
                return 0f;
            }
        }

        private bool SafeGetButtonDown(string buttonName)
        {
            try
            {
                return Input.GetButtonDown(buttonName);
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private bool SafeGetButton(string buttonName)
        {
            try
            {
                return Input.GetButton(buttonName);
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private void UpdateBuffers()
        {
            // Jump buffer
            if (jumpPressed)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }

            // Dash buffer
            if (dashPressed)
            {
                dashBufferCounter = dashBufferTime;
            }
            else
            {
                dashBufferCounter -= Time.deltaTime;
            }
        }

        public bool HasJumpBuffered()
        {
            return jumpBufferCounter > 0f;
        }

        public void ConsumeJumpInput()
        {
            jumpBufferCounter = 0f;
        }

        public bool HasDashBuffered()
        {
            return dashBufferCounter > 0f;
        }

        public void ConsumeDashInput()
        {
            dashBufferCounter = 0f;
        }

        public void EnableInput()
        {
            inputEnabled = true;
        }

        public void DisableInput()
        {
            inputEnabled = false;
            ClearRuntimeInputState();
        }

        private bool ShouldBlockByFullMap()
        {
            // Block if UI menu is open
            if (uiInput != null && uiInput.IsMenuOpen())
                return true;

            // Block if full map is open (when enabled)
            if (!blockGameplayInputWhenFullMapOpen)
                return false;

            if (mapController == null)
                mapController = FindAnyObjectByType<MapRenderTextureController>();

            return mapController != null && mapController.IsFullMapOpen;
        }

        private void ClearRuntimeInputState()
        {
            moveInput = Vector2.zero;
            jumpPressed = false;
            jumpHeld = false;
            dashPressed = false;
            dashHeld = false;
            attackPressed = false;
            attackHeld = false;
            interactPressed = false;
            usePotionPressed = false;
            useToolPressed = false;
            useSpellPressed = false;
            transformPressed = false;
            cameraLookUpHeld = false;
            cameraLookDownHeld = false;
            cameraLookLeftHeld = false;
            cameraLookRightHeld = false;
            jumpBufferCounter = 0f;
            dashBufferCounter = 0f;
        }
    }
}
