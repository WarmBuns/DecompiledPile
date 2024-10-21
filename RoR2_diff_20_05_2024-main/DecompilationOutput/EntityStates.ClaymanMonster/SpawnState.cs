using RoR2;
using UnityEngine;

namespace EntityStates.ClaymanMonster;

public class SpawnState : BaseState
{
	public static float duration;

	public static string spawnSoundString;

	public static GameObject spawnEffectPrefab;

	public static string spawnEffectChildString;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	private static int SpawnParamHash = Animator.StringToHash("Spawn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		ChildLocator component = GetModelTransform().GetComponent<ChildLocator>();
		if ((bool)component)
		{
			Transform transform = component.FindChild(spawnEffectChildString);
			if ((bool)transform)
			{
				Object.Instantiate(spawnEffectPrefab, transform.position, Quaternion.identity);
			}
		}
		if (spawnSoundString.Length > 0)
		{
			Util.PlaySound(spawnSoundString, base.gameObject);
		}
		PlayAnimation("Body", SpawnStateHash, SpawnParamHash, duration);
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
		return InterruptPriority.Death;
	}
}
