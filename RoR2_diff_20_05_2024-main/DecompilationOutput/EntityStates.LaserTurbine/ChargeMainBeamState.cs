using RoR2;
using UnityEngine;

namespace EntityStates.LaserTurbine;

public class ChargeMainBeamState : LaserTurbineBaseState
{
	public static float baseDuration;

	public static GameObject beamIndicatorPrefab;

	private GameObject beamIndicatorInstance;

	private ChildLocator beamIndicatorChildLocator;

	private Transform beamIndicatorEndTransform;

	protected override bool shouldFollow => false;

	public override void OnEnter()
	{
		base.OnEnter();
		beamIndicatorInstance = Object.Instantiate(beamIndicatorPrefab, GetMuzzleTransform(), worldPositionStays: false);
		beamIndicatorChildLocator = beamIndicatorInstance.GetComponent<ChildLocator>();
		if ((bool)beamIndicatorChildLocator)
		{
			beamIndicatorEndTransform = beamIndicatorChildLocator.FindChild("End");
		}
	}

	public override void OnExit()
	{
		EntityState.Destroy(beamIndicatorInstance);
		base.OnExit();
	}

	public override void Update()
	{
		base.Update();
		if (base.isAuthority && base.fixedAge >= baseDuration)
		{
			outer.SetNextState(new FireMainBeamState());
		}
		if ((bool)beamIndicatorInstance && (bool)beamIndicatorEndTransform)
		{
			float num = 1000f;
			Ray aimRay = GetAimRay();
			_ = beamIndicatorInstance.transform.parent.position;
			Vector3 point = aimRay.GetPoint(num);
			if (Util.CharacterRaycast(base.ownerBody.gameObject, aimRay, out var hitInfo, num, (int)LayerIndex.entityPrecise.mask | (int)LayerIndex.world.mask, QueryTriggerInteraction.UseGlobal))
			{
				point = hitInfo.point;
			}
			beamIndicatorEndTransform.transform.position = point;
		}
	}
}
