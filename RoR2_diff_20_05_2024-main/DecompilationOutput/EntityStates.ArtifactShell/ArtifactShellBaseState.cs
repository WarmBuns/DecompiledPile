using RoR2;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace EntityStates.ArtifactShell;

public class ArtifactShellBaseState : BaseState
{
	protected Light light;

	private string stopLoopSound;

	protected PurchaseInteraction purchaseInteraction { get; private set; }

	protected virtual CostTypeIndex interactionCostType => CostTypeIndex.None;

	protected virtual bool interactionAvailable => false;

	protected virtual int interactionCost => 0;

	protected ParticleSystem rayParticleSystem { get; private set; }

	protected PostProcessVolume postProcessVolume { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		purchaseInteraction = GetComponent<PurchaseInteraction>();
		if ((object)purchaseInteraction != null)
		{
			purchaseInteraction.costType = interactionCostType;
			purchaseInteraction.Networkcost = interactionCost;
			purchaseInteraction.Networkavailable = interactionAvailable;
			purchaseInteraction.onPurchase.AddListener(DoOnPurchase);
		}
		rayParticleSystem = FindModelChild("RayParticles")?.GetComponent<ParticleSystem>();
		postProcessVolume = FindModelChild("PP")?.GetComponent<PostProcessVolume>();
		light = FindModelChild("Light").GetComponent<Light>();
		CalcLoopSounds(base.healthComponent.combinedHealthFraction, out var startLoopSound, out stopLoopSound);
		Util.PlaySound(startLoopSound, base.gameObject);
	}

	private static void CalcLoopSounds(float currentHealthFraction, out string startLoopSound, out string stopLoopSound)
	{
		startLoopSound = null;
		stopLoopSound = null;
		float num = 0.05f;
		if (currentHealthFraction > 0.75f + num)
		{
			startLoopSound = "Play_artifactBoss_loop_level1";
			stopLoopSound = "Stop_artifactBoss_loop_level1";
		}
		else if (currentHealthFraction > 0.25f + num)
		{
			startLoopSound = "Play_artifactBoss_loop_level2";
			stopLoopSound = "Stop_artifactBoss_loop_level2";
		}
		else if (currentHealthFraction > 0f + num)
		{
			startLoopSound = "Play_artifactBoss_loop_level2";
			stopLoopSound = "Stop_artifactBoss_loop_level2";
		}
	}

	public override void OnExit()
	{
		Util.PlaySound(stopLoopSound, base.gameObject);
		if ((object)purchaseInteraction != null)
		{
			purchaseInteraction.onPurchase.RemoveListener(DoOnPurchase);
			purchaseInteraction = null;
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		UpdateVisuals();
	}

	private void DoOnPurchase(Interactor activator)
	{
		OnPurchase(activator);
	}

	protected virtual void OnPurchase(Interactor activator)
	{
	}

	protected void UpdateVisuals()
	{
		float num = 1f - base.healthComponent.combinedHealthFraction;
		if ((bool)rayParticleSystem)
		{
			ParticleSystem.EmissionModule emission = rayParticleSystem.emission;
			emission.rateOverTime = Util.Remap(num, 0f, 1f, 0f, 10f);
			ParticleSystem.MainModule main = rayParticleSystem.main;
			main.simulationSpeed = Util.Remap(num, 0f, 1f, 1f, 5f);
		}
		if ((bool)postProcessVolume)
		{
			if (!Mathf.Approximately(postProcessVolume.weight, num))
			{
				PostProcessVolume.DispatchVolumeSettingsChangedEvent();
			}
			postProcessVolume.weight = num;
		}
		if ((bool)light)
		{
			light.range = 10f + num * 150f;
		}
	}
}
