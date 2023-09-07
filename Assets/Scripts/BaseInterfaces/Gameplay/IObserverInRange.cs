using UnityEngine;

namespace Logic
{
    public interface IObserverInRange
    {
        void OnInRangeArea(GameObject observable);
    }
}