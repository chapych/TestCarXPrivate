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
			// GameObject projectile =
			// 	Instantiate(m_projectilePrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
			// var projectileBeh = projectile.GetComponent<GuidedProjectile>();
			var projectile = ProjectilePool.Get() as GuidedProjectile;
			projectile.transform.position = transform.position + Vector3.up * 1.5f;
			projectile.Construct(Speed, Damage, target);
		}
	}
}
