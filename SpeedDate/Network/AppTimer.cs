using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedDate.Interfaces;
using SpeedDate.Logging;

namespace SpeedDate.Network
{
    public sealed class AppTimer : IUpdatable
    {
        private readonly List<Action> _mainThreadActions;

        private readonly object _mainThreadLock = new object();


        public AppTimer()
        {
            _mainThreadActions = new List<Action>();
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


        public static async void AfterSeconds(float time, Action callback)
        {
            await Task.Delay(TimeSpan.FromSeconds(time));
            callback.Invoke();
        }

        public void ExecuteOnMainThread(Action action)
        {
            OnMainThread(action);
        }

        private void OnMainThread(Action action)
        {
            lock (_mainThreadLock)
            {
                _mainThreadActions.Add(action);
            }
        }

    }
}
