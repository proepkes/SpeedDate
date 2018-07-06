using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedDate.Interfaces;
using SpeedDate.Logging;

namespace SpeedDate.Network
{
    public class AppUpdater
    {

        private static readonly Lazy<AppUpdater> LazyInstance = new Lazy<AppUpdater>(() => new AppUpdater());

        private readonly List<IUpdatable> _addList;
        private readonly List<IUpdatable> _removeList;

        private readonly List<IUpdatable> _runnables;

        public bool KeepRunning = true;
        public static AppUpdater Instance => LazyInstance.Value;

        private AppUpdater()
        {
            _runnables = new List<IUpdatable>();
            _addList = new List<IUpdatable>();
            _removeList = new List<IUpdatable>();

            StartTicker();
            StartUpdating();
        }
        
        public long CurrentTick { get; private set; }

        public event Action<long> OnTick;

        private async void StartUpdating()
        {
            await Task.Factory.StartNew(async () =>
            {
                while (KeepRunning || _runnables.Count > 0)
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

                        lock (_removeList)
                        {
                            if (_removeList.Count > 0)
                            {
                                _runnables.RemoveAll(updatable => _removeList.Contains(updatable));
                                _removeList.Clear();
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
            }, TaskCreationOptions.LongRunning);
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

        public void Remove(IUpdatable updatable)
        {
            lock (_removeList)
            {
                if (_removeList.Contains(updatable))
                    return;

                _removeList.Add(updatable);
            }
        }

        private async void StartTicker()
        {
            CurrentTick = 0;
            await Task.Factory.StartNew(async () =>
            {
                while (KeepRunning|| _runnables.Count > 0)
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
            }, TaskCreationOptions.LongRunning);
        }
    }
}
