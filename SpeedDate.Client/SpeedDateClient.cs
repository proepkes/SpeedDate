using System;
using System.Threading.Tasks;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network.Interfaces;

namespace SpeedDate.Client
{
    public sealed class SpeedDateClient : IClient, ISpeedDateStartable, IDisposable
    {
        private const float MinTimeToConnect = 0.5f;
        private const float MaxTimeToConnect = 4f;

        [Inject] private ILogger _logger;
        private int _port;
        private string _serverIp;
        private float _timeToConnect = 0.5f;

        [Inject] public IClientSocket Connection { get; set; }

        public event Action Started;
        public event Action Stopped;


        public void Start()
        {
            ConnectAsync(SpeedDateConfig.Network.Address, SpeedDateConfig.Network.Port);
        }

        public void Stop()
        {
            Connection.Disconnect();
        }

        private async void ConnectAsync(string serverIp, int port)
        {
            _serverIp = serverIp;
            _port = port;

            await Task.Factory.StartNew(async () =>
            {
                Connection.Connected += Connected;
                Connection.Disconnected += Disconnected;

                while (!Connection.IsConnected)
                {
                    // If we got here, we're not connected 
                    if (Connection.IsConnecting)
                        _logger.Debug("Retrying to connect to server at: " + _serverIp + ":" + _port);
                    else
                        _logger.Debug("Connecting to server at: " + _serverIp + ":" + _port);

                    Connection.Connect(_serverIp, _port);

                    // Give a few seconds to try and connect
                    await Task.Delay(TimeSpan.FromSeconds(_timeToConnect));

                    // If we're still not connected
                    if (!Connection.IsConnected) _timeToConnect = Math.Min(_timeToConnect * 2, MaxTimeToConnect);
                }
            });
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

        public void Dispose()
        {
            Connection.Disconnect();
        }
    }
}