using System;
using System.IO;
using Avalonia;
using Avalonia.Platform;
using NetCoreAudio;

namespace ZeroTouch.Services
{
    public static class SoundService
    {
        private static readonly Player _player = new Player();

        public static void PlaySound(string fileName)
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), fileName);

                if (!File.Exists(tempPath))
                {
                    var uri = new Uri($"avares://ZeroTouch.UI/Assets/Announcement/{fileName}");
                    
                    using (var stream = AssetLoader.Open(uri))
                    using (var fileStream = File.Create(tempPath))
                    {
                        stream.CopyTo(fileStream);
                    }
                }

                _player.Play(tempPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing: {ex.Message}");
            }
        }
    }
}
