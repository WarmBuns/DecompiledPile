using RoR2;
using UnityEngine;

namespace EntityStates.VoidRaidCrab.Weapon;

public abstract class BaseMultiBeamState : BaseState
{
	public static float beamMaxDistance = 1000f;

	public static string muzzleName;

	protected Transform muzzleTransform { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		muzzleTransform = FindModelChild(muzzleName);
	}

	protected void CalcBeamPath(out Ray beamRay, out Vector3 beamEndPos)
	{
		Ray aimRay = GetAimRay();
		float num = float.PositiveInfinity;
		RaycastHit[] array = Physics.RaycastAll(aimRay, beamMaxDistance, LayerIndex.CommonMasks.bullet, QueryTriggerInteraction.Ignore);
		Transform root = GetModelTransform().root;
		for (int i = 0; i < array.Length; i++)
		{
			ref RaycastHit reference = ref array[i];
			float distance = reference.distance;
			if (distance < num && (object)reference.collider.transform.root != root)
			{
				num = distance;
			}
		}
		num = Mathf.Min(num, beamMaxDistance);
		beamEndPos = aimRay.GetPoint(num);
		Vector3 position = muzzleTransform.position;
		beamRay = new Ray(position, beamEndPos - position);
	}
}
