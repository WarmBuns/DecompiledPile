using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Drone.DroneWeapon;

public class HealBeam : BaseState
{
	public static float baseDuration;

	public static float healCoefficient = 5f;

	public static GameObject healBeamPrefab;

	public HurtBox target;

	private HealBeamController healBeamController;

	private float duration;

	private float lineWidthRefVelocity;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayCrossfade("Gesture", "Heal", 0.2f);
		duration = baseDuration / attackSpeedStat;
		float healRate = healCoefficient * damageStat / duration;
		Ray aimRay = GetAimRay();
		Transform transform = FindModelChild("Muzzle");
		if (NetworkServer.active)
		{
			BullseyeSearch bullseyeSearch = new BullseyeSearch();
			bullseyeSearch.teamMaskFilter = TeamMask.none;
			if ((bool)base.teamComponent)
			{
				bullseyeSearch.teamMaskFilter.AddTeam(base.teamComponent.teamIndex);
			}
			bullseyeSearch.filterByLoS = false;
			bullseyeSearch.maxDistanceFilter = 50f;
			bullseyeSearch.maxAngleFilter = 180f;
			bullseyeSearch.searchOrigin = aimRay.origin;
			bullseyeSearch.searchDirection = aimRay.direction;
			bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
			bullseyeSearch.RefreshCandidates();
			bullseyeSearch.FilterOutGameObject(base.gameObject);
			target = bullseyeSearch.GetResults().FirstOrDefault();
			if ((bool)transform && (bool)target)
			{
				GameObject gameObject = Object.Instantiate(healBeamPrefab, transform);
				healBeamController = gameObject.GetComponent<HealBeamController>();
				healBeamController.healRate = healRate;
				healBeamController.target = target;
				healBeamController.ownership.ownerObject = base.gameObject;
				NetworkServer.Spawn(gameObject);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((base.fixedAge >= duration || !target) && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		PlayCrossfade("Gesture", "Empty", 0.2f);
		if ((bool)healBeamController)
		{
			healBeamController.BreakServer();
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Any;
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		HurtBoxReference.FromHurtBox(target).Write(writer);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		HurtBoxReference hurtBoxReference = default(HurtBoxReference);
		hurtBoxReference.Read(reader);
		target = hurtBoxReference.ResolveGameObject()?.GetComponent<HurtBox>();
	}
}
