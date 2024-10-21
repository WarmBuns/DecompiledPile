using System.Linq;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Huntress.HuntressWeapon;

public class IdleTracking : BaseState
{
	public static float maxTrackingDistance = 20f;

	public static float maxTrackingAngle = 20f;

	public static float orbDamageCoefficient;

	public static float orbProcCoefficient;

	public static string muzzleString;

	public static GameObject muzzleflashEffectPrefab;

	public static string attackSoundString;

	public static float fireFrequency;

	private float fireTimer;

	private Transform trackingIndicatorTransform;

	private HurtBox trackingTarget;

	private ChildLocator childLocator;

	private BullseyeSearch search;

	private GameObject TrackingIndicatorPrefab;

	private EffectManagerHelper _emh_trackingIndicator;

	private static int FireSeekingArrowStateHash = Animator.StringToHash("FireSeekingArrow");

	public override void Reset()
	{
		base.Reset();
		fireTimer = 0f;
		trackingIndicatorTransform = null;
		trackingTarget = null;
		childLocator = null;
		if (search != null)
		{
			search.Reset();
		}
		_emh_trackingIndicator = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
		}
	}

	protected void DestroyTrackingIndicator()
	{
		if ((bool)trackingIndicatorTransform)
		{
			if (_emh_trackingIndicator != null && _emh_trackingIndicator.OwningPool != null)
			{
				_emh_trackingIndicator.OwningPool.ReturnObject(_emh_trackingIndicator);
			}
			else
			{
				EntityState.Destroy(trackingIndicatorTransform.gameObject);
			}
			trackingIndicatorTransform = null;
			_emh_trackingIndicator = null;
		}
	}

	public override void OnExit()
	{
		DestroyTrackingIndicator();
		base.OnExit();
	}

	private void FireOrbArrow()
	{
		if (NetworkServer.active)
		{
			HuntressArrowOrb huntressArrowOrb = new HuntressArrowOrb();
			huntressArrowOrb.damageValue = base.characterBody.damage * orbDamageCoefficient;
			huntressArrowOrb.isCrit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
			huntressArrowOrb.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
			huntressArrowOrb.attacker = base.gameObject;
			huntressArrowOrb.damageColorIndex = DamageColorIndex.Poison;
			huntressArrowOrb.procChainMask.AddProc(ProcType.HealOnHit);
			huntressArrowOrb.procCoefficient = orbProcCoefficient;
			HurtBox hurtBox = trackingTarget;
			if ((bool)hurtBox)
			{
				Transform transform = childLocator.FindChild(muzzleString).transform;
				EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: true);
				huntressArrowOrb.origin = transform.position;
				huntressArrowOrb.target = hurtBox;
				PlayAnimation("Gesture, Override", FireSeekingArrowStateHash);
				PlayAnimation("Gesture, Additive", FireSeekingArrowStateHash);
				Util.PlaySound(attackSoundString, base.gameObject);
				OrbManager.instance.AddOrb(huntressArrowOrb);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.isAuthority)
		{
			return;
		}
		fireTimer -= GetDeltaTime();
		if ((bool)base.characterBody)
		{
			float extraRaycastDistance = 0f;
			Ray ray = CameraRigController.ModifyAimRayIfApplicable(GetAimRay(), base.gameObject, out extraRaycastDistance);
			if (search == null)
			{
				search = new BullseyeSearch();
			}
			search.searchOrigin = ray.origin;
			search.searchDirection = ray.direction;
			search.maxDistanceFilter = maxTrackingDistance + extraRaycastDistance;
			search.maxAngleFilter = maxTrackingAngle;
			search.teamMaskFilter = TeamMask.allButNeutral;
			search.teamMaskFilter.RemoveTeam(TeamComponent.GetObjectTeam(base.gameObject));
			search.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
			search.RefreshCandidates();
			trackingTarget = search.GetResults().FirstOrDefault();
		}
		if ((bool)trackingTarget)
		{
			if (!trackingIndicatorTransform)
			{
				if (TrackingIndicatorPrefab == null)
				{
					TrackingIndicatorPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/ShieldTransferIndicator");
				}
				if (!EffectManager.ShouldUsePooledEffect(TrackingIndicatorPrefab))
				{
					trackingIndicatorTransform = Object.Instantiate(TrackingIndicatorPrefab, trackingTarget.transform.position, Quaternion.identity).transform;
				}
				else
				{
					_emh_trackingIndicator = EffectManager.GetAndActivatePooledEffect(TrackingIndicatorPrefab, trackingTarget.transform.position, Quaternion.identity);
					trackingIndicatorTransform = _emh_trackingIndicator.gameObject.transform;
				}
			}
			trackingIndicatorTransform.position = trackingTarget.transform.position;
			if ((bool)base.inputBank && base.inputBank.skill1.down && fireTimer <= 0f)
			{
				fireTimer = 1f / fireFrequency / attackSpeedStat;
				FireOrbArrow();
			}
		}
		else
		{
			DestroyTrackingIndicator();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Any;
	}
}
