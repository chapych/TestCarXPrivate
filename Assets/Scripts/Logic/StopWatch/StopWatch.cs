using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Logic
{
    public class StopWatch
    {
        protected readonly float Interval;
        private readonly ICoroutineRunner coroutineRunner;
        private Coroutine coroutine;

        public event Action OnTime;

        public StopWatch(float interval, ICoroutineRunner coroutineRunner)
        {
            Interval = interval;
            this.coroutineRunner = coroutineRunner;
        }

        public void Run()
        {
            coroutine = coroutineRunner.StartCoroutine(WatchRoutine());
        }

        public void Stop() => coroutineRunner.StopCoroutine(coroutine);

        protected virtual IEnumerator WatchRoutine()
        {
            while (true)
            {
                OnWaitAction();
                yield return new WaitForSeconds(Interval);
            }
        }
        protected void OnWaitAction() => OnTime?.Invoke();
    }
}