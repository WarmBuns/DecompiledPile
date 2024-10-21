using System;
using HG;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2.CharacterAI;

public class AISkillDriver : MonoBehaviour
{
	public enum TargetType
	{
		CurrentEnemy,
		NearestFriendlyInSkillRange,
		CurrentLeader,
		Custom
	}

	public enum AimType
	{
		None,
		AtMoveTarget,
		AtCurrentEnemy,
		AtCurrentLeader,
		MoveDirection
	}

	public enum MovementType
	{
		Stop,
		ChaseMoveTarget,
		StrafeMovetarget,
		FleeMoveTarget
	}

	public enum ButtonPressType
	{
		Hold,
		Abstain,
		TapContinuous
	}

	[Tooltip("The name of this skill driver for reference purposes.")]
	public string customName;

	[Tooltip("The slot of the associated skill. Set to None to allow this behavior to run regardless of skill availability.")]
	public SkillSlot skillSlot;

	[Header("Selection Conditions")]
	[Tooltip("The skill that the specified slot must have for this behavior to run. Set to none to allow any skill.")]
	public SkillDef requiredSkill;

	[Tooltip("If set, this cannot be the dominant driver while the skill is on cooldown or out of stock.")]
	public bool requireSkillReady;

	[Tooltip("If set, this cannot be the dominant driver while the equipment is on cooldown or out of stock.")]
	public bool requireEquipmentReady;

	[Tooltip("The minimum health fraction required of the user for this behavior.")]
	public float minUserHealthFraction = float.NegativeInfinity;

	[Tooltip("The maximum health fraction required of the user for this behavior.")]
	public float maxUserHealthFraction = float.PositiveInfinity;

	[Tooltip("The minimum health fraction required of the target for this behavior.")]
	public float minTargetHealthFraction = float.NegativeInfinity;

	[Tooltip("The maximum health fraction required of the target for this behavior.")]
	public float maxTargetHealthFraction = float.PositiveInfinity;

	[Tooltip("The minimum distance from the target required for this behavior.")]
	public float minDistance;

	[Tooltip("The maximum distance from the target required for this behavior.")]
	public float maxDistance = float.PositiveInfinity;

	public bool selectionRequiresTargetLoS;

	public bool selectionRequiresOnGround;

	public bool selectionRequiresAimTarget;

	[Tooltip("The maximum number of times that this skill can be selected.  If the value is < 0, then there is no maximum.")]
	public int maxTimesSelected = -1;

	[FormerlySerializedAs("targetType")]
	[Header("Behavior")]
	[Tooltip("The type of object targeted for movement.")]
	public TargetType moveTargetType;

	[Tooltip("If set, this skill will not be activated unless there is LoS to the target.")]
	public bool activationRequiresTargetLoS;

	[Tooltip("If set, this skill will not be activated unless there is LoS to the aim target.")]
	public bool activationRequiresAimTargetLoS;

	[Tooltip("If set, this skill will not be activated unless the aim vector is pointing close to the target.")]
	public bool activationRequiresAimConfirmation;

	[Tooltip("The movement type to use while this is the dominant skill driver.")]
	public MovementType movementType = MovementType.ChaseMoveTarget;

	public float moveInputScale = 1f;

	[Tooltip("Where to look while this is the dominant skill driver")]
	public AimType aimType = AimType.AtMoveTarget;

	[Tooltip("If set, the nodegraph will not be used to direct the local navigator while this is the dominant skill driver. Direction toward the target will be used instead.")]
	public bool ignoreNodeGraph;

	[Tooltip("If true, the AI will attempt to sprint while this is the dominant skill driver.")]
	public bool shouldSprint;

	public bool shouldFireEquipment;

	[Obsolete("Use buttonPressType instead.")]
	[ShowFieldObsolete]
	public bool shouldTapButton;

	public ButtonPressType buttonPressType;

	[Header("Transition Behavior")]
	[Tooltip("If non-negative, this value will be used for the driver evaluation timer while this is the dominant skill driver.")]
	public float driverUpdateTimerOverride = -1f;

	[Tooltip("If set and this is the dominant skill driver, the current enemy will be reset at the time of the next evaluation.")]
	public bool resetCurrentEnemyOnNextDriverSelection;

	[Tooltip("If true, this skill driver cannot be chosen twice in a row.")]
	public bool noRepeat;

	[Tooltip("The AI skill driver that will be treated as having top priority after this one.")]
	public AISkillDriver nextHighPriorityOverride;

	public float minDistanceSqr => minDistance * minDistance;

	public float maxDistanceSqr => maxDistance * maxDistance;

	public int timesSelected { get; private set; }

	private void OnValidate()
	{
		if (shouldTapButton)
		{
			buttonPressType = ButtonPressType.TapContinuous;
		}
	}

	private void OnEnable()
	{
	}

	public void OnSelected()
	{
		int num = timesSelected + 1;
		timesSelected = num;
	}
}
