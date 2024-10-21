using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(TeamFilter))]
public class ProjectileControllerTrigger : ProjectileController
{
	public void OnTriggerEnter(Collider collider)
	{
		if (NetworkServer.active && canImpactOnTrigger)
		{
			Vector3 vector = Vector3.zero;
			if ((bool)rigidbody)
			{
				vector = rigidbody.velocity;
			}
			ProjectileImpactInfo projectileImpactInfo = default(ProjectileImpactInfo);
			projectileImpactInfo.collider = collider;
			projectileImpactInfo.estimatedPointOfImpact = base.transform.position;
			projectileImpactInfo.estimatedImpactNormal = -vector.normalized;
			ProjectileImpactInfo impactInfo = projectileImpactInfo;
			IProjectileImpactBehavior[] components = GetComponents<IProjectileImpactBehavior>();
			for (int i = 0; i < components.Length; i++)
			{
				components[i].OnProjectileImpact(impactInfo);
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool flag = base.OnSerialize(writer, forceAll);
		bool flag2 = default(bool);
		return flag2 || flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		base.OnDeserialize(reader, initialState);
	}

	public override void PreStartClient()
	{
		base.PreStartClient();
	}
}
