using UnityEngine;
using UnityEngine.Events;

namespace Kabir
{
    public static class LevelEventManager
    {
        /// <summary>
        /// Event to show main menu
        /// </summary>
        public static UnityAction<bool> OnShowMainMenu;

        /// <summary>
        /// Event to update the player's current interactable
        /// </summary>
        public static UnityAction<InteractableController> OnUpdatePlayerInteractable;
    }
}
