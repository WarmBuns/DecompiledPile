using RoR2.Networking;
using UnityEngine;

namespace RoR2;

public static class OffScreenMissHelper
{
	public static bool IsPositionOffScreen(Vector3 position, float offScreenThreshold = -0.5f, float minDistance = 5f, float maxDistance = 120f)
	{
		bool result = true;
		foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
		{
			CharacterBody currentBody = readOnlyInstances.GetCurrentBody();
			if ((object)currentBody == null)
			{
				continue;
			}
			NetworkedViewAngles component = readOnlyInstances.GetComponent<NetworkedViewAngles>();
			if ((object)component == null)
			{
				continue;
			}
			Vector3 vector = currentBody.transform.position - position;
			float magnitude = vector.magnitude;
			if (!(magnitude > maxDistance))
			{
				if (magnitude < minDistance)
				{
					result = false;
					break;
				}
				if (Vector3.Dot(Quaternion.Euler(component.viewAngles.pitch, component.viewAngles.yaw, 0f) * Vector3.forward, vector.normalized) < offScreenThreshold)
				{
					result = false;
					break;
				}
			}
		}
		return result;
	}

	public static Vector3 ApplyRandomTrajectoryOffset(Vector3 hitTrajectory, float magnitude = 4f)
	{
		return Util.ApplySpread(hitTrajectory, magnitude, magnitude, 1f, 1f);
	}
}
