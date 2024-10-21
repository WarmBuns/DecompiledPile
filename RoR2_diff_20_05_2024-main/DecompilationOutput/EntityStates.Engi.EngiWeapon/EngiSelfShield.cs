using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Engi.EngiWeapon;

public class EngiSelfShield : BaseState
{
	public static float transferDelay = 0.1f;

	private HurtBox transferTarget;

	private BullseyeSearch friendLocator;

	private Indicator indicator;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active && (bool)base.characterBody)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.EngiShield);
			base.characterBody.RecalculateStats();
			if ((bool)base.healthComponent)
			{
				base.healthComponent.RechargeShieldFull();
			}
		}
		friendLocator = new BullseyeSearch();
		friendLocator.teamMaskFilter = TeamMask.none;
		if ((bool)base.teamComponent)
		{
			friendLocator.teamMaskFilter.AddTeam(base.teamComponent.teamIndex);
		}
		friendLocator.maxDistanceFilter = 80f;
		friendLocator.maxAngleFilter = 20f;
		friendLocator.sortMode = BullseyeSearch.SortMode.Angle;
		friendLocator.filterByLoS = false;
		indicator = new Indicator(base.gameObject, LegacyResourcesAPI.Load<GameObject>("Prefabs/ShieldTransferIndicator"));
	}

	public override void OnExit()
	{
		base.skillLocator.utility = base.skillLocator.FindSkill("RetractShield");
		if (NetworkServer.active)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.EngiShield);
		}
		if (base.isAuthority)
		{
			base.skillLocator.utility.RemoveAllStocks();
		}
		indicator.active = false;
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.isAuthority || !(base.fixedAge >= transferDelay) || !base.skillLocator.utility.IsReady())
		{
			return;
		}
		if ((bool)base.characterBody)
		{
			float extraRaycastDistance = 0f;
			Ray ray = CameraRigController.ModifyAimRayIfApplicable(GetAimRay(), base.gameObject, out extraRaycastDistance);
			friendLocator.searchOrigin = ray.origin;
			friendLocator.searchDirection = ray.direction;
			friendLocator.maxDistanceFilter += extraRaycastDistance;
			friendLocator.RefreshCandidates();
			friendLocator.FilterOutGameObject(base.gameObject);
			transferTarget = friendLocator.GetResults().FirstOrDefault();
		}
		HealthComponent healthComponent = (transferTarget ? transferTarget.healthComponent : null);
		if ((bool)healthComponent)
		{
			indicator.targetTransform = Util.GetCoreTransform(healthComponent.gameObject);
			if ((bool)base.inputBank && base.inputBank.skill3.justPressed)
			{
				EngiOtherShield engiOtherShield = new EngiOtherShield();
				engiOtherShield.target = healthComponent.gameObject.GetComponent<CharacterBody>();
				outer.SetNextState(engiOtherShield);
				return;
			}
		}
		else
		{
			indicator.targetTransform = null;
		}
		indicator.active = indicator.targetTransform;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
