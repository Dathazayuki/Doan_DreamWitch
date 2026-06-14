using UnityEngine;

namespace DreamKnight.Interfaces
{
    /// <summary>
    /// Interface cho các đối tượng có thể tương tác
    /// </summary>
    public interface IInteractable
    {
        void Interact(GameObject interactor);
        string GetInteractPrompt();
        bool CanInteract(GameObject interactor);
    }
}
