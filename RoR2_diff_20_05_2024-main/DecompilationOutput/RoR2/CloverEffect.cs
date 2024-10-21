using UnityEngine;

namespace RoR2;

public class CloverEffect : MonoBehaviour
{
	public GameObject triggerEffect;

	private CharacterBody characterBody;

	private GameObject triggerEffectInstance;

	private bool trigger;

	private void Start()
	{
		CharacterBody body = GetComponentInParent<CharacterModel>().body;
		characterBody = body.GetComponent<CharacterBody>();
	}

	private void FixedUpdate()
	{
		if ((bool)characterBody && characterBody.wasLucky)
		{
			characterBody.wasLucky = false;
			EffectData effectData = new EffectData();
			effectData.origin = base.transform.position;
			effectData.rotation = base.transform.rotation;
			EffectManager.SpawnEffect(triggerEffect, effectData, transmit: true);
		}
	}
}
