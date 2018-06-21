using System.Collections.Generic;

namespace SpeedDate.Configuration
{
    public interface IConfigProvider
    {
        SpeedDateConfig Configure(IEnumerable<IConfig> configInstances);
    }
}