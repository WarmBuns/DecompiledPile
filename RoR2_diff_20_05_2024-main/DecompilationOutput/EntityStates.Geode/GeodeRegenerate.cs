using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Geode;

public class GeodeRegenerate : GeodeEntityStates
{
	private new float age;

	public override void OnEnter()
	{
		base.OnEnter();
		geodeModel.SetActive(value: true);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		age += Time.deltaTime;
		float printThreshold = geodeController.printCurve.Evaluate(age / geodeController.printTime);
		SetPrintThreshold(printThreshold);
		if (age >= geodeController.idleVFXRegenerationTimer && !geodeIdleVFX.activeInHierarchy)
		{
			geodeIdleVFX.SetActive(value: true);
		}
		if (age >= geodeController.printTime)
		{
			if (NetworkServer.active)
			{
				geodeController.SetAvailable(activeState: true);
			}
			outer.SetNextState(new GeodeEntityStates());
		}
	}

	private void SetPrintThreshold(float sample)
	{
		float num = 1f - sample;
		float value = sample * geodeController.maxPrintHeight + num * geodeController.startingPrintHeight;
		geodeTrunkRenderer.material.SetFloat(sliceHeightNameId, value);
	}

	public override void OnExit()
	{
		base.OnExit();
		age = 0f;
	}
}
