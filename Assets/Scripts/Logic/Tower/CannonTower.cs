using System.Collections;
using Extensions;
using Logic.Math;
using Logic.Tower.Base;
using UnityEngine;

namespace Logic.Tower
{
	public class CannonTower : TowerBase
	{
		[SerializeField] private Transform m_shootPoint;
		[SerializeField] private Transform Hub;
		[SerializeField] private Transform CanonItself;

		protected override void Shoot(IDamageable target)
		{
			var aim = CalculateAim(target.transform);
			if(aim == null) return;
			Vector3 relativePosition = (Vector3) aim - m_shootPoint.position;
			Quaternion lookRotation = Quaternion.LookRotation(relativePosition);

			StartCoroutine(PrepareAndShoot(lookRotation, target));
		}

		private Vector3? CalculateAim(Transform target)
		{
			Vector3 position = target.position;
			Vector3 targetRelativePosition = position - m_shootPoint.position;
			Vector3 targetVelocity = target.GetComponent<Rigidbody>().velocity;

			var solutions = QuadraticSolver.SolveWithParameters(targetVelocity.DoubleMagnitude() - Speed * Speed,
				2 * Vector3.Dot(targetVelocity, targetRelativePosition),
				targetRelativePosition.DoubleMagnitude());

			if (!NonNegativeMin.TryNonNegativeMin(solutions, out float time)) return null;

			return position + targetVelocity * time;
		}

		private IEnumerator PrepareAndShoot(Quaternion finish, IDamageable target)
		{
			yield return StartCoroutine(TurnTo(Hub, Hub.rotation, new Quaternion(0, finish.y, 0, finish.w)));
			yield return StartCoroutine(TurnTo(CanonItself, CanonItself.rotation, new Quaternion(finish.x, finish.y, 0, finish.w)));

			ExtractProjectile(target);
		}

		private IEnumerator TurnTo(Transform turningObject, Quaternion startRotation, Quaternion finish)
		{
			float rotationProgress = 0;

			while (rotationProgress < 1)
			{
				rotationProgress += Time.deltaTime * 15;
				turningObject.transform.rotation = Quaternion.Lerp(startRotation, finish, rotationProgress);
				yield return null;
			}
		}

		private void ExtractProjectile(IDamageable target)
		{
			var projectile = (CannonProjectile) ProjectilePool.Get();
			projectile.Target = target;

			Transform projectileTransform = projectile.transform;
			projectileTransform.position = m_shootPoint.position;
			projectileTransform.rotation = m_shootPoint.rotation;
			projectile.SetMovementDirection(CanonItself.forward);
		}
	}
}
