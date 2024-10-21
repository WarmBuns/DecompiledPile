using UnityEngine;

namespace RoR2.Projectile;

public struct ProjectileImpactInfo
{
	public Collider collider;

	public Vector3 estimatedPointOfImpact;

	public Vector3 estimatedImpactNormal;
}
