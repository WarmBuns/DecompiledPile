using UnityEngine;

namespace RoR2.Projectile;

public class ProjectileTargetComponent : MonoBehaviour
{
	public Transform target { get; set; }

	private void FixedUpdate()
	{
		if ((bool)target && !target.gameObject.activeSelf)
		{
			target = null;
		}
	}
}
