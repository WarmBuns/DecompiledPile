using RoR2;
using UnityEngine;

namespace EntityStates.TitanMonster;

public class SpawnState : BaseState
{
	public static float duration = 4f;

	public static GameObject burrowPrefab;

	public static string spawnSoundString;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	private static int SpawnParamHash = Animator.StringToHash("Spawn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(spawnSoundString, base.gameObject);
		ChildLocator component = GetModelTransform().GetComponent<ChildLocator>();
		PlayAnimation("Body", SpawnStateHash, SpawnParamHash, duration);
		Transform transform = component.FindChild("BurrowCenter");
		if ((bool)transform)
		{
			if (!EffectManager.ShouldUsePooledEffect(burrowPrefab))
			{
				Object.Instantiate(burrowPrefab, transform.position, Quaternion.identity);
			}
			else
			{
				EffectManager.GetAndActivatePooledEffect(burrowPrefab, transform.position, Quaternion.identity);
			}
		}
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
