using System;
using System.Threading.Tasks;
using SpeedDate.Configuration;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network.Interfaces;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.Client
{
    public sealed class SpeedDateClient : IClient, ISpeedDateStartable, IDisposable
    {
        private const float MinTimeToConnect = 0.5f;
        private const float MaxTimeToConnect = 4f;
        private bool _isStarted;

        [Inject] private ILogger _logger;
        [Inject] private IClientSocket _connection;

        private float _timeToConnect = 0.5f;
        private readonly SpeedDateKernel _kernel;

        public event Action Started;
        public event Action Stopped;

        public bool IsConnected => _connection != null && _connection.IsConnected;

        public SpeedDateClient()
        {
            _kernel = new SpeedDateKernel();
        }

        public void Start(IConfigProvider configProvider)
        {
            _isStarted = true;
            _kernel.Load(this, configProvider, config =>
            {
                _connection.Connected -= Connected;
                _connection.Disconnected -= Disconnected;

                _connection.Connected += Connected;
                _connection.Disconnected += Disconnected;

                ConnectAsync(config.Network.Address, config.Network.Port);
            });
        }

        private async void ConnectAsync(string serverIp, int port)
        {
            
            await Task.Factory.StartNew(async () =>
            {
                while (!_connection.IsConnected && _isStarted)
                {
                    // If we got here, we're not connected 
                    _logger.Debug(_connection.IsConnecting
                        ? $"Retrying to connect to server at: {serverIp}:{port}"
                        : $"Connecting to server at: {serverIp}:{port}");

                    _connection.Connect(serverIp, port);

                    // Give a few seconds to try and connect
                    await Task.Delay(TimeSpan.FromSeconds(_timeToConnect));

                    // If we're still not connected
                    if (!_connection.IsConnected) 
                        _timeToConnect = Math.Min(_timeToConnect * 2, MaxTimeToConnect);
                }
            }, TaskCreationOptions.LongRunning);
        }
        
        public void Stop()
        {
            if(IsConnected)
                _connection.Disconnect();
            else
                Disconnected();
        }
        
        public void Dispose()
        {
            Stop();
        }

        private void Disconnected()
        {
            _timeToConnect = MinTimeToConnect;
            
            _isStarted = false;
            _kernel.Stop();
            
            Stopped?.Invoke();
        }

        private void Connected()
        {
            _timeToConnect = MinTimeToConnect;
            _logger.Info("Connected");
            Started?.Invoke();
        }

        public T GetPlugin<T>() where T : class, IPlugin
        {
            return _kernel.PluginProvider.Get<T>();
        }
    }
}
