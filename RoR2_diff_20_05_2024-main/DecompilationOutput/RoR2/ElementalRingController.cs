using UnityEngine;

namespace RoR2;

public class ElementalRingController : MonoBehaviour
{
	public GameObject elementalRingAvailableObject;

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
			bool flag = characterBody.HasBuff(RoR2Content.Buffs.ElementalRingsReady);
			if (flag != elementalRingAvailableObject.activeSelf)
			{
				elementalRingAvailableObject.SetActive(flag);
			}
		}
	}
}
