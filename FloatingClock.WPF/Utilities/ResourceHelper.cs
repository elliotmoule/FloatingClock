using System.IO;
using System.Reflection;

namespace FloatingClock.WPF.Utilities;

internal static class ResourceHelper
{
    private static readonly Lazy<Assembly> _currentAssembly = new(Assembly.GetExecutingAssembly());

    internal static string[] GetAllResourceNames() => _currentAssembly.Value.GetManifestResourceNames();

    internal static string[] GetAllWavResourceNames()
    {
        var resources = GetAllResourceNames();
        var sounds = resources.Where(f => f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));

        return [.. sounds];
    }

    internal static Stream GetManifestResourceStream(string? resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
        {
            throw new Exception($"Provided resource '{resourceName}' wasn't found.");
        }

        var resource = GetAllResourceNames().FirstOrDefault(r => r.Contains(resourceName)) ?? throw new Exception($"Provided resource '{resourceName}' wasn't found.");
        using var stream = _currentAssembly.Value.GetManifestResourceStream(resource);
        return stream ?? throw new Exception($"Provided resource '{resourceName}' wasn't found.");
    }

    internal static async Task<string> GetCachedEmbeddedResource(string resourceName)
    {
        try
        {
            Directory.CreateDirectory(StateManagement.AppResourceDirectoryPath);
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new Exception($"Provided resource '{resourceName}' wasn't found.");
            }

            var filePath = Path.Combine(StateManagement.AppResourceDirectoryPath, resourceName);
            if (File.Exists(filePath))
            {
                return filePath;
            }

            var resource = GetAllResourceNames().FirstOrDefault(r => r.Contains(resourceName)) ?? throw new Exception($"Provided resource '{resourceName}' wasn't found.");

            using var stream = _currentAssembly.Value.GetManifestResourceStream(resource) ?? throw new Exception($"Provided resource '{resourceName}' wasn't found.");
            
            
            using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream);

            return filePath;
        }
        catch
        {
            throw;
        }
    }
}
