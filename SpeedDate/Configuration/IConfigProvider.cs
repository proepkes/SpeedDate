using System.Collections.Generic;

namespace SpeedDate.Configuration
{
    public interface IConfigProvider
    {
        SpeedDateConfig Result { get; }
        void Configure(IEnumerable<IConfig> configInstances);
    }
}
