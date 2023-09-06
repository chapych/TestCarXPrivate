using BaseInterfaces.Gameplay;
using Logic.Tower;
using Logic.Tower.Base;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace Logic
{
	public class SimpleTower : TowerBase
	{
		protected override void Shoot(IDamageable target)
		{
			var projectile = (GuidedProjectile) ProjectilePool.Get();
			projectile.transform.position = transform.position + Vector3.up * 1.5f;
		}
	}
}
