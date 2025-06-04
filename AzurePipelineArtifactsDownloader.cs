using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;

namespace ReleaseNotesUpdater
{
    public class AzurePipelineArtifactsDownloader
    {
        private readonly string _organization;
        private readonly string _project;
        private readonly int _buildId;
        private readonly string _personalAccessToken;
        private readonly string _artifactName;
        private readonly string _downloadPath;
        private readonly string _runtimeId;
        private readonly string _logFileLocation;

        public AzurePipelineArtifactsDownloader(string organization, string project, int buildId, string personalAccessToken, string artifactName, string downloadPath, string runtimeId, string logFileLocation)
        {
            _organization = organization;
            _project = project;
            _buildId = buildId;
            _personalAccessToken = personalAccessToken;
            _artifactName = artifactName;
            _downloadPath = downloadPath;
            _runtimeId = runtimeId;
            _logFileLocation = logFileLocation;

            CreateDirectoryIfNotExists(_downloadPath); // Ensure the download directory exists
        }

        public async Task DownloadArtifactsAsync()
        {
            try
            {
                string url = $"https://dev.azure.com/{_organization}/{_project}/_apis/build/builds/{_buildId}/artifacts?api-version=6.0";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{_personalAccessToken}")));

                    HttpResponseMessage response = await client.GetAsync(url);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorMessage = $"Unable to Download Artifacts for {_runtimeId}, please verify your Personal Azure Token or Build ID is valid. HTTP {response.StatusCode}: {response.ReasonPhrase}";
                        LogError(errorMessage);
                        
                        Console.WriteLine($"Request URL: {url}");
                        Console.WriteLine($"Response Body: {responseBody}");
                        throw new HttpRequestException(errorMessage);
                    }

                    // Parse the JSON response using System.Text.Json
                    using JsonDocument doc = JsonDocument.Parse(responseBody);
                    JsonElement root = doc.RootElement;
                    JsonElement valueArray = root.GetProperty("value");
                    
                    // Find the requested artifact
                    string? downloadUrl = null;
                    foreach (JsonElement artifact in valueArray.EnumerateArray())
                    {
                        if (artifact.TryGetProperty("name", out JsonElement nameElement) && 
                            nameElement.GetString() == _artifactName &&
                            artifact.TryGetProperty("resource", out JsonElement resourceElement) &&
                            resourceElement.TryGetProperty("downloadUrl", out JsonElement urlElement))
                        {
                            downloadUrl = urlElement.GetString();
                            break;
                        }
                    }

                    if (downloadUrl == null)
                    {
                        string errorMessage = $"Unable to Download Artifacts for {_runtimeId}, please verify your Personal Azure Token or Build ID is valid. Artifact '{_artifactName}' not found in build {_buildId}.";
                        LogError(errorMessage);
                        throw new Exception(errorMessage);
                    }

                    // Download the artifact
                    HttpResponseMessage downloadResponse = await client.GetAsync(downloadUrl);
                    if (!downloadResponse.IsSuccessStatusCode)
                    {
                        string errorMessage = $"Unable to Download Artifacts for {_runtimeId}, please verify your Personal Azure Token or Build ID is valid. Failed to download artifact: HTTP {downloadResponse.StatusCode}";
                        LogError(errorMessage);
                        throw new HttpRequestException(errorMessage);
                    }

                    byte[] artifactData = await downloadResponse.Content.ReadAsByteArrayAsync();
                    string artifactZipPath = Path.Combine(_downloadPath, $"{_artifactName}.zip");
                    await File.WriteAllBytesAsync(artifactZipPath, artifactData);

                    // Unzip the artifact
                    string artifactFolderPath = Path.Combine(_downloadPath, $"{_artifactName}_{_runtimeId}");
                    ZipFile.ExtractToDirectory(artifactZipPath, artifactFolderPath);

                    // Delete the zip file after extraction
                    File.Delete(artifactZipPath);

                    Console.WriteLine($"Artifact '{_artifactName}' downloaded and unzipped successfully to '{artifactFolderPath}'.");
                }
            }
            catch (HttpRequestException)
            {
                // Re-throw HTTP-related exceptions with our custom message
                throw;
            }
            catch (JsonException ex)
            {
                string errorMessage = $"Unable to Download Artifacts for {_runtimeId}, please verify your Personal Azure Token or Build ID is valid. JSON parsing error: {ex.Message}";
                LogError(errorMessage);
                throw new Exception(errorMessage, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                string errorMessage = $"Unable to Download Artifacts for {_runtimeId}, please verify your Personal Azure Token or Build ID is valid. Access denied: {ex.Message}";
                LogError(errorMessage);
                throw new Exception(errorMessage, ex);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unable to Download Artifacts for {_runtimeId}, please verify your Personal Azure Token or Build ID is valid. Error: {ex.Message}";
                LogError(errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }

        private void LogError(string message)
        {
            try
            {
                using (StreamWriter logFile = new StreamWriter(_logFileLocation, true))
                {
                    logFile.WriteLine($"{DateTime.Now:M/d/yyyy h:mm:ss tt}: {message}");
                }
            }
            catch (Exception ex)
            {
                // If we can't write to the log file, at least output to console
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
                Console.WriteLine($"Original error message: {message}");
            }
        }

        private void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}