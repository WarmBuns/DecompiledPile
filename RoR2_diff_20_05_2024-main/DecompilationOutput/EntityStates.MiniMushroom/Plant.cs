using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.MiniMushroom;

public class Plant : BaseState
{
	public static float healFraction;

	public static float baseMaxDuration;

	public static float baseMinDuration;

	public static float mushroomRadius;

	public static string healSoundLoop;

	public static string healSoundStop;

	private float maxDuration;

	private float minDuration;

	private GameObject mushroomWard;

	private uint soundID;

	private static int PlantLoopStateHash = Animator.StringToHash("PlantLoop");

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Plant", PlantLoopStateHash);
		maxDuration = baseMaxDuration / attackSpeedStat;
		minDuration = baseMinDuration / attackSpeedStat;
		soundID = Util.PlaySound(healSoundLoop, base.characterBody.modelLocator.modelTransform.gameObject);
		if (NetworkServer.active && mushroomWard == null)
		{
			mushroomWard = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/MiniMushroomWard"), base.characterBody.footPosition, Quaternion.identity);
			mushroomWard.GetComponent<TeamFilter>().teamIndex = base.teamComponent.teamIndex;
			if ((bool)mushroomWard)
			{
				HealingWard component = mushroomWard.GetComponent<HealingWard>();
				component.healFraction = healFraction;
				component.healPoints = 0f;
				component.Networkradius = mushroomRadius;
			}
			NetworkServer.Spawn(mushroomWard);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			bool flag = base.inputBank.moveVector.sqrMagnitude > 0.1f;
			if (base.fixedAge > maxDuration || (base.fixedAge > minDuration && flag))
			{
				outer.SetNextState(new UnPlant());
			}
		}
	}

	public override void OnExit()
	{
		PlayAnimation("Plant", EmptyStateHash);
		AkSoundEngine.StopPlayingID(soundID);
		Util.PlaySound(healSoundStop, base.gameObject);
		if ((bool)mushroomWard)
		{
			EntityState.Destroy(mushroomWard);
		}
		base.OnExit();
	}
}
