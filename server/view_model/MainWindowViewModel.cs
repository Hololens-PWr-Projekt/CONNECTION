using ReactiveUI;
using Server.Model;
using Server.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using System.Threading.Tasks;
using System.Reactive;

namespace Server.ViewModel
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly WebSocketManager webSocketManager;
        private readonly DataProcessor dataProcessor;

        private List<Vertex> vertices;
        private List<int> triangles;
        private bool hideData;

        public ObservableCollection<string> Messages { get; } = [];

        public List<Vertex> Vertices
        {
            get => vertices;
            set => this.RaiseAndSetIfChanged(ref vertices, value);
        }

        public List<int> Triangles
        {
            get => triangles;
            set => this.RaiseAndSetIfChanged(ref triangles, value);
        }

        public bool HideData
        {
            get => hideData;
            set => this.RaiseAndSetIfChanged(ref hideData, value);
        }

        public string HideDataStatus => $"Hide Data: {HideData}";

        public MainWindowViewModel()
        {
            vertices = [];
            triangles = [];

            hideData = true;

            dataProcessor = new DataProcessor(this);
            webSocketManager = new WebSocketManager(OnMessageReceived, OnMessageSent, OnError);

            StartWebSocketServer();
        }

        private void StartWebSocketServer()
        {
            try
            {
                webSocketManager.StartServer();
            }
            catch (Exception e)
            {
                AppendMessage($"Error starting WebSocket server: {e.Message}");
            }
        }

        private void OnMessageReceived(string message)
        {
            try
            {
                if (message.StartsWith('{'))
                {
                    dataProcessor.ProcessPacket(message);
                }
                AppendMessage($"\nReceived: {message}");
            }
            catch (Exception e)
            {
                AppendMessage($"\nError processing message: {e.Message}");
            }
        }

        private void AppendMessage(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() => Messages.Add(message));
        }

        private void OnMessageSent(string message) => AppendMessage($"\nSent: {message}");

        private void OnError(Exception e) => AppendMessage($"\nError: {e.Message}");

    }
}
