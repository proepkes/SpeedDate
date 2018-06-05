using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Utils.Conversion;
using SpeedDate.Network.Utils.IO;
using SpeedDate.Packets;

namespace SpeedDate.ClientPlugins.GameServer
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