using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Plugin.Interfaces;
using SpeedDate.Server;

namespace SpeedDate.ServerPlugins.Echo
{
    public class EchoPlugin : SpeedDateServerPlugin
    {
        public override void Loaded()
        {
            Server.SetHandler(OpCodes.Echo, HandleEcho);
        }

        private void HandleEcho(IIncommingMessage message)
        {
            message.Respond(message.AsString(), ResponseStatus.Success);
        }
    }
}
