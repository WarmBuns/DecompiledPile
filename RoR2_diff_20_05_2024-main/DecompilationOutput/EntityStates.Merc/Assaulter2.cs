using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Merc;

public class Assaulter2 : BasicMeleeAttack
{
	public static float speedCoefficientOnExit;

	public static float speedCoefficient;

	public static string endSoundString;

	public static float exitSmallHop;

	public static GameObject selfOnHitOverlayEffectPrefab;

	public bool grantAnotherDash;

	private Transform modelTransform;

	private Vector3 dashVector;

	private int originalLayer;

	private static int EvisLoopExitStateHash = Animator.StringToHash("EvisLoopExit");

	private static int AssaulterLoopStateHash = Animator.StringToHash("AssaulterLoop");

	private bool bufferedSkill2;

	private Vector3 dashVelocity => dashVector * moveSpeedStat * speedCoefficient;

	public override void OnEnter()
	{
		base.OnEnter();
		dashVector = base.inputBank.aimDirection;
		originalLayer = base.gameObject.layer;
		base.gameObject.layer = LayerIndex.GetAppropriateFakeLayerForTeam(base.teamComponent.teamIndex).intVal;
		base.characterMotor.Motor.RebuildCollidableLayers();
		base.characterMotor.Motor.ForceUnground();
		base.characterMotor.velocity = Vector3.zero;
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
			temporaryOverlayInstance.duration = 0.7f;
			temporaryOverlayInstance.animateShaderAlpha = true;
			temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			temporaryOverlayInstance.destroyComponentOnEnd = true;
			temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matMercEnergized");
			temporaryOverlayInstance.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
		}
		PlayCrossfade("FullBody, Override", "AssaulterLoop", 0.1f);
		base.characterDirection.forward = base.characterMotor.velocity.normalized;
		if (NetworkServer.active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
		}
	}

	public override void OnExit()
	{
		if (NetworkServer.active)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
		}
		base.characterMotor.velocity *= speedCoefficientOnExit;
		SmallHop(base.characterMotor, exitSmallHop);
		Util.PlaySound(endSoundString, base.gameObject);
		PlayAnimation("FullBody, Override", EvisLoopExitStateHash);
		base.gameObject.layer = originalLayer;
		base.characterMotor.Motor.RebuildCollidableLayers();
		base.OnExit();
	}

	protected override void PlayAnimation()
	{
		base.PlayAnimation();
		PlayCrossfade("FullBody, Override", AssaulterLoopStateHash, 0.1f);
	}

	protected override void AuthorityFixedUpdate()
	{
		base.AuthorityFixedUpdate();
		if (!base.authorityInHitPause)
		{
			base.characterMotor.rootMotion += dashVelocity * GetDeltaTime();
			base.characterDirection.forward = dashVelocity;
			base.characterDirection.moveVector = dashVelocity;
			base.characterBody.isSprinting = true;
			if (bufferedSkill2)
			{
				base.skillLocator.secondary.ExecuteIfReady();
				bufferedSkill2 = false;
			}
		}
		if ((bool)base.skillLocator && base.skillLocator.secondary.IsReady() && base.inputBank.skill2.down)
		{
			bufferedSkill2 = true;
		}
	}

	protected override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
	{
		base.AuthorityModifyOverlapAttack(overlapAttack);
		overlapAttack.damageType = DamageType.Stun1s;
		overlapAttack.damage = damageCoefficient * damageStat;
	}

	protected override void OnMeleeHitAuthority()
	{
		base.OnMeleeHitAuthority();
		grantAnotherDash = true;
		float num = hitPauseDuration / attackSpeedStat;
		if ((bool)selfOnHitOverlayEffectPrefab && num > 1f / 30f)
		{
			EffectData effectData = new EffectData
			{
				origin = base.transform.position,
				genericFloat = hitPauseDuration / attackSpeedStat
			};
			effectData.SetNetworkedObjectReference(base.gameObject);
			EffectManager.SpawnEffect(selfOnHitOverlayEffectPrefab, effectData, transmit: true);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
