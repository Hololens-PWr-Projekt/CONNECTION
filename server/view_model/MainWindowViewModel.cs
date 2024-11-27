using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using Server.Service;
using Server.Model;


namespace Server.ViewModel
{
    public class MainWindowViewModel : ReactiveObject
    {
        private List<Vertex> _vertices;
        private List<int> _triangles;
        private readonly WebSocketManager _webSocketManager;
        private readonly DataProcessor _dataProcessor;

        public ObservableCollection<string> Messages { get; } = [];
        public List<Vertex> Vertices
        {
            get => _vertices;
            set => this.RaiseAndSetIfChanged(ref _vertices, value);
        }

        public List<int> Triangles
        {
            get => _triangles;
            set => this.RaiseAndSetIfChanged(ref _triangles, value);
        }

        public MainWindowViewModel()
        {
            _vertices = [];
            _triangles = [];

            _dataProcessor = new DataProcessor(this);
            _webSocketManager = new WebSocketManager(OnMessageReceived, OnMessageSent, OnError);

            StartWebSocketServer();
        }

        private void StartWebSocketServer()
        {
            try
            {
                _webSocketManager.StartServer();
            }
            catch (Exception ex)
            {
                AppendMessage($"Error starting WebSocket server: {ex.Message}");
            }
        }

        private void OnMessageReceived(string message)
        {
            try
            {
                if (IsJson(message))
                {
                    _dataProcessor.ProcessPacket(message);

                }
                AppendMessage($"\nReceived: {message}");
            }
            catch (Exception ex)
            {
                AppendMessage($"\nError processing message: {ex.Message}");
            }
        }

        private void OnMessageSent(string message)
        {
            AppendMessage($"\nSent: {message}");
        }

        private void OnError(Exception e)
        {
            AppendMessage($"\nError: {e.Message}");
        }

        private void AppendMessage(string message)
        {
            // Use Dispatcher to ensure UI thread safety
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Messages.Add(message);
            });
        }

        private bool IsJson(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && (input.StartsWith('{') || input.StartsWith('['));
        }
    }
}
