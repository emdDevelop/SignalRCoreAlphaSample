using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace WpfSampleApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILoggerProvider, ILogger
    {
        private HubConnection _connection;
        private Task _startedTask;

        public MainWindow()
        {
            InitializeComponent();

            _connection = new HubConnectionBuilder()
                        .WithUrl("http://localhost:5000/chat")
                        .WithLogger(loggerFactory =>
                        {
                            loggerFactory.AddProvider(this);   
                        })
                        .Build();

            _connection.On<string>("Send", data =>
            {
                Dispatcher.Invoke(() =>
                {
                    RichTextBoxConsole.AppendText($"Received {data}\n");
                });
            });

            _startedTask = _connection.StartAsync();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return this;
        }

        public void Dispose()
        {
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Dispatcher.Invoke(() =>
            {
                RichTextBoxConsole.AppendText(formatter(state, exception) + "\n");
            });
        }

        private async void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            await _startedTask;

            await _connection.InvokeAsync("Send", TextBoxMessage.Text);
            TextBoxMessage.Text = String.Empty;
            TextBoxMessage.Focus();
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information;
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            await _connection.DisposeAsync();

            base.OnClosing(e);
        }
    }
}
