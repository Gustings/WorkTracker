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
        private static readonly HttpClient _httpClient;

        static UpdateService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WorkTracker-Updater");
        }

        public UpdateService()
        {
        }

        public async Task<UpdateInfo> CheckForUpdatesAsync(Action<string>? logCallback = null)
        {
            logCallback?.Invoke("Initiating update check with GitHub API...");
            try
            {
                logCallback?.Invoke($"Fetching latest release metadata from: {GitHubApiUrl}");
                var response = await _httpClient.GetAsync(GitHubApiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    logCallback?.Invoke($"GitHub API request failed. Status code: {response.StatusCode} ({(int)response.StatusCode})");
                    return new UpdateInfo { HasUpdate = false };
                }

                logCallback?.Invoke("Release metadata fetched successfully. Parsing response...");
                string json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json);

                if (release == null || string.IsNullOrWhiteSpace(release.TagName))
                {
                    logCallback?.Invoke("Failed to parse release information (no valid tag found).");
                    return new UpdateInfo { HasUpdate = false };
                }

                logCallback?.Invoke($"Latest remote version tag: {release.TagName}");
                // Parse tag name (e.g. "v1.4.0" -> "1.4.0")
                string cleanTag = release.TagName.TrimStart('v', 'V');
                if (!Version.TryParse(cleanTag, out Version? latestVersion))
                {
                    logCallback?.Invoke($"Could not parse tag '{cleanTag}' as a valid version.");
                    return new UpdateInfo { HasUpdate = false };
                }

                // Get current running version
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
                logCallback?.Invoke($"Local app version: v{currentVersion.ToString(3)}");

                if (latestVersion > currentVersion)
                {
                    logCallback?.Invoke($"Newer version available: v{cleanTag}");
                    // Find the installer asset (.exe)
                    string downloadUrl = string.Empty;
                    foreach (var asset in release.Assets)
                    {
                        if (asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.BrowserDownloadUrl;
                            logCallback?.Invoke($"Found matching asset: '{asset.Name}'");
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
                    else
                    {
                        logCallback?.Invoke("No executable installer asset (.exe) found in this release.");
                    }
                }
                else
                {
                    logCallback?.Invoke($"Application is up to date (Local: v{currentVersion.ToString(3)} >= Remote: v{cleanTag})");
                }
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"Error checking for updates: {ex.Message}");
                Debug.WriteLine($"Error checking for updates: {ex.Message}");
            }

            return new UpdateInfo { HasUpdate = false };
        }

        public async Task DownloadAndInstallUpdateAsync(string downloadUrl, Action<double>? progressCallback = null, Action<string>? logCallback = null)
        {
            string tempInstallerPath = Path.Combine(Path.GetTempPath(), $"WorkTrackerSetup_{Guid.NewGuid():N}.exe");
            logCallback?.Invoke($"Preparing temporary path for installer: {tempInstallerPath}");

            try
            {
                logCallback?.Invoke($"Initiating file download from: {downloadUrl}");
                // Download file and report progress
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long? totalBytes = response.Content.Headers.ContentLength;
                    if (totalBytes.HasValue)
                    {
                        logCallback?.Invoke($"Download stream opened. File size: {(totalBytes.Value / 1024.0 / 1024.0):F2} MB");
                    }
                    else
                    {
                        logCallback?.Invoke("Download stream opened. File size is unknown.");
                    }

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempInstallerPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int read;
                        int lastLoggedPercentage = -1;

                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;

                            if (totalBytes.HasValue)
                            {
                                double progress = (double)totalRead / totalBytes.Value;
                                progressCallback?.Invoke(progress);

                                int percentage = (int)(progress * 100);
                                if (percentage != lastLoggedPercentage && percentage % 10 == 0) // Log every 10%
                                {
                                    lastLoggedPercentage = percentage;
                                    logCallback?.Invoke($"Downloading installer... {percentage}%");
                                }
                            }
                        }
                    }
                }

                logCallback?.Invoke("Download completed successfully.");
                logCallback?.Invoke("Launching silent installer. Work Tracker will shut down shortly...");

                // Launch silent installation and shutdown
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempInstallerPath,
                    Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART",
                    UseShellExecute = true
                });

                await Task.Delay(1000); // Give the UI a moment to show the shutdown log entry
                
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (System.Windows.Application.Current is App app)
                        {
                            app.ExitApplication();
                        }
                        else
                        {
                            System.Windows.Application.Current.Shutdown();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"Error installing update: {ex.Message}");
                Debug.WriteLine($"Error installing update: {ex.Message}");
                // Cleanup temp file on failure
                if (File.Exists(tempInstallerPath))
                {
                    try 
                    { 
                        File.Delete(tempInstallerPath); 
                        logCallback?.Invoke("Cleaned up temporary installer file.");
                    } 
                    catch { }
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
