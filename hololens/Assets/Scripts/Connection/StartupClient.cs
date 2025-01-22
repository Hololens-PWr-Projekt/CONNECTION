using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Hololens.Assets.Scripts.Connection.Manager;
using Hololens.Assets.Scripts.Connection.Utils;
using UnityEngine;
#if WINDOWS_UWP
using Windows.Storage;
using Windows.Foundation;
using System.Threading.Tasks;
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

            CreateRequiredDirectories();

            SelectChannelsToEnable();
            StartCoroutine(InitChannels());
        }

        private void CreateRequiredDirectories()
        {
#if WINDOWS_UWP
            CreateRequiredDirectoriesUWP();
#else
            CreateRequiredDirectoriesUnity();
#endif
        }

#if WINDOWS_UWP
        private async void CreateRequiredDirectoriesUWP()
        {
            StorageFolder watchedFolder;

            try
            {
                // Get or create the watched folder
                watchedFolder = await StorageFolder.GetFolderFromPathAsync(
                    AppConfig.WATCHED_DATA_PATH
                );
            }
            catch (Exception)
            {
                // If the folder does not exist, create it
                watchedFolder = await StorageFolder
                    .GetFolderFromPathAsync(
                        System.IO.Path.GetDirectoryName(AppConfig.WATCHED_DATA_PATH)
                    )
                    .AsTask();

                watchedFolder = await watchedFolder.CreateFolderAsync(
                    System.IO.Path.GetFileName(AppConfig.WATCHED_DATA_PATH),
                    CreationCollisionOption.OpenIfExists
                );
                Debug.Log($"Created watch folder: {AppConfig.WATCHED_DATA_PATH}");
            }

            // Create "mesh" and "hands" directories if they do not exist
            await watchedFolder.CreateFolderAsync("mesh", CreationCollisionOption.OpenIfExists);
            await watchedFolder.CreateFolderAsync("hands", CreationCollisionOption.OpenIfExists);

            Debug.Log(
                $"Directories 'mesh' and 'hands' ensured inside: {AppConfig.WATCHED_DATA_PATH}"
            );
        }
#else
        private void CreateRequiredDirectoriesUnity()
        {
            string meshPath = System.IO.Path.Combine(AppConfig.WATCHED_DATA_PATH, "mesh");
            string handsPath = System.IO.Path.Combine(AppConfig.WATCHED_DATA_PATH, "hands");

            if (!System.IO.Directory.Exists(meshPath))
            {
                System.IO.Directory.CreateDirectory(meshPath);
                Debug.Log($"Created directory: {meshPath}");
            }

            if (!System.IO.Directory.Exists(handsPath))
            {
                System.IO.Directory.CreateDirectory(handsPath);
                Debug.Log($"Created directory: {handsPath}");
            }
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

        private IEnumerator InitChannels()
        {
            foreach (var config in _enabledChannels)
            {
                yield return StartCoroutine(_channelManager.AddChannel(config));
            }

            Debug.Log("All channels have been started.");
            StartGlobalProcessFileQueue();

            yield return StartCoroutine(_channelManager.SendSignal("mesh", true));
        }

        private void StartGlobalProcessFileQueue()
        {
            if (!_isProcessFileQueueRunning)
            {
                _isProcessFileQueueRunning = true;
                StartCoroutine(ProcessFileQueue());
            }
        }

        private IEnumerator ProcessFileQueue()
        {
            while (true)
            {
                if (_fileQueue.TryDequeue(out var queueItem))
                {
                    string[] splitData = queueItem.Split('|');
                    string channel = splitData[0];
                    string filePath = splitData[1];

                    byte[] fileData = null;

#if WINDOWS_UWP
                    var storageFile = await StorageFile.GetFileFromPathAsync(filePath);
                    using (var stream = await storageFile.OpenReadAsync())
                    {
                        fileData = new byte[stream.Size];
                        using (var reader = new DataReader(stream))
                        {
                            await reader.LoadAsync((uint)stream.Size);
                            reader.ReadBytes(fileData);
                        }
                    }
#else
                    fileData = System.IO.File.ReadAllBytes(filePath);
#endif

                    while (!_channelManager.IsChannelOpen(channel))
                    {
                        Debug.LogWarning($"Channel {channel} is not open. Reconnecting...");
                        yield return new WaitForSeconds(5f);
                    }

                    Debug.Log($"Transmitting file: {filePath} on channel: {channel}");
                    yield return StartCoroutine(_channelManager.TransmitFile(channel, fileData));
                }
                else
                {
                    yield return new WaitForSeconds(0.6f);
                }
            }
        }

        private void OnDestroy()
        {
            if (_channelManager != null)
            {
                foreach (var config in AppConfig.CHANNELS_CONFIGS)
                {
                    _channelManager.RemoveChannel(config.Name);
                }
            }
        }
    }
}
