using UnityEngine;
using DreamKnight.Systems.Shop;
using DreamKnight.Systems.Facility;

namespace Project.UI
{
    /// <summary>
    /// UI-specific input handler. Should be attached to the Canvas or a UI input GameObject.
    /// It listens for UI keys (Escape) and toggles the `MenuMain2Controller`.
    /// This component is separate from `PlayerInput` and only handles UI-level keys.
    /// </summary>
    public class UIInput : MonoBehaviour
    {
        [Tooltip("Reference to the MenuMain2Controller to toggle with Esc")]
        public MenuMain2Controller menuController;

        [Tooltip("If true, this component will swallow the Esc key when a UI element is focused.")]
        public bool swallowWhenUiFocused = true;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (ShopCanvasController.Instance != null &&
                    (ShopCanvasController.Instance.IsShopOpen || ShopCanvasController.Instance.ConsumedEscThisFrame))
                    return;

                if (FacilityCanvasController.Instance != null &&
                    (FacilityCanvasController.Instance.IsFacilityOpen || FacilityCanvasController.Instance.ConsumedEscThisFrame))
                    return;

                if (menuController != null)
                {
                    menuController.ToggleMenuRoot();
                }
            }
        }

        // Exposed helper so PlayerInput or other systems can ask whether UI input is currently open
        public bool IsMenuOpen()
        {
            return menuController != null && menuController.IsOpen;
        }
    }
}
