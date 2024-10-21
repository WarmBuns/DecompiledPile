using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.CaptainSupplyDrop;

public class HackingInProgressState : BaseMainState
{
	public static int baseGoldForBaseDuration = 25;

	public static float baseDuration = 15f;

	public static GameObject targetIndicatorVfxPrefab;

	public PurchaseInteraction target;

	private GameObject targetIndicatorVfxInstance;

	private Run.FixedTimeStamp startTime;

	private Run.FixedTimeStamp endTime;

	protected override bool shouldShowEnergy => true;

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			int difficultyScaledCost = Run.instance.GetDifficultyScaledCost(baseGoldForBaseDuration, Stage.instance.entryDifficultyCoefficient);
			float num = (float)((double)target.cost / (double)difficultyScaledCost * (double)baseDuration);
			startTime = Run.FixedTimeStamp.now;
			endTime = startTime + num;
		}
		energyComponent.normalizedChargeRate = 1f / (endTime - startTime);
		if (NetworkServer.active)
		{
			energyComponent.energy = 0f;
		}
		if (!targetIndicatorVfxPrefab || !target)
		{
			return;
		}
		targetIndicatorVfxInstance = Object.Instantiate(targetIndicatorVfxPrefab, target.transform.position, Quaternion.identity);
		ChildLocator component = targetIndicatorVfxInstance.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			Transform transform = component.FindChild("LineEnd");
			if ((bool)transform)
			{
				transform.position = FindModelChild("ShaftTip").position;
			}
		}
	}

	public override void OnExit()
	{
		if ((bool)targetIndicatorVfxInstance)
		{
			EntityState.Destroy(targetIndicatorVfxInstance);
			targetIndicatorVfxInstance = null;
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			if (energyComponent.normalizedEnergy >= 1f)
			{
				outer.SetNextState(new UnlockTargetState
				{
					target = target
				});
			}
			else if (!HackingMainState.PurchaseInteractionIsValidTarget(target))
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(target ? target.gameObject : null);
		writer.Write(startTime);
		writer.Write(endTime);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		GameObject gameObject = reader.ReadGameObject();
		target = (gameObject ? gameObject.GetComponent<PurchaseInteraction>() : null);
		startTime = reader.ReadFixedTimeStamp();
		endTime = reader.ReadFixedTimeStamp();
	}
}
