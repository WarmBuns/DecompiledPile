using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VagrantMonster;

public class FireMegaNova : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject novaEffectPrefab;

	public static GameObject novaImpactEffectPrefab;

	public static string novaSoundString;

	public static float novaDamageCoefficient;

	public static float novaForce;

	public float novaRadius;

	private float duration;

	private float stopwatch;

	private static int FireMegaNovaStateHash = Animator.StringToHash("FireMegaNova");

	private static int FireMegaNovaParamHash = Animator.StringToHash("FireMegaNova.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture, Override", FireMegaNovaStateHash, FireMegaNovaParamHash, duration);
		Detonate();
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void Detonate()
	{
		Vector3 position = base.transform.position;
		Util.PlaySound(novaSoundString, base.gameObject);
		if ((bool)novaEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(novaEffectPrefab, base.gameObject, "NovaCenter", transmit: false);
		}
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
			temporaryOverlayInstance.duration = 3f;
			temporaryOverlayInstance.animateShaderAlpha = true;
			temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			temporaryOverlayInstance.destroyComponentOnEnd = true;
			temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matVagrantEnergized");
			temporaryOverlayInstance.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
		}
		if (NetworkServer.active)
		{
			BlastAttack blastAttack = new BlastAttack();
			blastAttack.attacker = base.gameObject;
			blastAttack.baseDamage = damageStat * novaDamageCoefficient;
			blastAttack.baseForce = novaForce;
			blastAttack.bonusForce = Vector3.zero;
			blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
			blastAttack.crit = base.characterBody.RollCrit();
			blastAttack.damageColorIndex = DamageColorIndex.Default;
			blastAttack.damageType = DamageType.Generic;
			blastAttack.falloffModel = BlastAttack.FalloffModel.None;
			blastAttack.inflictor = base.gameObject;
			blastAttack.position = position;
			blastAttack.procChainMask = default(ProcChainMask);
			blastAttack.procCoefficient = 3f;
			blastAttack.radius = novaRadius;
			blastAttack.losType = BlastAttack.LoSType.NearestHit;
			blastAttack.teamIndex = base.teamComponent.teamIndex;
			blastAttack.impactEffect = EffectCatalog.FindEffectIndexFromPrefab(novaImpactEffectPrefab);
			blastAttack.Fire();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}
