using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Commando.CommandoWeapon;

public class CastSmokescreenNoDelay : BaseState
{
	public static float duration;

	public static float minimumStateDuration = 3f;

	public static string startCloakSoundString;

	public static string stopCloakSoundString;

	public static GameObject smokescreenEffectPrefab;

	public static Material destealthMaterial;

	public static float damageCoefficient = 1.3f;

	public static float radius = 4f;

	public static float forceMagnitude = 100f;

	private float stopwatch;

	private bool hasCastSmoke;

	private Animator animator;

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		CastSmoke();
		if ((bool)base.characterBody && NetworkServer.active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.Cloak);
			base.characterBody.AddBuff(RoR2Content.Buffs.CloakSpeed);
		}
	}

	public override void OnExit()
	{
		if ((bool)base.characterBody && NetworkServer.active)
		{
			if (base.characterBody.HasBuff(RoR2Content.Buffs.Cloak))
			{
				base.characterBody.RemoveBuff(RoR2Content.Buffs.Cloak);
			}
			if (base.characterBody.HasBuff(RoR2Content.Buffs.CloakSpeed))
			{
				base.characterBody.RemoveBuff(RoR2Content.Buffs.CloakSpeed);
			}
		}
		if (!outer.destroying)
		{
			CastSmoke();
		}
		if ((bool)destealthMaterial)
		{
			TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(animator.gameObject);
			temporaryOverlayInstance.duration = 1f;
			temporaryOverlayInstance.destroyComponentOnEnd = true;
			temporaryOverlayInstance.originalMaterial = destealthMaterial;
			temporaryOverlayInstance.inspectorCharacterModel = animator.gameObject.GetComponent<CharacterModel>();
			temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			temporaryOverlayInstance.animateShaderAlpha = true;
		}
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

	private void CastSmoke()
	{
		if (!hasCastSmoke)
		{
			Util.PlaySound(startCloakSoundString, base.gameObject);
			hasCastSmoke = true;
		}
		else
		{
			Util.PlaySound(stopCloakSoundString, base.gameObject);
		}
		EffectManager.SpawnEffect(smokescreenEffectPrefab, new EffectData
		{
			origin = base.transform.position
		}, transmit: false);
		int layerIndex = animator.GetLayerIndex("Impact");
		if (layerIndex >= 0)
		{
			animator.SetLayerWeight(layerIndex, 1f);
			animator.PlayInFixedTime("LightImpact", layerIndex, 0f);
		}
		if (NetworkServer.active)
		{
			BlastAttack blastAttack = new BlastAttack();
			blastAttack.attacker = base.gameObject;
			blastAttack.inflictor = base.gameObject;
			blastAttack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
			blastAttack.baseDamage = damageStat * damageCoefficient;
			blastAttack.baseForce = forceMagnitude;
			blastAttack.position = base.transform.position;
			blastAttack.radius = radius;
			blastAttack.falloffModel = BlastAttack.FalloffModel.None;
			blastAttack.damageType = DamageType.Stun1s;
			blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
			blastAttack.Fire();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		if (!(stopwatch > minimumStateDuration))
		{
			return InterruptPriority.PrioritySkill;
		}
		return InterruptPriority.Any;
	}
}
