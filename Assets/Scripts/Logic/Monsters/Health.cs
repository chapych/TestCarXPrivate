using System;
using Logic.PoolingSystem;
using UnityEngine;

namespace Logic
{
	[RequireComponent(typeof(Rigidbody))]
	public class Health : MonoBehaviour, IDamageable
	{
		private int m_hp;
		private IPooled pooled;
		[HideInInspector] public int Max;

		private void Awake()
		{
			pooled = GetComponent<IPooled>();
			m_hp = Max;
		}
		public bool IsDead()
		{
			return m_hp <= 0;
		}

		public void GetDamage(int amount)
		{
			m_hp -= amount;
			if (m_hp <= 0) pooled.Free();
		}
	}
}
