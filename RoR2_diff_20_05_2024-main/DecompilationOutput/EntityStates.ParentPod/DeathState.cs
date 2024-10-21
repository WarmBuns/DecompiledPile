using RoR2;
using UnityEngine;

namespace EntityStates.ParentPod;

public class DeathState : GenericCharacterDeath
{
	public static float deathAnimTimer;

	private float mDeathAnimTimer;

	public static GameObject deathEffect;

	private bool printingStarted;

	public override void OnEnter()
	{
		base.OnEnter();
		mDeathAnimTimer = deathAnimTimer;
		EffectManager.SimpleEffect(deathEffect, base.gameObject.transform.position, base.transform.rotation, transmit: false);
	}

	public override void FixedUpdate()
	{
		mDeathAnimTimer -= Time.deltaTime;
		if (mDeathAnimTimer <= 0f && !printingStarted)
		{
			printingStarted = true;
			PrintController printController = GetComponent<ModelLocator>().modelTransform.gameObject.AddComponent<PrintController>();
			printController.enabled = false;
			printController.printTime = 1f;
			printController.startingPrintHeight = 99999f;
			printController.maxPrintHeight = 99999f;
			printController.startingPrintBias = 0.95f;
			printController.maxPrintBias = 1.95f;
			printController.animateFlowmapPower = true;
			printController.startingFlowmapPower = 1.14f;
			printController.maxFlowmapPower = 30f;
			printController.disableWhenFinished = false;
			printController.printCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
			printController.enabled = true;
		}
		if (printingStarted)
		{
			base.FixedUpdate();
		}
	}
}
