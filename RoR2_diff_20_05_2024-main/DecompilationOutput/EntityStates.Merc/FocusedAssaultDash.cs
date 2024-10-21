using RoR2;
using RoR2.Networking;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Merc;

public class FocusedAssaultDash : BasicMeleeAttack
{
	[SerializeField]
	public float speedCoefficientOnExit;

	[SerializeField]
	public float speedCoefficient;

	[SerializeField]
	public string endSoundString;

	[SerializeField]
	public float exitSmallHop;

	[SerializeField]
	public float delayedDamageCoefficient;

	[SerializeField]
	public float delayedProcCoefficient;

	[SerializeField]
	public float delay;

	[SerializeField]
	public string enterAnimationLayerName = "FullBody, Override";

	[SerializeField]
	public string enterAnimationStateName = "AssaulterLoop";

	[SerializeField]
	public float enterAnimationCrossfadeDuration = 0.1f;

	[SerializeField]
	public string exitAnimationLayerName = "FullBody, Override";

	[SerializeField]
	public string exitAnimationStateName = "EvisLoopExit";

	[SerializeField]
	public Material enterOverlayMaterial;

	[SerializeField]
	public float enterOverlayDuration = 0.7f;

	[SerializeField]
	public GameObject delayedEffectPrefab;

	[SerializeField]
	public GameObject orbEffect;

	[SerializeField]
	public float delayPerHit;

	[SerializeField]
	public GameObject selfOnHitOverlayEffectPrefab;

	private Transform modelTransform;

	private Vector3 dashVector;

	private int originalLayer;

	private int currentHitCount;

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
			temporaryOverlayInstance.duration = enterOverlayDuration;
			temporaryOverlayInstance.animateShaderAlpha = true;
			temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			temporaryOverlayInstance.destroyComponentOnEnd = true;
			temporaryOverlayInstance.originalMaterial = enterOverlayMaterial;
			temporaryOverlayInstance.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
		}
		PlayCrossfade(enterAnimationLayerName, enterAnimationStateName, enterAnimationCrossfadeDuration);
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
		PlayAnimation(exitAnimationLayerName, exitAnimationStateName);
		base.gameObject.layer = originalLayer;
		base.characterMotor.Motor.RebuildCollidableLayers();
		base.OnExit();
	}

	protected override void PlayAnimation()
	{
		base.PlayAnimation();
		PlayCrossfade(enterAnimationLayerName, enterAnimationStateName, enterAnimationCrossfadeDuration);
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
		}
	}

	protected override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
	{
		base.AuthorityModifyOverlapAttack(overlapAttack);
		overlapAttack.damage = damageCoefficient * damageStat;
	}

	protected override void OnMeleeHitAuthority()
	{
		base.OnMeleeHitAuthority();
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
		foreach (HurtBox hitResult in hitResults)
		{
			currentHitCount++;
			float damageValue = base.characterBody.damage * delayedDamageCoefficient;
			float num2 = delay + delayPerHit * (float)currentHitCount;
			bool isCrit = RollCrit();
			HandleHit(base.gameObject, hitResult, damageValue, delayedProcCoefficient, isCrit, num2, orbEffect, delayedEffectPrefab);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	private static void HandleHit(GameObject attackerObject, HurtBox victimHurtBox, float damageValue, float procCoefficient, bool isCrit, float delay, GameObject orbEffectPrefab, GameObject orbImpactEffectPrefab)
	{
		if (!NetworkServer.active)
		{
			NetworkWriter networkWriter = new NetworkWriter();
			networkWriter.StartMessage(77);
			networkWriter.Write(attackerObject);
			networkWriter.Write(HurtBoxReference.FromHurtBox(victimHurtBox));
			networkWriter.Write(damageValue);
			networkWriter.Write(procCoefficient);
			networkWriter.Write(isCrit);
			networkWriter.Write(delay);
			networkWriter.WriteEffectIndex(EffectCatalog.FindEffectIndexFromPrefab(orbEffectPrefab));
			networkWriter.WriteEffectIndex(EffectCatalog.FindEffectIndexFromPrefab(orbImpactEffectPrefab));
			networkWriter.FinishMessage();
			ClientScene.readyConnection?.SendWriter(networkWriter, QosChannelIndex.defaultReliable.intVal);
		}
		else if ((bool)victimHurtBox && (bool)victimHurtBox.healthComponent)
		{
			SetStateOnHurt.SetStunOnObject(victimHurtBox.healthComponent.gameObject, delay);
			OrbManager.instance.AddOrb(new DelayedHitOrb
			{
				attacker = attackerObject,
				target = victimHurtBox,
				damageColorIndex = DamageColorIndex.Default,
				damageValue = damageValue,
				damageType = DamageType.ApplyMercExpose,
				isCrit = isCrit,
				procChainMask = default(ProcChainMask),
				procCoefficient = procCoefficient,
				delay = delay,
				orbEffect = orbEffectPrefab,
				delayedEffectPrefab = orbImpactEffectPrefab
			});
		}
	}

	[NetworkMessageHandler(msgType = 77, client = false, server = true)]
	private static void HandleReportMercFocusedAssaultHitReplaceMeLater(NetworkMessage netMsg)
	{
		GameObject attackerObject = netMsg.reader.ReadGameObject();
		HurtBox victimHurtBox = netMsg.reader.ReadHurtBoxReference().ResolveHurtBox();
		float damageValue = netMsg.reader.ReadSingle();
		float num = netMsg.reader.ReadSingle();
		bool isCrit = netMsg.reader.ReadBoolean();
		float num2 = netMsg.reader.ReadSingle();
		GameObject orbEffectPrefab = EffectCatalog.GetEffectDef(netMsg.reader.ReadEffectIndex())?.prefab ?? null;
		GameObject orbImpactEffectPrefab = EffectCatalog.GetEffectDef(netMsg.reader.ReadEffectIndex())?.prefab ?? null;
		HandleHit(attackerObject, victimHurtBox, damageValue, num, isCrit, num2, orbEffectPrefab, orbImpactEffectPrefab);
	}
}
