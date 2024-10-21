using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GrandParent;

public class ChannelSunStart : ChannelSunBase
{
	public static string animLayerName;

	public static string animStateName;

	public static string animPlaybackRateParam;

	public static string beginSoundName;

	public static float baseDuration;

	public static GameObject beamVfxPrefab;

	private float duration;

	private Vector3? sunSpawnPosition;

	private ParticleSystem leftHandBeamParticleSystem;

	private ParticleSystem rightHandBeamParticleSystem;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation(animLayerName, animStateName, animPlaybackRateParam, duration);
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			AimAnimator component = modelTransform.GetComponent<AimAnimator>();
			if ((bool)component)
			{
				component.enabled = true;
			}
		}
		if (base.isAuthority)
		{
			sunSpawnPosition = sunSpawnPosition ?? ChannelSun.FindSunSpawnPosition(base.transform.position);
		}
		ChildLocator modelChildLocator = GetModelChildLocator();
		if ((bool)modelChildLocator && sunSpawnPosition.HasValue)
		{
			CreateBeamEffect(modelChildLocator, ChannelSunBase.leftHandVfxTargetNameInChildLocator, sunSpawnPosition.Value, ref leftHandBeamParticleSystem);
			CreateBeamEffect(modelChildLocator, ChannelSunBase.rightHandVfxTargetNameInChildLocator, sunSpawnPosition.Value, ref rightHandBeamParticleSystem);
		}
		if (beginSoundName != null)
		{
			Util.PlaySound(beginSoundName, base.gameObject);
		}
	}

	public override void OnExit()
	{
		EndBeamEffect(ref leftHandBeamParticleSystem);
		EndBeamEffect(ref rightHandBeamParticleSystem);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextState(new ChannelSun
			{
				activatorSkillSlot = base.activatorSkillSlot,
				sunSpawnPosition = sunSpawnPosition
			});
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(sunSpawnPosition.HasValue);
		if (sunSpawnPosition.HasValue)
		{
			writer.Write(sunSpawnPosition.Value);
		}
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		sunSpawnPosition = null;
		if (reader.ReadBoolean())
		{
			sunSpawnPosition = reader.ReadVector3();
		}
	}

	private void CreateBeamEffect(ChildLocator childLocator, string nameInChildLocator, Vector3 targetPosition, ref ParticleSystem dest)
	{
		Transform transform = childLocator.FindChild(nameInChildLocator);
		if ((bool)transform)
		{
			ChildLocator component = Object.Instantiate(beamVfxPrefab, transform).GetComponent<ChildLocator>();
			component.FindChild("EndPoint").SetPositionAndRotation(targetPosition, Quaternion.identity);
			Transform transform2 = component.FindChild("BeamParticles");
			dest = transform2.GetComponent<ParticleSystem>();
		}
		else
		{
			dest = null;
		}
	}

	private void EndBeamEffect(ref ParticleSystem particleSystem)
	{
		if ((bool)particleSystem)
		{
			ParticleSystem.MainModule main = particleSystem.main;
			main.loop = false;
			particleSystem.Stop();
		}
		particleSystem = null;
	}
}
