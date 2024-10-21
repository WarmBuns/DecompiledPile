using RoR2;
using UnityEngine;

namespace EntityStates.Vulture;

public class VultureModeState : BaseSkillState
{
	[SerializeField]
	public float mecanimTransitionDuration;

	[SerializeField]
	public float flyOverrideMecanimLayerWeight;

	[SerializeField]
	public float movementSpeedMultiplier;

	[SerializeField]
	public string enterSoundString;

	protected Animator animator;

	protected int flyOverrideLayer;

	protected ICharacterGravityParameterProvider characterGravityParameterProvider;

	protected ICharacterFlightParameterProvider characterFlightParameterProvider;

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		characterGravityParameterProvider = base.gameObject.GetComponent<ICharacterGravityParameterProvider>();
		characterFlightParameterProvider = base.gameObject.GetComponent<ICharacterFlightParameterProvider>();
		if ((bool)animator)
		{
			flyOverrideLayer = animator.GetLayerIndex("FlyOverride");
		}
		if ((bool)base.characterMotor)
		{
			base.characterMotor.walkSpeedPenaltyCoefficient = movementSpeedMultiplier;
		}
		if ((bool)base.modelLocator)
		{
			base.modelLocator.normalizeToFloor = false;
		}
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void Update()
	{
		base.Update();
		if ((bool)animator)
		{
			animator.SetLayerWeight(flyOverrideLayer, Util.Remap(Mathf.Clamp01(base.age / mecanimTransitionDuration), 0f, 1f, 1f - flyOverrideMecanimLayerWeight, flyOverrideMecanimLayerWeight));
		}
	}

	public override void OnExit()
	{
		if ((bool)base.characterMotor)
		{
			base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
		}
		base.OnExit();
	}
}
