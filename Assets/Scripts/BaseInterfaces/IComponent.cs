using UnityEngine;

namespace BaseInterfaces
{
    public interface IComponent
    {
        Transform transform { get; }
        GameObject gameObject { get; }
    }
}