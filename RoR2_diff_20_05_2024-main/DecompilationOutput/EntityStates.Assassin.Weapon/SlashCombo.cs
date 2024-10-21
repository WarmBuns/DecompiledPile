using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Assassin.Weapon;

public class SlashCombo : BaseState
{
	public enum SlashComboPermutation
	{
		Slash1,
		Slash2,
		Final
	}

	public static float baseDuration = 3.5f;

	public static float mecanimDurationCoefficient;

	public static float damageCoefficient = 4f;

	public static float forceMagnitude = 16f;

	public static float selfForceMagnitude;

	public static float radius = 3f;

	public static GameObject hitEffectPrefab;

	public static GameObject swingEffectPrefab;

	public static string attackString;

	private OverlapAttack attack;

	private Animator modelAnimator;

	private float duration;

	private bool hasSlashed;

	public SlashComboPermutation slashComboPermutation;

	private static int SlashP1Hash = Animator.StringToHash("SlashP1");

	private static int SlashP2Hash = Animator.StringToHash("SlashP2");

	private static int SlashP3Hash = Animator.StringToHash("SlashP3");

	private static int SlashComboParamHash = Animator.StringToHash("SlashCombo.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		Transform modelTransform = GetModelTransform();
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.damage = damageCoefficient * damageStat;
		attack.hitEffectPrefab = hitEffectPrefab;
		attack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
		Util.PlaySound(attackString, base.gameObject);
		string hitboxGroupName = "";
		int animationStateHash = -1;
		switch (slashComboPermutation)
		{
		case SlashComboPermutation.Slash1:
			hitboxGroupName = "DaggerLeft";
			animationStateHash = SlashP1Hash;
			break;
		case SlashComboPermutation.Slash2:
			hitboxGroupName = "DaggerLeft";
			animationStateHash = SlashP2Hash;
			break;
		case SlashComboPermutation.Final:
			hitboxGroupName = "DaggerLeft";
			animationStateHash = SlashP3Hash;
			break;
		}
		if ((bool)modelTransform)
		{
			attack.hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == hitboxGroupName);
		}
		if ((bool)modelAnimator)
		{
			PlayAnimation("Gesture, Override", animationStateHash, SlashComboParamHash, duration * mecanimDurationCoefficient);
			PlayAnimation("Gesture, Additive", animationStateHash, SlashComboParamHash, duration * mecanimDurationCoefficient);
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(2f);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && (bool)modelAnimator && modelAnimator.GetFloat("SlashCombo.hitBoxActive") > 0.1f)
		{
			if (!hasSlashed)
			{
				EffectManager.SimpleMuzzleFlash(swingEffectPrefab, base.gameObject, "SwingCenter", transmit: true);
				HealthComponent healthComponent = base.characterBody.healthComponent;
				CharacterDirection component = base.characterBody.GetComponent<CharacterDirection>();
				if ((bool)healthComponent)
				{
					healthComponent.TakeDamageForce(selfForceMagnitude * component.forward, alwaysApply: true);
				}
				hasSlashed = true;
			}
			attack.forceVector = base.transform.forward * forceMagnitude;
			attack.Fire();
		}
		if (!(base.fixedAge >= duration) || !base.isAuthority)
		{
			return;
		}
		if ((bool)base.inputBank && base.inputBank.skill1.down)
		{
			SlashCombo slashCombo = new SlashCombo();
			switch (slashComboPermutation)
			{
			case SlashComboPermutation.Slash1:
				slashCombo.slashComboPermutation = SlashComboPermutation.Slash2;
				break;
			case SlashComboPermutation.Slash2:
				slashCombo.slashComboPermutation = SlashComboPermutation.Slash1;
				break;
			}
			outer.SetNextState(slashCombo);
		}
		else
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
