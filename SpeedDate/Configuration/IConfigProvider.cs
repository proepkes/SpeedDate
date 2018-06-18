using System.Collections.Generic;

namespace SpeedDate.Configuration
{
    public interface IConfigProvider
    {
        SpeedDateConfig Create(IEnumerable<IConfig> configInstances);
    }
}