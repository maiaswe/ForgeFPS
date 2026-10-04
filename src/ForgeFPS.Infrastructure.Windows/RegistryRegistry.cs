namespace ForgeFPS.Infrastructure.Windows;

using Domain;

/// <summary>
/// Registry abstraction wrapping Microsoft.Win32.RegistryKey.
/// </summary>
public class RegistryRegistry : IRegistryRegistry
{
    public object? GetValue(string keyPath, string valueName)
    {
        using var key = OpenKey(keyPath, writable: false);
        return key?.GetValue(valueName);
    }

    public void SetValue(string keyPath, string valueName, object value)
    {
        using var key = OpenKey(keyPath, writable: true);
        key?.SetValue(valueName, value);
    }

    public void DeleteValue(string keyPath, string valueName)
    {
        using var key = OpenKey(keyPath, writable: true);
        key?.DeleteValue(valueName, false);
    }

    public bool Exists(string keyPath, string? valueName = null)
    {
        try
        {
            using var key = OpenKey(keyPath, writable: false);
            if (key is null) return false;
            if (string.IsNullOrEmpty(valueName)) return true;
            return key.GetValue(valueName) is not null;
        }
        catch
        {
            return false;
        }
    }

    private Microsoft.Win32.RegistryKey OpenKey(string keyPath, bool writable)
    {
        var parts = keyPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) throw new ArgumentException("Invalid key path.", nameof(keyPath));

        var rootName = parts[0];
        var rest = string.Join("\\", parts.Skip(1));

        Microsoft.Win32.RegistryKey root;
        switch (rootName.ToUpperInvariant())
        {
            case "HKEY_LOCAL_MACHINE":
            case "HKLM":
                root = Microsoft.Win32.Registry.LocalMachine;
                break;
            case "HKEY_CURRENT_USER":
            case "HKCU":
                root = Microsoft.Win32.Registry.CurrentUser;
                break;
            default:
                throw new ArgumentException($"Unknown registry root: {rootName}", nameof(keyPath));
        }

        return root.OpenSubKey(rest, writable);
    }
}