using System;
using RoR2;
using UnityEngine;

namespace EntityStates.VoidBarnacle;

public class FindSurface : NoCastSpawn
{
	public static int raycastCount;

	public static float maxRaycastLength;

	public static float raycastSphereYOffset;

	public static float raycastMinimumAngle;

	public static float raycastMaximumAngle;

	private const float _cRadianConversionCoeficcient = MathF.PI / 180f;

	public override void OnEnter()
	{
		base.OnEnter();
		RaycastHit hitInfo = default(RaycastHit);
		Vector3 origin = new Vector3(base.characterBody.corePosition.x, base.characterBody.corePosition.y + raycastSphereYOffset, base.characterBody.corePosition.z);
		if (!base.isAuthority)
		{
			return;
		}
		raycastMinimumAngle = Mathf.Clamp(raycastMinimumAngle, 0f, raycastMaximumAngle);
		raycastMaximumAngle = Mathf.Clamp(raycastMaximumAngle, raycastMinimumAngle, 90f);
		raycastCount = Mathf.Abs(raycastCount);
		float num = 360f / (float)raycastCount;
		for (int i = 0; i < raycastCount; i++)
		{
			float minInclusive = num * (float)i;
			float maxInclusive = num * (float)(i + 1) - 1f;
			float f = UnityEngine.Random.Range(minInclusive, maxInclusive) * (MathF.PI / 180f);
			float f2 = UnityEngine.Random.Range(raycastMinimumAngle, raycastMaximumAngle) * (MathF.PI / 180f);
			float x = Mathf.Cos(f);
			float y = Mathf.Sin(f2);
			float z = Mathf.Sin(f);
			Vector3 direction = new Vector3(x, y, z);
			if (Physics.Raycast(origin, direction, out hitInfo, maxRaycastLength, LayerIndex.world.mask))
			{
				base.transform.position = hitInfo.point;
				base.transform.up = hitInfo.normal;
				break;
			}
		}
	}
}
