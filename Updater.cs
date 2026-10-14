using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace InkOverlay;

/// <summary>Updates from GitHub Releases. Does nothing when not installed through Velopack (e.g. dotnet run).</summary>
public sealed class Updater
{
    public const string RepoUrl = "https://github.com/Busemann-ai/InkOverlay";

    readonly UpdateManager _manager = new(new GithubSource(RepoUrl, null, false));
    bool _busy;

    public bool IsInstalled => _manager.IsInstalled;

    public string Version => _manager.CurrentVersion?.ToString()
        ?? typeof(Updater).Assembly.GetName().Version?.ToString(3) ?? "";

    /// <summary>
    /// Checks and downloads in the background. Returns the downloaded version, or null if there is
    /// nothing new. With <paramref name="throwOnError"/> false, network errors count as "nothing new".
    /// </summary>
    public async Task<UpdateInfo?> DownloadAsync(bool throwOnError)
    {
        if (!IsInstalled || _busy) return null;
        _busy = true;
        try
        {
            var info = await _manager.CheckForUpdatesAsync();
            if (info is null) return null;
            await _manager.DownloadUpdatesAsync(info);
            return info;
        }
        catch (Exception) when (!throwOnError)
        {
            // Being offline is normal; try again next start.
            return null;
        }
        finally
        {
            _busy = false;
        }
    }

    public void ApplyAndRestart(UpdateInfo info) => _manager.ApplyUpdatesAndRestart(info.TargetFullRelease);
}
