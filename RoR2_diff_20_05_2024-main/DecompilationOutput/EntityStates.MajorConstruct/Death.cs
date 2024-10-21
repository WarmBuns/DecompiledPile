using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.MajorConstruct;

public class Death : GenericCharacterDeath
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public GameObject beginEffect;

	[SerializeField]
	public string beginMuzzleName;

	[SerializeField]
	public GameObject padEffect;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	protected override bool shouldAutoDestroy => false;

	protected override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
	{
		PlayAnimation(animationLayerName, animationStateName);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)beginEffect)
		{
			EffectManager.SimpleMuzzleFlash(beginEffect, base.gameObject, beginMuzzleName, transmit: false);
		}
		FindModelChild("Collision").gameObject.SetActive(value: false);
		MasterSpawnSlotController component = GetComponent<MasterSpawnSlotController>();
		if (NetworkServer.active)
		{
			_ = (bool)component;
		}
	}
}
