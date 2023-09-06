using BaseInterfaces.Gameplay;
using Logic.Tower.Base;
using UnityEngine;

namespace Logic.Tower
{
	public class GuidedProjectile : ProjectileBase
	{
		protected override void Update ()
		{
			base.Update();
			Vector3 translation = Target.transform.position - transform.position;
			if (translation.magnitude > m_speed) {
				translation = translation.normalized * m_speed;
			}
			transform.Translate(translation);
		}
	}
}
