using Microsoft.Win32;

namespace ComputerClub.Client;

/// Windows siyosatlarini joriy foydalanuvchi (HKCU) darajasida o'zgartiradi.
/// Admin huquqi talab qilmaydi.
public static class RegistryLock
{
    private const string SystemPolicies = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string ExplorerPolicies = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";

    public static void Apply()
    {
        SetDword(SystemPolicies, "DisableTaskMgr", 1);
        SetDword(SystemPolicies, "DisableLockWorkstation", 1);
        SetDword(ExplorerPolicies, "NoWinKeys", 1);
        SetDword(ExplorerPolicies, "NoViewContextMenu", 1);
        SetDword(ExplorerPolicies, "NoRun", 1);
    }

    public static void Revert()
    {
        DeleteDword(SystemPolicies, "DisableTaskMgr");
        DeleteDword(SystemPolicies, "DisableLockWorkstation");
        DeleteDword(ExplorerPolicies, "NoWinKeys");
        DeleteDword(ExplorerPolicies, "NoViewContextMenu");
        DeleteDword(ExplorerPolicies, "NoRun");
    }

    private static void SetDword(string keyPath, string name, int value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
            key?.SetValue(name, value, RegistryValueKind.DWord);
        }
        catch (Exception)
        {
            // Registry yozilmasa ham bloklash oynasi ishlashda davom etadi.
        }
    }

    private static void DeleteDword(string keyPath, string name)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
        catch (Exception)
        {
        }
    }
}