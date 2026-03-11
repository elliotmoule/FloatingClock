using NetCoreAudio;

namespace FloatingClock.WPF.Utilities;

internal static class AudioPlayer
{
    private static readonly Player _player = new();

    internal static async Task Play(string soundName)
    {
        var sound = ResourceHelper.GetAllWavResourceNames()
            .FirstOrDefault(n => n.Contains(soundName, StringComparison.OrdinalIgnoreCase));

        if (sound == null)
        {
            return;
        }

        var file = await ResourceHelper.GetCachedEmbeddedResource(sound);

        await _player.Play(file);
    }
}
