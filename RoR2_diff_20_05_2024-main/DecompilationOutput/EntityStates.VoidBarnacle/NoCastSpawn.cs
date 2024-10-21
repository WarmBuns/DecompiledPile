using RoR2;
using UnityEngine;

namespace EntityStates.VoidBarnacle;

public class NoCastSpawn : BaseState
{
	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateName;

	[SerializeField]
	public float duration;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public GameObject spawnFXPrefab;

	[SerializeField]
	public string spawnFXTransformName;

	public override void OnEnter()
	{
		base.OnEnter();
		if (spawnFXPrefab != null)
		{
			EffectManager.SimpleMuzzleFlash(spawnFXPrefab, base.gameObject, spawnFXTransformName, transmit: false);
		}
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateName, duration);
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
}
