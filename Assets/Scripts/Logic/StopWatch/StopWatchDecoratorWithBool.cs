using System.Collections;
using UnityEngine;

namespace Logic
{
    public class StopWatchDecoratorWithBool : StopWatch
    {
        private bool hasCrossed;
        public StopWatchDecoratorWithBool(float interval, ICoroutineRunner coroutineRunner)
            : base(interval, coroutineRunner) {}
        public void SetBoolToTrue() => hasCrossed = true;
        protected override IEnumerator WatchRoutine()
        {
            while (true)
            {
                while(!hasCrossed) yield return null;
                hasCrossed = false;
                OnWaitAction();
                yield return new WaitForSeconds(Interval);
            }
        }
    }
}