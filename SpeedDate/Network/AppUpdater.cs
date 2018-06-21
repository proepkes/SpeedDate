using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedDate.Interfaces;
using SpeedDate.Logging;

namespace SpeedDate.Network
{
    /// <summary>
    ///     This is an object which gets spawned into game once.
    ///     It's main purpose is to call update methods
    /// </summary>
    public class AppUpdater
    {
        private readonly List<IUpdatable> _addList;

        private readonly List<IUpdatable> _runnables;
        
        public long CurrentTick { get; private set; }

        public bool KeepRunning = true;
        
        public event Action<long> OnTick;

        public AppUpdater()
        {
            _runnables = new List<IUpdatable>();
            _addList = new List<IUpdatable>();


            Task.Factory.StartNew(StartTicker, TaskCreationOptions.LongRunning);
            
            Update();
        }

        private async void Update()
        {
            while (KeepRunning)
            {
                try
                {
                    lock (_addList)
                    {
                        if (_addList.Count > 0)
                        {
                            _runnables.AddRange(_addList);
                            _addList.Clear();
                        }
                    }

                    foreach (var runnable in _runnables)
                        runnable.Update();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                await Task.Delay(100);
            }
        }

        public void Add(IUpdatable updatable)
        {
            lock (_addList)
            {
                if (_addList.Contains(updatable))
                    return;

                _addList.Add(updatable);
            }
        }
        public void AddRange(IEnumerable<IUpdatable> updatables)
        {
            lock (_addList)
            {
                _addList.AddRange(updatables);
            }
        }
        
        private async void StartTicker()
        {
            CurrentTick = 0;
            
            while (KeepRunning)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                CurrentTick++;
                try
                {
                    OnTick?.Invoke(CurrentTick);
                }
                catch (Exception e)
                {
                    Logs.Error(e);
                }
            }
        }
    }
}
