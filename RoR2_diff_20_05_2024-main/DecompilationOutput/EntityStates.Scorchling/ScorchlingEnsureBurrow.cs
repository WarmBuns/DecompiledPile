using RoR2;
using UnityEngine;

namespace EntityStates.Scorchling;

public class ScorchlingEnsureBurrow : BaseState
{
	[SerializeField]
	public string burrowSoundString;

	[SerializeField]
	public string burrowLoopSoundString;

	[SerializeField]
	public string burrowStopLoopSoundString;

	[SerializeField]
	public GameObject burrowEffectPrefab;

	[SerializeField]
	public float burrowRadius = 1f;

	[SerializeField]
	public float animDurationBurrow = 1f;

	[SerializeField]
	public float burrowAnimationDuration = 1f;

	private ScorchlingController sController;

	private bool waitingForBurrow;

	public override void OnEnter()
	{
		base.OnEnter();
		sController = base.characterBody.GetComponent<ScorchlingController>();
		if (!sController.isBurrowed)
		{
			Util.PlaySound(burrowSoundString, base.gameObject);
			Util.PlaySound(burrowLoopSoundString, base.gameObject);
			EffectManager.SpawnEffect(burrowEffectPrefab, new EffectData
			{
				origin = base.characterBody.footPosition,
				scale = burrowRadius
			}, transmit: true);
			PlayAnimation("FullBody, Override", "Burrow", "Burrow.playbackRate", animDurationBurrow);
			if ((bool)base.characterMotor)
			{
				base.characterMotor.walkSpeedPenaltyCoefficient = 0f;
			}
			if ((bool)base.characterBody)
			{
				base.characterBody.isSprinting = false;
			}
			if ((bool)base.rigidbodyMotor)
			{
				base.rigidbodyMotor.moveVector = Vector3.zero;
			}
			waitingForBurrow = true;
		}
	}

	public override void Update()
	{
		base.Update();
		HandleWaitForBurrow();
	}

	public override void OnExit()
	{
		base.OnExit();
		HandleWaitForBurrow();
	}

	private void HandleWaitForBurrow()
	{
		if (waitingForBurrow && base.age > burrowAnimationDuration)
		{
			sController.Burrow();
			waitingForBurrow = false;
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		if (waitingForBurrow)
		{
			return InterruptPriority.Frozen;
		}
		return InterruptPriority.Skill;
	}
}
