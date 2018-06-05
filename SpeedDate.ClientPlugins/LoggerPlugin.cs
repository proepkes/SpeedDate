using SpeedDate.Interfaces.Network;
using SpeedDate.Logging;

namespace SpeedDate.ClientPlugins
{
    public class LoggerPlugin : SpeedDateClientPlugin
    {
        private readonly ILogger _logger;

        public LoggerPlugin(IClientSocket connection, ILogger logger) : base(connection)
        {
            _logger = logger;
            connection.Connected +=ConnectionOnConnected;
        }

        private void ConnectionOnConnected()
        {
            _logger.Info($"Connected to {Connection.ConnectionIp}:{Connection.ConnectionPort}");
        }
    }
}