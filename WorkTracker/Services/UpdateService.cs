using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkTracker.Services
{
    public class UpdateInfo
    {
        public bool HasUpdate { get; set; }
        public string LatestVersion { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
    }

    public class UpdateService
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/Gustings/WorkTracker/releases/latest";
        private readonly HttpClient _httpClient;

        public UpdateService()
        {
            _httpClient = new HttpClient();
            // GitHub API requires a User-Agent header
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WorkTracker-Updater");
        }

        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(GitHubApiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"GitHub API returned status code: {response.StatusCode}");
                    return new UpdateInfo { HasUpdate = false };
                }

                string json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json);

                if (release == null || string.IsNullOrWhiteSpace(release.TagName))
                {
                    return new UpdateInfo { HasUpdate = false };
                }

                // Parse tag name (e.g. "v1.4.0" -> "1.4.0")
                string cleanTag = release.TagName.TrimStart('v', 'V');
                if (!Version.TryParse(cleanTag, out Version? latestVersion))
                {
                    return new UpdateInfo { HasUpdate = false };
                }

                // Get current running version
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

                if (latestVersion > currentVersion)
                {
                    // Find the installer asset (.exe)
                    string downloadUrl = string.Empty;
                    foreach (var asset in release.Assets)
                    {
                        if (asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.BrowserDownloadUrl;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(downloadUrl))
                    {
                        return new UpdateInfo
                        {
                            HasUpdate = true,
                            LatestVersion = cleanTag,
                            DownloadUrl = downloadUrl,
                            ReleaseNotes = release.Body
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking for updates: {ex.Message}");
            }

            return new UpdateInfo { HasUpdate = false };
        }

        public async Task DownloadAndInstallUpdateAsync(string downloadUrl, Action<double>? progressCallback = null)
        {
            string tempInstallerPath = Path.Combine(Path.GetTempPath(), $"WorkTrackerSetup_{Guid.NewGuid():N}.exe");

            try
            {
                // Download file and report progress
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long? totalBytes = response.Content.Headers.ContentLength;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempInstallerPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int read;

                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;

                            if (totalBytes.HasValue && progressCallback != null)
                            {
                                double progress = (double)totalRead / totalBytes.Value;
                                progressCallback(progress);
                            }
                        }
                    }
                }

                // Launch silent installation and shutdown
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempInstallerPath,
                    Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART",
                    UseShellExecute = true
                });

                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error installing update: {ex.Message}");
                // Cleanup temp file on failure
                if (File.Exists(tempInstallerPath))
                {
                    try { File.Delete(tempInstallerPath); } catch { }
                }
                throw;
            }
        }

        // Inner classes for GitHub API serialization
        private class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = string.Empty;

            [JsonPropertyName("body")]
            public string Body { get; set; } = string.Empty;

            [JsonPropertyName("assets")]
            public List<GitHubAsset> Assets { get; set; } = new();
        }

        private class GitHubAsset
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("browser_download_url")]
            public string BrowserDownloadUrl { get; set; } = string.Empty;
        }
    }
}
