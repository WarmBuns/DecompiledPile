using UnityEngine;

namespace RoR2;

public class TeslaCoilAnimator : MonoBehaviour
{
	public GameObject activeEffectParent;

	private CharacterBody characterBody;

	private void Start()
	{
		CharacterModel componentInParent = GetComponentInParent<CharacterModel>();
		if ((bool)componentInParent)
		{
			characterBody = componentInParent.body;
		}
	}

	private void FixedUpdate()
	{
		if ((bool)characterBody)
		{
			activeEffectParent.SetActive(characterBody.HasBuff(RoR2Content.Buffs.TeslaField));
		}
	}
}
