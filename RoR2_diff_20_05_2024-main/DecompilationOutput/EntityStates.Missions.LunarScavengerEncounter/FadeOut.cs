using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;

namespace EntityStates.Missions.LunarScavengerEncounter;

public class FadeOut : BaseState
{
	public static float delay;

	public static float duration;

	private Run.TimeStamp startTime;

	private Light light;

	private float initialIntensity;

	private float initialAmbientIntensity;

	private Color initialAmbientColor;

	private Color initialFogColor;

	private PostProcessVolume postProcessVolume;

	private bool finished;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			startTime = Run.TimeStamp.now + delay;
		}
		light = GetComponent<ChildLocator>().FindChild("PrimaryLight").GetComponent<Light>();
		initialIntensity = light.intensity;
		initialAmbientIntensity = RenderSettings.ambientIntensity;
		initialAmbientColor = RenderSettings.ambientLight;
		initialFogColor = RenderSettings.fogColor;
		light.GetComponent<FlickerLight>().enabled = false;
		postProcessVolume = GetComponent<PostProcessVolume>();
		postProcessVolume.enabled = true;
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(startTime);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		startTime = reader.ReadTimeStamp();
	}

	public override void Update()
	{
		base.Update();
		float num = Mathf.Clamp01(startTime.timeSince / duration);
		num *= num;
		light.intensity = Mathf.Lerp(initialIntensity, 0f, num);
		RenderSettings.ambientIntensity = Mathf.Lerp(initialAmbientIntensity, 0f, num);
		RenderSettings.ambientLight = Color.Lerp(initialAmbientColor, Color.black, num);
		RenderSettings.fogColor = Color.Lerp(initialFogColor, Color.black, num);
		if (!Mathf.Approximately(postProcessVolume.weight, num))
		{
			PostProcessVolume.DispatchVolumeSettingsChangedEvent();
		}
		postProcessVolume.weight = num;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			FixedUpdateServer();
		}
		if (!(startTime.timeSince > duration))
		{
			return;
		}
		foreach (CharacterBody readOnlyInstances in CharacterBody.readOnlyInstancesList)
		{
			if (readOnlyInstances.hasEffectiveAuthority)
			{
				EntityStateMachine entityStateMachine = EntityStateMachine.FindByCustomName(readOnlyInstances.gameObject, "Body");
				if ((bool)entityStateMachine && !(entityStateMachine.state is Idle))
				{
					entityStateMachine.SetInterruptState(new Idle(), InterruptPriority.Frozen);
				}
			}
		}
	}

	private void FixedUpdateServer()
	{
		if ((startTime + duration).hasPassed && !finished)
		{
			finished = true;
			Run.instance.BeginGameOver(RoR2Content.GameEndings.LimboEnding);
		}
	}
}
