using RoR2;
using UnityEngine;

namespace EntityStates.VoidMegaCrab;

public class SpawnState : BaseState
{
	[SerializeField]
	public float duration = 4f;

	[SerializeField]
	public string spawnSoundString;

	[SerializeField]
	public GameObject spawnEffectPrefab;

	[SerializeField]
	public string spawnMuzzleName;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		Util.PlaySound(spawnSoundString, base.gameObject);
		if ((bool)spawnEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(spawnEffectPrefab, base.gameObject, spawnMuzzleName, transmit: false);
		}
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			modelTransform.GetComponent<PrintController>().enabled = true;
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
