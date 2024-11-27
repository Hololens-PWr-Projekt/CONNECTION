using ReactiveUI;
using System;
using System.Collections.Generic;
using Server.Service;
using Server.Model;

namespace Server.ViewModel
{
    public class MainWindowViewModel : ReactiveObject
    {
        private List<Vertex> _vertices;
        private List<int> _triangles;
        private string _messages;
        private readonly WebSocketManager _webSocketManager;
        private readonly DataProcessor _dataProcessor;

        public string Messages
        {
            get => _messages;
            set => this.RaiseAndSetIfChanged(ref _messages, value);
        }

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
            // Initialize fields
            _vertices = new List<Vertex>();
            _triangles = new List<int>();
            _messages = string.Empty;

            // Dependency initialization
            _dataProcessor = new DataProcessor(this);
            _webSocketManager = new WebSocketManager(OnMessageReceived, OnMessageSent, OnError);

            // Start WebSocket server
            try
            {
                _webSocketManager.StartServer();
            }
            catch (Exception ex)
            {
                Messages += $"Error starting WebSocket server: {ex.Message}";
            }
        }

        private void OnMessageReceived(string message)
        {
            try
            {
                // If it's JSON, process packet (need refactor)
                if (message.StartsWith('{'))
                {
                    _dataProcessor.ProcessPacket(message);

                }
                Messages += $"\nReceived: {message}";
            }
            catch (Exception ex)
            {
                Messages += $"\nError processing message: {ex.Message}";
            }
        }

        private void OnMessageSent(string message)
        {
            Messages += $"\nSent: {message}";
        }

        private void OnError(Exception ex)
        {
            Messages += $"\nError: {ex.Message}";
        }
    }
}
