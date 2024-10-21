using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidSurvivor.Vent;

public class VentCorruption : GenericCharacterMain
{
	[SerializeField]
	public float minimumDuration;

	[SerializeField]
	public float maximumDuration;

	[SerializeField]
	public string leftVentEffectChildLocatorEntry;

	[SerializeField]
	public string rightVentEffectChildLocatorEntry;

	[SerializeField]
	public string miniVentEffectChildLocatorEntry;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public string exitSoundString;

	[SerializeField]
	public float animationCrossfadeDuration;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string enterAnimationStateName;

	[SerializeField]
	public string exitAnimationStateName;

	[SerializeField]
	public float hoverVelocity;

	[SerializeField]
	public float hoverAcceleration;

	[SerializeField]
	public float healingPercentagePerSecond;

	[SerializeField]
	public float healingTickRate;

	[SerializeField]
	public float corruptionReductionPerSecond;

	[SerializeField]
	public GameObject crosshairOverridePrefab;

	[SerializeField]
	public float turnSpeed;

	private float healPerTick;

	private float healTickStopwatch;

	private float corruptionReductionPerTick;

	private Vector3 liftVector = Vector3.up;

	private VoidSurvivorController voidSurvivorController;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	private float previousTurnSpeed;

	public override void OnEnter()
	{
		base.OnEnter();
		voidSurvivorController = GetComponent<VoidSurvivorController>();
		voidSurvivorController = GetComponent<VoidSurvivorController>();
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayCrossfade(animationLayerName, enterAnimationStateName, animationCrossfadeDuration);
		healPerTick = base.healthComponent.fullHealth * healingPercentagePerSecond / healingTickRate;
		corruptionReductionPerTick = corruptionReductionPerSecond / healingTickRate;
		FindModelChild(leftVentEffectChildLocatorEntry)?.gameObject.SetActive(value: true);
		FindModelChild(rightVentEffectChildLocatorEntry)?.gameObject.SetActive(value: true);
		FindModelChild(miniVentEffectChildLocatorEntry)?.gameObject.SetActive(value: true);
		if ((bool)crosshairOverridePrefab)
		{
			crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
		}
		if ((bool)base.characterDirection)
		{
			previousTurnSpeed = base.characterDirection.turnSpeed;
			base.characterDirection.turnSpeed = turnSpeed;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		base.characterBody.SetAimTimer(1f);
		if (NetworkServer.active)
		{
			healTickStopwatch += GetDeltaTime();
			if (healTickStopwatch > 1f / healingTickRate)
			{
				healTickStopwatch -= 1f / healingTickRate;
				base.healthComponent.Heal(healPerTick, default(ProcChainMask));
				if ((bool)voidSurvivorController)
				{
					voidSurvivorController.AddCorruption(0f - corruptionReductionPerTick);
				}
			}
		}
		if (!base.isAuthority)
		{
			return;
		}
		if ((bool)base.characterMotor)
		{
			float y = base.characterMotor.velocity.y;
			if (y < hoverVelocity)
			{
				y = Mathf.MoveTowards(y, hoverVelocity, hoverAcceleration * GetDeltaTime());
				base.characterMotor.velocity = new Vector3(base.characterMotor.velocity.x, y, base.characterMotor.velocity.z);
			}
		}
		if (base.fixedAge >= maximumDuration || (base.fixedAge >= minimumDuration && (bool)voidSurvivorController && voidSurvivorController.corruption <= voidSurvivorController.minimumCorruption))
		{
			outer.SetNextStateToMain();
		}
	}

	protected override bool CanExecuteSkill(GenericSkill skillSlot)
	{
		return false;
	}

	public override void OnExit()
	{
		if ((bool)base.characterDirection)
		{
			base.characterDirection.turnSpeed = previousTurnSpeed;
		}
		crosshairOverrideRequest?.Dispose();
		FindModelChild(leftVentEffectChildLocatorEntry)?.gameObject.SetActive(value: false);
		FindModelChild(rightVentEffectChildLocatorEntry)?.gameObject.SetActive(value: false);
		FindModelChild(miniVentEffectChildLocatorEntry)?.gameObject.SetActive(value: false);
		PlayCrossfade(animationLayerName, exitAnimationStateName, animationCrossfadeDuration);
		Util.PlaySound(exitSoundString, base.gameObject);
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
