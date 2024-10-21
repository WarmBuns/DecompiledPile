using RoR2;
using UnityEngine;

namespace EntityStates.TitanMonster;

public class FireGoldFist : FireFist
{
	public static int fistCount;

	public static float distanceBetweenFists;

	public static float delayBetweenFists;

	protected override void PlacePredictedAttack()
	{
		int num = 0;
		Vector3 vector = predictedTargetPosition;
		Vector3 vector2 = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
		for (int i = -(fistCount / 2); i < fistCount / 2; i++)
		{
			Vector3 vector3 = vector + vector2 * distanceBetweenFists * i;
			float num2 = 60f;
			if (Physics.Raycast(new Ray(vector3 + Vector3.up * (num2 / 2f), Vector3.down), out var hitInfo, num2, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
			{
				vector3 = hitInfo.point;
			}
			PlaceSingleDelayBlast(vector3, delayBetweenFists * (float)num);
			num++;
		}
	}
}
