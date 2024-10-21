using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Huntress;

public class BackflipState : BaseState
{
	public static float duration = 0.9f;

	public static float initialSpeedCoefficient;

	public static float finalSpeedCoefficient;

	public static string dodgeSoundString;

	public static float dodgeFOV;

	public static float orbDamageCoefficient;

	public static float orbRange;

	public static int orbCountMax;

	public static float orbPrefireDuration;

	public static float orbFrequency;

	public static float orbProcCoefficient;

	public static string muzzleString;

	public static float smallHopStrength;

	public static GameObject muzzleflashEffectPrefab;

	private ChildLocator childLocator;

	private float stopwatch;

	private float orbStopwatch;

	private Vector3 forwardDirection;

	private Animator animator;

	private int orbCount;

	private static int BackflipStateHash = Animator.StringToHash("Backflip");

	private static int BackflipParamHash = Animator.StringToHash("Backflip.playbackRate");

	private static int FireSeekingArrowStateHash = Animator.StringToHash("FireSeekingArrow");

	public override void OnEnter()
	{
		base.OnEnter();
		Transform modelTransform = GetModelTransform();
		childLocator = modelTransform.GetComponent<ChildLocator>();
		base.characterMotor.velocity.y = Mathf.Max(base.characterMotor.velocity.y, 0f);
		animator = GetModelAnimator();
		Util.PlaySound(dodgeSoundString, base.gameObject);
		orbStopwatch = 0f - orbPrefireDuration;
		if ((bool)base.characterMotor && smallHopStrength != 0f)
		{
			base.characterMotor.velocity.y = smallHopStrength;
		}
		if (base.isAuthority && (bool)base.inputBank)
		{
			forwardDirection = -Vector3.ProjectOnPlane(base.inputBank.aimDirection, Vector3.up);
		}
		base.characterDirection.moveVector = -forwardDirection;
		PlayAnimation("FullBody, Override", BackflipStateHash, BackflipParamHash, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float deltaTime = GetDeltaTime();
		stopwatch += deltaTime;
		orbStopwatch += deltaTime;
		if ((bool)base.cameraTargetParams)
		{
			base.cameraTargetParams.fovOverride = Mathf.Lerp(dodgeFOV, 60f, stopwatch / duration);
		}
		if ((bool)base.characterMotor && (bool)base.characterDirection)
		{
			Vector3 velocity = base.characterMotor.velocity;
			Vector3 velocity2 = forwardDirection * (moveSpeedStat * Mathf.Lerp(initialSpeedCoefficient, finalSpeedCoefficient, stopwatch / duration));
			base.characterMotor.velocity = velocity2;
			base.characterMotor.velocity.y = velocity.y;
			base.characterMotor.moveDirection = forwardDirection;
		}
		if (orbStopwatch >= 1f / orbFrequency / attackSpeedStat && orbCount < orbCountMax)
		{
			orbStopwatch -= 1f / orbFrequency / attackSpeedStat;
			FireOrbArrow();
		}
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void FireOrbArrow()
	{
		if (NetworkServer.active)
		{
			orbCount++;
			HuntressArrowOrb huntressArrowOrb = new HuntressArrowOrb();
			huntressArrowOrb.damageValue = base.characterBody.damage * orbDamageCoefficient;
			huntressArrowOrb.isCrit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
			huntressArrowOrb.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
			huntressArrowOrb.attacker = base.gameObject;
			huntressArrowOrb.damageColorIndex = DamageColorIndex.Poison;
			huntressArrowOrb.procChainMask.AddProc(ProcType.HealOnHit);
			huntressArrowOrb.procCoefficient = orbProcCoefficient;
			Ray aimRay = GetAimRay();
			BullseyeSearch bullseyeSearch = new BullseyeSearch();
			bullseyeSearch.searchOrigin = aimRay.origin;
			bullseyeSearch.searchDirection = aimRay.direction;
			bullseyeSearch.maxDistanceFilter = orbRange;
			bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
			bullseyeSearch.teamMaskFilter.RemoveTeam(huntressArrowOrb.teamIndex);
			bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
			bullseyeSearch.RefreshCandidates();
			List<HurtBox> list = bullseyeSearch.GetResults().ToList();
			HurtBox hurtBox = ((list.Count > 0) ? list[Random.Range(0, list.Count)] : null);
			if ((bool)hurtBox)
			{
				Transform transform = childLocator.FindChild(muzzleString).transform;
				EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: true);
				huntressArrowOrb.origin = transform.position;
				huntressArrowOrb.target = hurtBox;
				PlayAnimation("Gesture, Override", FireSeekingArrowStateHash);
				PlayAnimation("Gesture, Additive", FireSeekingArrowStateHash);
				OrbManager.instance.AddOrb(huntressArrowOrb);
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if ((bool)base.cameraTargetParams)
		{
			base.cameraTargetParams.fovOverride = -1f;
		}
		int layerIndex = animator.GetLayerIndex("Impact");
		if (layerIndex >= 0)
		{
			animator.SetLayerWeight(layerIndex, 1.5f);
			animator.PlayInFixedTime("LightImpact", layerIndex, 0f);
		}
	}
}
