using RoR2;
using UnityEngine;

namespace EntityStates.VoidRaidCrab.Joint;

public class DeathState : GenericCharacterDeath
{
	[SerializeField]
	public string joint1Name;

	[SerializeField]
	public string joint2Name;

	[SerializeField]
	public string joint3Name;

	[SerializeField]
	public GameObject joint1EffectPrefab;

	[SerializeField]
	public GameObject joint2EffectPrefab;

	[SerializeField]
	public GameObject joint3EffectPrefab;

	private CharacterModel characterModel;

	public override void OnEnter()
	{
		base.OnEnter();
		EffectManager.SimpleMuzzleFlash(joint1EffectPrefab, base.gameObject, joint1Name, transmit: false);
		EffectManager.SimpleMuzzleFlash(joint2EffectPrefab, base.gameObject, joint2Name, transmit: false);
		EffectManager.SimpleMuzzleFlash(joint3EffectPrefab, base.gameObject, joint3Name, transmit: false);
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			characterModel = modelTransform.GetComponent<CharacterModel>();
		}
		if ((bool)characterModel)
		{
			characterModel.invisibilityCount++;
		}
	}

	public override void OnExit()
	{
		if ((bool)characterModel)
		{
			characterModel.invisibilityCount--;
		}
		base.OnExit();
	}
}
