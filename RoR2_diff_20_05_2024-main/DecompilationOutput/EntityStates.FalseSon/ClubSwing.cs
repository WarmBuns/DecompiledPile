using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.FalseSon;

public class ClubSwing : BasicMeleeAttack, SteppedSkillDef.IStepSetter, ISkillOverrideHandoff
{
	public int club;

	public static GameObject secondarySwingEffectPrefab;

	[SerializeField]
	public SkillDef secondaryOverrideSkill;

	[SerializeField]
	public SkillDef specialOverrideSkill;

	[SerializeField]
	public float secondaryAltCooldownDuration = 0.7f;

	private SkillStateOverrideData skillOverrideData;

	private float startTimeStamp;

	private bool resetClubSwing;

	private bool clearOverrides = true;

	private FalseSonController falseSonController;

	void SteppedSkillDef.IStepSetter.SetStep(int i)
	{
		club = i;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		falseSonController = base.characterBody.GetComponent<FalseSonController>();
		if (CheckReadyForSecondaryAlt())
		{
			GenerateSkillOverrideData();
		}
		else
		{
			EnsureSkillOverrideState(readyToFire: false);
		}
		clearOverrides = true;
		startTimeStamp = Time.time;
	}

	void ISkillOverrideHandoff.TransferSkillOverride(SkillStateOverrideData skillOverrideData)
	{
		this.skillOverrideData = skillOverrideData;
	}

	private bool CheckReadyForSecondaryAlt()
	{
		if (base.inputBank.skill1.down)
		{
			return Time.time - falseSonController.GetClubSwingAltSecondaryTimestamp() > secondaryAltCooldownDuration;
		}
		return false;
	}

	private void EnsureSkillOverrideState(bool readyToFire)
	{
		if (readyToFire ^ (skillOverrideData != null))
		{
			if (readyToFire)
			{
				GenerateSkillOverrideData();
			}
			else
			{
				ClearSkillOverrideData();
			}
		}
	}

	private void GenerateSkillOverrideData()
	{
		if (skillOverrideData == null)
		{
			skillOverrideData = new SkillStateOverrideData(base.characterBody);
			skillOverrideData.secondarySkillOverride = secondaryOverrideSkill;
			skillOverrideData.overrideFullReloadOnAssign = true;
			skillOverrideData.simulateRestockForOverridenSkills = false;
			skillOverrideData.OverrideSkills(base.skillLocator);
		}
	}

	private void ClearSkillOverrideData()
	{
		if (skillOverrideData != null)
		{
			skillOverrideData.ClearOverrides();
		}
		skillOverrideData = null;
	}

	public override void FixedUpdate()
	{
		if (skillOverrideData != null)
		{
			skillOverrideData.StepRestock();
		}
		bool flag = CheckReadyForSecondaryAlt();
		EnsureSkillOverrideState(flag);
		if (flag && base.inputBank.skill2.justPressed)
		{
			ResetClubSwing();
			outer.SetNextState(GetNextStateAuthority());
		}
		base.FixedUpdate();
	}

	private void ResetClubSwing()
	{
		SteppedSkillDef.InstanceData instanceData = (SteppedSkillDef.InstanceData)(base.skillLocator.GetSkill(SkillSlot.Primary)?.skillInstanceData);
		if (instanceData != null)
		{
			instanceData.step = 0;
			club = 0;
			resetClubSwing = true;
		}
	}

	protected override void PlayAnimation()
	{
		string animationStateName = ((club == 0) ? "SwingClubRight" : "SwingClubLeft");
		float num = Mathf.Max(duration, 0.2f);
		PlayCrossfade("Gesture, Additive", animationStateName, "SwingClub.playbackRate", num, 0.1f);
		PlayCrossfade("Gesture, Override", animationStateName, "SwingClub.playbackRate", num, 0.1f);
	}

	protected override void BeginMeleeAttackEffect()
	{
		swingEffectPrefab = ((club == 0) ? swingEffectPrefab : secondarySwingEffectPrefab);
		swingEffectMuzzleString = ((club == 0) ? "SwingRight" : "SwingLeft");
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

	public override void OnExit()
	{
		base.OnExit();
		if (clearOverrides && skillOverrideData != null)
		{
			skillOverrideData.ClearOverrides();
		}
	}

	public override void ModifyNextState(EntityState nextState)
	{
		base.ModifyNextState(nextState);
		if (nextState is ISkillOverrideHandoff skillOverrideHandoff && skillOverrideData != null)
		{
			skillOverrideHandoff.TransferSkillOverride(skillOverrideData);
			clearOverrides = false;
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}

	protected virtual EntityState GetNextStateAuthority()
	{
		return new ClubSwing3();
	}
}
