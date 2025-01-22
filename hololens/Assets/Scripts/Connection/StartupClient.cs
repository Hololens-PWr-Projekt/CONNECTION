using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hololens.Assets.Scripts.Connection.Manager;
using Hololens.Assets.Scripts.Connection.Utils;
using UnityEngine;
#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Search;
#endif

namespace Hololens.Assets.Scripts.Connection
{
    public class StartupClient : MonoBehaviour
    {
        private ChannelManager _channelManager;

        private ConcurrentQueue<string> _fileQueue = new ConcurrentQueue<string>();
        private bool _isProcessFileQueueRunning = false;

        private List<ChannelConfig> _enabledChannels;

        private void Start()
        {
            _channelManager = gameObject.AddComponent<ChannelManager>();

            // Create necessary directories in WATCHED_DATA_PATH
            CreateRequiredDirectories();

            SelectChannelsToEnable();
            _ = InitChannelsAsync(); // Start initialization asynchronously
        }

#if ENABLE_WINMD_SUPPORT
        private async void CreateRequiredDirectories()
        {
            try
            {
                StorageFolder watchedFolder = await StorageFolder.GetFolderFromPathAsync(
                    AppConfig.WATCHED_DATA_PATH
                );

                // Create "mesh" and "hands" directories if they do not exist
                await watchedFolder.CreateFolderAsync("mesh", CreationCollisionOption.OpenIfExists);
                await watchedFolder.CreateFolderAsync(
                    "hands",
                    CreationCollisionOption.OpenIfExists
                );

                Debug.Log(
                    $"Directories 'mesh' and 'hands' ensured inside: {AppConfig.WATCHED_DATA_PATH}"
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error ensuring directories: {ex.Message}");
            }
        }
#else
        private void CreateRequiredDirectories()
        {
            Debug.LogWarning(
                "CreateRequiredDirectories is not supported in this build configuration."
            );
        }
#endif

        private void SelectChannelsToEnable()
        {
            string input = "mesh,hands";
            var selectedChannels = input.Split(',').Select(c => c.Trim()).ToList();

            _enabledChannels = AppConfig
                .CHANNELS_CONFIGS.Where(c => selectedChannels.Contains(c.Name))
                .ToList();

            Debug.Log("Enabled channels: " + string.Join(", ", selectedChannels));
        }

        private async Task InitChannelsAsync()
        {
            foreach (var config in _enabledChannels)
            {
                await _channelManager.AddChannelAsync(config); // Ensure AddChannel is asynchronous in UWP
            }

            Debug.Log("All channels have been started.");
            StartFileWatcherAsync(AppConfig.WATCHED_DATA_PATH); // Start file watcher asynchronously
            StartProcessFileQueueAsync(); // Start processing file queue asynchronously

            await _channelManager.SendSignalAsync("mesh", true);
        }

        private async void StartFileWatcherAsync(string folderPath)
        {
#if ENABLE_WINMD_SUPPORT
            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);

                var queryOptions = new QueryOptions(CommonFileQuery.OrderByDate, new[] { ".obj" })
                {
                    FolderDepth = FolderDepth.Deep,
                };

                var fileQuery = folder.CreateFileQueryWithOptions(queryOptions);

                // Listen for changes
                fileQuery.ContentsChanged += async (sender, args) =>
                {
                    try
                    {
                        var queryResult = sender as StorageFileQueryResult;
                        if (queryResult != null)
                        {
                            var files = await queryResult.GetFilesAsync();
                            foreach (var file in files)
                            {
                                string directoryName = file
                                    .Path.Split('\\')
                                    .Reverse()
                                    .Skip(1)
                                    .First();
                                _fileQueue.Enqueue($"{directoryName}|{file.Path}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error during file watching: {ex.Message}");
                    }
                };

                // Trigger an initial scan to populate files
                var initialFiles = await fileQuery.GetFilesAsync();
                foreach (var file in initialFiles)
                {
                    string directoryName = file.Path.Split('\\').Reverse().Skip(1).First();
                    _fileQueue.Enqueue($"{directoryName}|{file.Path}");
                }

                Debug.Log($"FileWatcher started for folder: {folderPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start file watcher: {ex.Message}");
            }
#else
            Debug.LogWarning("File watcher is not supported in this build configuration.");
#endif
        }

        private async void StartProcessFileQueueAsync()
        {
            if (_isProcessFileQueueRunning)
                return;

            _isProcessFileQueueRunning = true;

            while (true)
            {
                if (_fileQueue.TryDequeue(out var queueItem))
                {
                    string[] splitData = queueItem.Split('|');
                    string channel = splitData[0];
                    string filePath = splitData[1];

                    byte[] fileData = null;

                    try
                    {
                        fileData = await FileProcessor.ReadFileAsync(filePath); // UWP async file reader
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error reading file: {filePath}, Error: {ex.Message}");
                        continue;
                    }

                    while (!_channelManager.IsChannelOpen(channel))
                    {
                        Debug.LogWarning($"Channel {channel} is not open. Reconnecting...");
                        await Task.Delay(5000); // Async delay instead of WaitForSeconds
                    }

                    Debug.Log($"Transmitting file: {filePath} on channel: {channel}");
                    await _channelManager.TransmitFileAsync(channel, fileData); // Ensure TransmitFile is async
                }
                else
                {
                    await Task.Delay(600); // Async delay instead of WaitForSeconds
                }
            }
        }

        private async void OnDestroy()
        {
            if (_channelManager != null)
            {
                foreach (var config in AppConfig.CHANNELS_CONFIGS)
                {
                    await _channelManager.RemoveChannelAsync(config.Name);
                }
            }

            Debug.Log("StartupClient cleanup complete.");
        }
    }
}
