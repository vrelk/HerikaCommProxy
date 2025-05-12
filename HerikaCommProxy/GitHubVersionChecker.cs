using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace HerikaCommProxy
{
    public class GitHubVersionChecker
    {
        private readonly HttpClient _httpClient;
        private const string GitHubApiBaseUrl = "https://api.github.com";
        private const string UserAgent = "HerikaCommProxy-VersionChecker";

        public GitHubVersionChecker()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            // GitHub API requires a user agent header
        }

        /// <summary>
        /// Checks if a newer version is available on GitHub
        /// </summary>
        /// <param name="owner">Repository owner</param>
        /// <param name="repo">Repository name</param>
        /// <param name="currentVersion">Current version in format 1.0.2+8a5fe5567f</param>
        /// <returns>A tuple with update availability and latest version if available</returns>
        public async Task<(bool UpdateAvailable, string LatestVersion)> CheckForUpdateAsync(
            string owner,
            string repo,
            string currentVersion)
        {
            try
            {
                // Extract the semantic version part (before the +)
                string currentSemanticVersion = ExtractSemanticVersion(currentVersion);
                if (string.IsNullOrEmpty(currentSemanticVersion))
                {
                    throw new ArgumentException("Could not parse current version format", nameof(currentVersion));
                }

                // Get the latest release from GitHub API
                string requestUrl = $"{GitHubApiBaseUrl}/repos/{owner}/{repo}/releases/latest";
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                string jsonContent = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(jsonContent);
                var root = jsonDoc.RootElement;

                // Get tag_name from the JSON response
                string latestTag = root.GetProperty("tag_name").GetString();

                // Remove 'v' prefix if exists
                latestTag = latestTag?.TrimStart('v');

                if (string.IsNullOrEmpty(latestTag))
                {
                    throw new Exception("Could not retrieve the latest version tag from GitHub");
                }

                // Compare versions
                bool updateAvailable = IsNewerVersion(currentSemanticVersion, latestTag);

                return (updateAvailable, latestTag);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error checking GitHub for updates: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Extracts the semantic version part from a version string in format "1.0.2+8a5fe5567f"
        /// </summary>
        private string ExtractSemanticVersion(string version)
        {
            // Match the part before the '+'
            var match = Regex.Match(version, @"^(\d+\.\d+\.\d+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Compares two semantic version strings to determine if the second is newer than the first
        /// </summary>
        private bool IsNewerVersion(string currentVersion, string latestVersion)
        {
            // Parse the version strings into their components
            var currentParts = currentVersion.Split('.').Select(int.Parse).ToArray();
            var latestParts = latestVersion.Split('.').Select(int.Parse).ToArray();

            // Compare major version
            if (latestParts[0] > currentParts[0]) return true;
            if (latestParts[0] < currentParts[0]) return false;

            // Major versions are equal, compare minor version
            if (latestParts[1] > currentParts[1]) return true;
            if (latestParts[1] < currentParts[1]) return false;

            // Major and minor versions are equal, compare patch version
            return latestParts[2] > currentParts[2];
        }
    }
}
