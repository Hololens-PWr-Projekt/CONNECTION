using System.Collections;
using System.IO;
using Hololens.Assets.Scripts.Connection.Manager;
using Hololens.Assets.Scripts.Connection.Utils;
using UnityEngine;

// TODO ChannelName based on prefix
namespace Hololens.Assets.Scripts.Connection
{
    public class StartupClient : MonoBehaviour
    {
        private ChannelManager _channelManager;

        private const string WatchFolderPath = "Assets/WatchedFiles";
        private const string ChannelName = "mesh";
        private const float GracePeriod = 5f;

        private FileSystemWatcher _fileWatcher;
        private bool _isTransmitting = false;

        private void Start()
        {
            _channelManager = gameObject.AddComponent<ChannelManager>();

            if (!Directory.Exists(WatchFolderPath))
            {
                Directory.CreateDirectory(WatchFolderPath);
                Debug.Log($"Created watch folder: {WatchFolderPath}");
            }

            StartCoroutine(InitializeAndTransmitExistingFiles());
        }

        private IEnumerator InitializeAndTransmitExistingFiles()
        {
            foreach (var config in AppConfig.CHANNELS_CONFIGS)
            {
                yield return StartCoroutine(_channelManager.AddChannel(config));
            }

            Debug.Log("All channels have been started.");

            yield return StartCoroutine(TransmitExistingFiles());

            StartFileWatcher(WatchFolderPath);
        }

        private IEnumerator TransmitExistingFiles()
        {
            while (!_channelManager.IsChannelOpen(ChannelName))
            {
                Debug.LogWarning($"Channel {ChannelName} is not open. Reconnecting...");
                yield return new WaitForSeconds(5f);
            }

            Debug.Log("Starting transmission of existing files...");
            yield return StartCoroutine(_channelManager.SendSignal(ChannelName, true));

            var existingFiles = Directory.GetFiles(WatchFolderPath, "*.obj");
            foreach (var filePath in existingFiles)
            {
                yield return StartCoroutine(TransmitFile(filePath));
            }

            yield return StartCoroutine(_channelManager.SendSignal(ChannelName, false));

            Debug.Log("Existing file transmission completed.");
        }

        private void StartFileWatcher(string folderPath)
        {
            _fileWatcher = new FileSystemWatcher(folderPath)
            {
                Filter = "*.obj",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true,
            };

            _fileWatcher.Created += (sender, args) =>
            {
                Debug.Log($"New file detected: {args.FullPath}");
                HandleNewFile(args.FullPath);
            };

            Debug.Log($"FileWatcher started for folder: {folderPath}");
        }

        private void HandleNewFile(string filePath)
        {
            if (!_isTransmitting)
            {
                _isTransmitting = true;
                StartCoroutine(TransmitFiles());
            }

            StartCoroutine(TransmitFile(filePath));
        }

        private IEnumerator TransmitFiles()
        {
            while (!_channelManager.IsChannelOpen(ChannelName))
            {
                Debug.LogWarning($"Channel {ChannelName} is not open. Reconnecting...");
                yield return new WaitForSeconds(5f);
            }

            Debug.Log("Starting dynamic file transmission...");
            yield return StartCoroutine(_channelManager.SendSignal(ChannelName, true));

            while (true)
            {
                yield return new WaitForSeconds(GracePeriod);

                if (_fileWatcher == null)
                {
                    Debug.Log("No new files detected. Ending dynamic transmission...");
                    yield return StartCoroutine(_channelManager.SendSignal(ChannelName, false));
                    _isTransmitting = false;
                    yield break;
                }
            }
        }

        private IEnumerator TransmitFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"File {filePath} does not exist.");
                yield break;
            }

            Debug.Log($"Transmitting file: {filePath}");
            yield return StartCoroutine(_channelManager.TransmitFile(ChannelName, filePath));
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
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Dispose();
                _fileWatcher = null;
                Debug.Log("FileWatcher stopped.");
            }
        }
    }
}
