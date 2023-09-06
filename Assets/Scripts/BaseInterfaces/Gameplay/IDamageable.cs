using BaseInterfaces;

namespace Logic
{
    public interface IDamageable : IComponent
    {
        bool IsDead();
        void GetDamage(int amount);
    }
}