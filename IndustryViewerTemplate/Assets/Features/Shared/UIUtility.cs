using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace Unity.Industry.Viewer.Shared
{
    public static class UIUtility
    {
        public const string k_ToastMessageName = "appui-toast__message";
        
        public static string GetTitleLocalizedStringForAppUI(this LocalizedString localizedString)
        {
            var tableEntry = LocalizationSettings.StringDatabase.GetTableEntry(localizedString.TableReference, localizedString.TableEntryReference);
            return $"@{tableEntry.Table.TableCollectionName}:{tableEntry.Entry.SharedEntry.Key}";
        }

        public static bool IsDisplayOn(this VisualElement element)
        {
            return element.style.display == DisplayStyle.Flex;
        }

        public static void DisplayOn(this VisualElement element)
        {
            element.style.display = DisplayStyle.Flex;
        }

        public static void DisplayOff(this VisualElement element)
        {
            element.style.display = DisplayStyle.None;
        }

        public static void SetDisplay(this VisualElement element, bool on)
        {
            if (on) element.DisplayOn(); else element.DisplayOff();
        }
    }
}
