using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Scorchling;

public class DeathState : GenericCharacterDeath
{
	public static GameObject deathExplosion;

	public static string deathSoundString;

	public static string stopBurrowLoopString;

	public static float animDuration = 1f;

	public static float cleanUpDuration = 0.3f;

	public static float printDuration;

	private bool burrowed;

	protected override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
	{
		PlayAnimation("FullBody, Override", "Burrow", "Burrow.playbackRate", animDuration);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			PrintController printController = modelTransform.gameObject.AddComponent<PrintController>();
			printController.printTime = printDuration;
			printController.enabled = true;
			printController.startingPrintHeight = 99999f;
			printController.maxPrintHeight = 99999f;
			printController.startingPrintBias = 1f;
			printController.maxPrintBias = 3.5f;
			printController.animateFlowmapPower = true;
			printController.startingFlowmapPower = 1.14f;
			printController.maxFlowmapPower = 30f;
			printController.disableWhenFinished = false;
			printController.printCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		}
		cleanUpDuration += animDuration;
		Util.PlaySound(deathSoundString, base.gameObject);
		Util.PlaySound(stopBurrowLoopString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!burrowed && base.fixedAge >= animDuration)
		{
			EffectManager.SpawnEffect(deathExplosion, new EffectData
			{
				origin = base.characterBody.corePosition,
				scale = 4f
			}, transmit: true);
			burrowed = true;
		}
		if (base.fixedAge >= cleanUpDuration)
		{
			DestroyModel();
			if (NetworkServer.active)
			{
				DestroyBodyAsapServer();
			}
		}
	}

	public override void OnExit()
	{
		DestroyModel();
		base.OnExit();
	}
}
