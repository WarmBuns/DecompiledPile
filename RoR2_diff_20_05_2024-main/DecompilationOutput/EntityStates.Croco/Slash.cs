using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Croco;

public class Slash : BasicMeleeAttack, SteppedSkillDef.IStepSetter
{
	public int step;

	public static float recoilAmplitude;

	public static float baseDurationBeforeInterruptable;

	[SerializeField]
	public float bloom;

	public static GameObject comboFinisherSwingEffectPrefab;

	public static float comboFinisherhitPauseDuration;

	public static float comboFinisherDamageCoefficient;

	public static float comboFinisherBloom;

	public static float comboFinisherBaseDurationBeforeInterruptable;

	public static string slash1Sound;

	public static string slash3Sound;

	private string animationStateName;

	private float durationBeforeInterruptable;

	private bool hasGrantedBuff;

	private bool isComboFinisher => step == 2;

	protected override bool allowExitFire
	{
		get
		{
			if ((bool)base.characterBody)
			{
				return !base.characterBody.isSprinting;
			}
			return false;
		}
	}

	void SteppedSkillDef.IStepSetter.SetStep(int i)
	{
		step = i;
	}

	public override void OnEnter()
	{
		if (isComboFinisher)
		{
			swingEffectPrefab = comboFinisherSwingEffectPrefab;
			hitPauseDuration = comboFinisherhitPauseDuration;
			damageCoefficient = comboFinisherDamageCoefficient;
			bloom = comboFinisherBloom;
		}
		base.OnEnter();
		base.characterDirection.forward = GetAimRay().direction;
		durationBeforeInterruptable = (isComboFinisher ? (comboFinisherBaseDurationBeforeInterruptable / attackSpeedStat) : (baseDurationBeforeInterruptable / attackSpeedStat));
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	protected override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
	{
		base.AuthorityModifyOverlapAttack(overlapAttack);
	}

	protected override void PlayAnimation()
	{
		animationStateName = "";
		string soundString = null;
		switch (step)
		{
		case 0:
			animationStateName = "Slash1";
			soundString = slash1Sound;
			break;
		case 1:
			animationStateName = "Slash2";
			soundString = slash1Sound;
			break;
		case 2:
			animationStateName = "Slash3";
			soundString = slash3Sound;
			break;
		}
		float num = Mathf.Max(duration, 0.2f);
		PlayCrossfade("Gesture, Additive", animationStateName, "Slash.playbackRate", num, 0.05f);
		PlayCrossfade("Gesture, Override", animationStateName, "Slash.playbackRate", num, 0.05f);
		Util.PlaySound(soundString, base.gameObject);
	}

	protected override void OnMeleeHitAuthority()
	{
		base.OnMeleeHitAuthority();
		base.characterBody.AddSpreadBloom(bloom);
		if (!hasGrantedBuff && isComboFinisher)
		{
			hasGrantedBuff = true;
			base.characterBody.AddTimedBuffAuthority(RoR2Content.Buffs.CrocoRegen.buffIndex, 0.5f);
		}
	}

	protected override void BeginMeleeAttackEffect()
	{
		swingEffectMuzzleString = animationStateName;
		AddRecoil(-0.1f * recoilAmplitude, 0.1f * recoilAmplitude, -1f * recoilAmplitude, 1f * recoilAmplitude);
		base.BeginMeleeAttackEffect();
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write((byte)step);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		step = reader.ReadByte();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		if (!(base.fixedAge < durationBeforeInterruptable))
		{
			return InterruptPriority.Skill;
		}
		return InterruptPriority.PrioritySkill;
	}
}
