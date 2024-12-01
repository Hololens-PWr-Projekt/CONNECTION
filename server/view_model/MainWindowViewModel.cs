using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using Server.Model;
using Server.Service;

namespace Server.ViewModel
{
    // todo reactive ui causes lag because of so much data send
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly WebSocketManager webSocketManager;
        private readonly DataProcessor dataProcessor;

        private List<Vertex> vertices;
        private List<int> triangles;

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

        public MainWindowViewModel()
        {
            vertices = [];
            triangles = [];

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
            // Use Dispatcher to ensure UI thread safety
            Dispatcher.UIThread.InvokeAsync(() => Messages.Add(message));
        }

        private static bool IsValidJson(string input)
        {
            try
            {
                var obj = JToken.Parse(input);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void OnMessageSent(string message) => AppendMessage($"\nSent: {message}");

        private void OnError(Exception e) => AppendMessage($"\nError: {e.Message}");
    }
}
