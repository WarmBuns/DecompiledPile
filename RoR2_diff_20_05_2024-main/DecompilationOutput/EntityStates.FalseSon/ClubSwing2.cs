using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.FalseSon;

public class ClubSwing2 : BasicMeleeAttack, SteppedSkillDef.IStepSetter
{
	public int club;

	public static float recoilAmplitude;

	public static float baseDurationBeforeInterruptable;

	[SerializeField]
	public float bloom;

	public static float comboFinisherBaseDuration;

	public static GameObject comboFinisherSwingEffectPrefab;

	public static float comboFinisherhitPauseDuration;

	public static float comboFinisherDamageCoefficient;

	public static float comboFinisherBloom;

	public static float comboFinisherBaseDurationBeforeInterruptable;

	public static string slash1Sound;

	public static string slash3Sound;

	private string animationStateName;

	private string meleeStreakStateName;

	private float durationBeforeInterruptable;

	private bool isComboFinisher => club == 1;

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
		club = i;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if (isComboFinisher)
		{
			swingEffectPrefab = comboFinisherSwingEffectPrefab;
			hitPauseDuration = comboFinisherhitPauseDuration;
			damageCoefficient = comboFinisherDamageCoefficient;
			bloom = comboFinisherBloom;
			hitBoxGroupName = "SwordLarge";
			baseDuration = comboFinisherBaseDuration;
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
		_ = isComboFinisher;
	}

	protected override void PlayAnimation()
	{
		animationStateName = "";
		string soundString = null;
		switch (club)
		{
		case 0:
			animationStateName = "SwingClubRight";
			meleeStreakStateName = "SwingRight";
			soundString = slash1Sound;
			break;
		case 1:
			animationStateName = "SwingClubLeft";
			meleeStreakStateName = "SwingLeft";
			soundString = slash1Sound;
			break;
		}
		bool @bool = animator.GetBool("isMoving");
		bool bool2 = animator.GetBool("isGrounded");
		if (!@bool && bool2)
		{
			PlayCrossfade("FullBody, Override", animationStateName, "SwingClub.playbackRate", duration, 0.05f);
		}
		else
		{
			PlayCrossfade("Gesture, Additive", animationStateName, "SwingClub.playbackRate", duration, 0.05f);
			PlayCrossfade("Gesture, Override", animationStateName, "SwingClub.playbackRate", duration, 0.05f);
		}
		Util.PlaySound(soundString, base.gameObject);
	}

	protected override void OnMeleeHitAuthority()
	{
		base.OnMeleeHitAuthority();
		base.characterBody.AddSpreadBloom(bloom);
	}

	protected override void BeginMeleeAttackEffect()
	{
		swingEffectMuzzleString = meleeStreakStateName;
		AddRecoil(-0.1f * recoilAmplitude, 0.1f * recoilAmplitude, -1f * recoilAmplitude, 1f * recoilAmplitude);
		base.BeginMeleeAttackEffect();
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write((byte)club);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		club = reader.ReadByte();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Any;
	}
}
