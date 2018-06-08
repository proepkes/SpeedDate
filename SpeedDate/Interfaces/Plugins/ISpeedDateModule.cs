using TinyIoC;

namespace SpeedDate.Interfaces.Plugins
{
    public interface ISpeedDateModule
    {
        void Load(TinyIoCContainer container);
    }
}