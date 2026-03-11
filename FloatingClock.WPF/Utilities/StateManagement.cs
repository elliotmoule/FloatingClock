using FloatingClock.WPF.Models;
using FloatingClock.WPF.ViewModels;
using FloatingClock.WPF.Views;
using System.IO;
using System.Text.Json;

namespace FloatingClock.WPF.Utilities;

internal static class StateManagement
{
    internal const string AppDirectoryName = "FloatingClock";
    internal const string CompanyDirectoryName = "EMSTechnologies";
    internal const string AppStateFileName = "floatingclock.json";

    internal static string AppDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyDirectoryName, AppDirectoryName);
    internal static string AppStateSaveFilePath = Path.Combine(AppDirectoryPath, AppStateFileName);
    internal static string AppResourceDirectoryPath = Path.Combine(AppDirectoryPath, "Resources");

    internal static void DoSave(AppState state)
    {
        try
        {
            Directory.CreateDirectory(AppDirectoryPath);
            var appStateJson = JsonSerializer.Serialize(state);
            File.WriteAllText(AppStateSaveFilePath, appStateJson);
        }
        catch (Exception ex)
        {
            var message = new MessageView(new MessageViewModel("Failed to Save", $"Exception:\r\n{ex.Message}", false));
            message.ShowMessage();
        }
    }

    internal static AppState DoLoad()
    {
        try
        {
            var appStateJson = File.ReadAllText(AppStateSaveFilePath);
            var state = JsonSerializer.Deserialize<AppState>(appStateJson);

            return state ?? new AppState();
        }
        catch (Exception ex)
        {
            var message = new MessageView(new MessageViewModel("Failed to Load", $"Exception:\r\n{ex.Message}", false));
            message.ShowMessage();
        }

        return new AppState();
    }
}
