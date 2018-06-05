using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpeedDate.Networking
{
    /// <summary>
    ///     This is an object which gets spawned into game once.
    ///     It's main purpose is to call update methods
    /// </summary>
    public class AppUpdater
    {
        private static AppUpdater _instance;

        private readonly List<IUpdatable> _addList;

        private readonly List<IUpdatable> _runnables;

        public static AppUpdater Instance => _instance ?? (_instance = new AppUpdater());

        private AppUpdater()
        {
            _runnables = new List<IUpdatable>();
            _addList = new List<IUpdatable>();

            Update();
        }

        private async void Update()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    if (_addList.Count > 0)
                    {
                        _runnables.AddRange(_addList);
                        _addList.Clear();
                    }

                    foreach (var runnable in _runnables)
                        runnable.Update();

                    await Task.Delay(100);
                }
            });
        }

        public void Add(IUpdatable updatable)
        {
            if (_addList.Contains(updatable))
                return;

            _addList.Add(updatable);
        }
    }

    public interface IUpdatable
    {
        void Update();
    }
}