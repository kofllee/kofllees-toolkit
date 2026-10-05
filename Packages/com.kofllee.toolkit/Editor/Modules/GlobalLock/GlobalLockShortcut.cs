using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Kofllee.Toolkit.GlobalLock
{
    internal static class GlobalLockShortcut
    {
        [Shortcut("kofllee's Toolkit/Toggle Window Lock", KeyCode.D, ShortcutModifiers.Alt)]
        public static void ToggleWindowLock()
        {
            GlobalLockUtility.ToggleFocusedWindowLock();
        }
    }
}