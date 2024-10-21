using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.MajorConstruct;

public class SpawnMinorConstructs : BaseState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public int numToSpawn;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public GameObject muzzleEffectPrefab;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public string enterSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		MasterSpawnSlotController component = GetComponent<MasterSpawnSlotController>();
		if (NetworkServer.active && (bool)component)
		{
			component.SpawnRandomOpen(numToSpawn, Run.instance.stageRng, base.gameObject);
		}
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
