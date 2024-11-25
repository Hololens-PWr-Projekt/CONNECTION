using ReactiveUI;
using Server.Manager;
using System;

namespace Server.ViewModel
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly WebSocketManager _webSocketManager;

        private string _messages = string.Empty;
        public string Messages
        {
            get => _messages;
            set => this.RaiseAndSetIfChanged(ref _messages, value);
        }

        public MainWindowViewModel()
        {
            _webSocketManager = new WebSocketManager(OnMessageReceived, OnMessageSent, OnError);
            _webSocketManager.StartServer();
        }

        private void OnMessageReceived(string message)
        {
            Messages += $"\nReceived: {message}";
        }

        private void OnMessageSent(string message)
        {
            Messages += $"\n{message}";
        }

        private void OnError(Exception ex)
        {
            Messages += $"\nError: {ex.Message}";
        }
    }
}
