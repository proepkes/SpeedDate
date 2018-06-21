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

        [Inject] private ILogger _logger;
        [Inject] private IClientSocket _connection;

        private int _port;
        private string _serverIp;
        private float _timeToConnect = 0.5f;
        private readonly SpeedDateKernel _kernel;

        public event Action Started;
        public event Action Stopped;

        public bool IsConnected => _connection.IsConnected;

        public SpeedDateClient()
        {
            _kernel = new SpeedDateKernel();
        }

        public void Start(IConfigProvider configProvider)
        {
            _kernel.Load(this, configProvider, config =>
            {
                ConnectAsync(config.Network.Address, config.Network.Port);
            });   
        }

        public void Stop()
        {
            _connection.Disconnect();
            _kernel.Stop();
        }

        private async void ConnectAsync(string serverIp, int port)
        {
            _serverIp = serverIp;
            _port = port;

            await Task.Factory.StartNew(async () =>
            {
                _connection.Connected += Connected;
                _connection.Disconnected += Disconnected;

                while (!_connection.IsConnected)
                {
                    // If we got here, we're not connected 
                    if (_connection.IsConnecting)
                        _logger.Debug("Retrying to connect to server at: " + _serverIp + ":" + _port);
                    else
                        _logger.Debug("Connecting to server at: " + _serverIp + ":" + _port);

                    _connection.Connect(_serverIp, _port);

                    // Give a few seconds to try and connect
                    await Task.Delay(TimeSpan.FromSeconds(_timeToConnect));

                    // If we're still not connected
                    if (!_connection.IsConnected) _timeToConnect = Math.Min(_timeToConnect * 2, MaxTimeToConnect);
                }
            });
        }
        public void Dispose()
        {
            _connection.Disconnect();
        }

        private void Disconnected()
        {
            _timeToConnect = MinTimeToConnect;
            Stopped?.Invoke();
        }

        private void Connected()
        {
            _timeToConnect = MinTimeToConnect;
            _logger.Info("Connected to: " + _serverIp + ":" + _port);
            Started?.Invoke();
        }

        public T GetPlugin<T>() where T : class, IPlugin
        {
            return _kernel.PluginProvider.Get<T>();
        }
    }
}