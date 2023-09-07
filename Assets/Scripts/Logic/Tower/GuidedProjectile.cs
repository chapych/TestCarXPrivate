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
			if (translation.magnitude > Speed) {
				translation = translation.normalized * Speed;
			}
			transform.Translate(translation);
		}
	}
}
