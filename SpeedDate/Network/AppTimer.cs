using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedDate.Interfaces;
using SpeedDate.Logging;

namespace SpeedDate.Network
{
    public sealed class AppTimer : IUpdatable
    {
        private static AppTimer _instance;

        private readonly List<Action> _mainThreadActions;

        private readonly object _mainThreadLock = new object();

        public bool keepRunning = true;

        public static long CurrentTick { get; private set; }

        public static AppTimer Instance => _instance ?? (_instance = new AppTimer());

        private AppTimer()
        {
            _mainThreadActions = new List<Action>();

            Task.Factory.StartNew(StartTicker, TaskCreationOptions.LongRunning);
            AppUpdater.Instance.Add(this);
        }

        public void Update()
        {
            if (_mainThreadActions.Count > 0)
                lock (_mainThreadLock)
                {
                    foreach (var actions in _mainThreadActions) actions.Invoke();

                    _mainThreadActions.Clear();
                }
        }

        public event Action<long> OnTick;

        public static async void AfterSeconds(float time, Action callback)
        {
            await Task.Delay(TimeSpan.FromSeconds(time));
            callback.Invoke();
        }

        public static void ExecuteOnMainThread(Action action)
        {
            Instance.OnMainThread(action);
        }

        private void OnMainThread(Action action)
        {
            lock (_mainThreadLock)
            {
                _mainThreadActions.Add(action);
            }
        }

        private async void StartTicker()
        {
            CurrentTick = 0;

            await Task.Run(async () =>
            {
                while (keepRunning)
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
            });
        }
    }
}