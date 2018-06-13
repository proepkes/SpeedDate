using SpeedDate.Network.Interfaces;

namespace SpeedDate.Network
{
    /// <summary>
    ///     Generic packet handler
    /// </summary>
    public class PacketHandler : IPacketHandler
    {
        private readonly IncommingMessageHandler _handler;
        private readonly ushort _opCode;

        public PacketHandler(ushort opCode, IncommingMessageHandler handler)
        {
            _opCode = opCode;
            _handler = handler;
        }

        public ushort OpCode => _opCode;

        public void Handle(IIncommingMessage message)
        {

            _handler.Invoke(message);
        }
    }
}