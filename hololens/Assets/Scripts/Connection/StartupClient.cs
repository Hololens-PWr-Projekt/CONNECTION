using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hololens.Assets.Scripts.Connection.Manager;
using Hololens.Assets.Scripts.Connection.Utils;
using UnityEngine;

namespace Hololens.Assets.Scripts.Connection
{
    public class StartupClient : MonoBehaviour
    {
        private ChannelManager _channelManager;

        private FileSystemWatcher _fileWatcher;
        private List<ChannelConfig> _enabledChannels;
        private ConcurrentQueue<string> _fileQueue = new ConcurrentQueue<string>();
        private bool _isProcessFileQueueRunning = false;

        private float _timeSinceLastFile = 0f;

        private void Start()
        {
            _channelManager = gameObject.AddComponent<ChannelManager>();

            if (!Directory.Exists(AppConfig.WATCHED_DATA_PATH))
            {
                Directory.CreateDirectory(AppConfig.WATCHED_DATA_PATH);
                Debug.Log($"Created watch folder: {AppConfig.WATCHED_DATA_PATH}");
            }

            SelectChannelsToEnable();
            StartCoroutine(InitChannels());
        }

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
            StartFileWatcher(AppConfig.WATCHED_DATA_PATH);

            // Start the global file processing coroutine
            StartGlobalProcessFileQueue();

            yield return StartCoroutine(_channelManager.SendSignal("mesh", true));
        }

        private void StartFileWatcher(string folderPath)
        {
            _fileWatcher = new FileSystemWatcher(folderPath)
            {
                Filter = "*.obj",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                IncludeSubdirectories = true, // Watch subdirectories for mesh and hands
                EnableRaisingEvents = true,
            };

            _fileWatcher.Created += OnFileCreated;
            Debug.Log(
                $"FileWatcher started for folder: {folderPath} with IncludeSubdirectories: {_fileWatcher.IncludeSubdirectories}"
            );
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            string directoryName = new DirectoryInfo(Path.GetDirectoryName(e.FullPath)).Name;
            _fileQueue.Enqueue($"{directoryName}|{e.FullPath}");
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
                    _timeSinceLastFile = 0f; // Reset the grace period timer

                    string[] splitData = queueItem.Split('|');
                    string channel = splitData[0];
                    string filePath = splitData[1];

                    byte[] fileData = File.ReadAllBytes(filePath);

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
                    // TDOO FOR TESTING PURPOSES
                    // _timeSinceLastFile += 0.6f;

                    // if (_timeSinceLastFile >= AppConfig.GRACE_PERIOD)
                    // {
                    //     Debug.Log($"Grace period elapsed. Sending signal on channel: mesh");
                    //     yield return new WaitForSeconds(4f); // wait for trnsmission
                    //     yield return StartCoroutine(_channelManager.SendSignal("mesh", false));

                    //     _timeSinceLastFile = 0f; // Reset timer after sending signal
                    // }

                    yield return new WaitForSeconds(0.6f);
                }
            }
        }

        private void DeleteAllObjFiles()
        {
            Debug.Log("Deleting all .obj and .meta files in watched folder...");
            var filesToDelete = Directory
                .GetFiles(AppConfig.WATCHED_DATA_PATH + "/mesh", "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith(".obj") || file.EndsWith(".meta"));
            foreach (var filePath in filesToDelete)
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException ex)
                {
                    Debug.LogError($"Failed to delete {filePath}: {ex.Message}");
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

            if (_fileWatcher != null)
            {
                _fileWatcher.Created -= OnFileCreated; // Remove the event handler
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Dispose();
                _fileWatcher = null;
                Debug.Log("FileWatcher stopped.");
            }

            DeleteAllObjFiles();
        }
    }
}
