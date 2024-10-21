using UnityEngine;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class ProjectileImpaleOnEnemy : MonoBehaviour, IProjectileImpactBehavior
{
	private bool alive = true;

	public GameObject impalePrefab;

	private Rigidbody rigidbody;

	private void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
	}

	public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
	{
		if (!alive)
		{
			return;
		}
		Collider collider = impactInfo.collider;
		if (!collider)
		{
			return;
		}
		HurtBox component = collider.GetComponent<HurtBox>();
		if ((bool)component)
		{
			_ = base.transform.position;
			Vector3 estimatedPointOfImpact = impactInfo.estimatedPointOfImpact;
			_ = Quaternion.identity;
			if ((bool)rigidbody)
			{
				Util.QuaternionSafeLookRotation(rigidbody.velocity);
			}
			GameObject obj = Object.Instantiate(impalePrefab, component.transform);
			obj.transform.position = estimatedPointOfImpact;
			obj.transform.rotation = base.transform.rotation;
		}
	}
}
