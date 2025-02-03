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
    }
}
