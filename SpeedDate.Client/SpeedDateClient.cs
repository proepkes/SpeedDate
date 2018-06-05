using System;
using System.Threading.Tasks;
using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;
using SpeedDate.Logging;

namespace SpeedDate.Client
{
    public class SpeedDateClient : IClient, ISpeedDateListener, IDisposable
    {
        private const float MinTimeToConnect = 0.5f;
        private const float MaxTimeToConnect = 4f;

        private readonly ILogger _logger;
        private int _port;
        private string _serverIp;
        private float _timeToConnect = 0.5f;

        public SpeedDateClient(IClientSocket clientSocket, ILogger logger)
        {
            _logger = logger;
            Connection = clientSocket;
        }

        public IClientSocket Connection { get; }

        public void Dispose()
        {
            Connection?.Disconnect();
        }


        public event Action Started;
        public event Action Stopped;

        public void OnSpeedDateStarted()
        {
            ConnectAsync(SpeedDateConfig.Network.IP, SpeedDateConfig.Network.Port);
            Started?.Invoke();
        }

        public void OnSpeedDateStopped()
        {
            Stopped?.Invoke();
        }

        public async void ConnectAsync(string serverIp, int port)
        {
            _serverIp = serverIp;
            _port = port;

            await Task.Factory.StartNew(StartConnection);
        }

        private async void StartConnection()
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
        }

        private void Disconnected()
        {
            _timeToConnect = MinTimeToConnect;
        }

        private void Connected()
        {
            _timeToConnect = MinTimeToConnect;
            _logger.Info("Connected to: " + _serverIp + ":" + _port);
        }
    }
}