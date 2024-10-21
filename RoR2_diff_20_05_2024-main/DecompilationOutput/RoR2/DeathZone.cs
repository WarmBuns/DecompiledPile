using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

internal class DeathZone : MonoBehaviour
{
	public void OnTriggerEnter(Collider other)
	{
		if (NetworkServer.active)
		{
			HealthComponent component = other.GetComponent<HealthComponent>();
			if ((bool)component)
			{
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.position = other.transform.position;
				damageInfo.attacker = null;
				damageInfo.inflictor = base.gameObject;
				damageInfo.damage = 999999f;
				component.TakeDamage(damageInfo);
			}
		}
	}
}
