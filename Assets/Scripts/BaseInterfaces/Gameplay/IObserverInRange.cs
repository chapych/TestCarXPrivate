using System;
using UnityEngine;

namespace Logic
{
    public interface IObserverInRange
    {
        event Action OnMonsterInRangeArea;
        void OnInRangeArea(GameObject observable);
    }
}