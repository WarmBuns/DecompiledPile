using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(ItemFollower))]
public class TalismanAnimator : MonoBehaviour
{
	private float lastCooldownTimer;

	private EquipmentSlot equipmentSlot;

	private ItemFollower itemFollower;

	private ParticleSystem[] killEffects;

	private void Start()
	{
		itemFollower = GetComponent<ItemFollower>();
		CharacterModel componentInParent = GetComponentInParent<CharacterModel>();
		if ((bool)componentInParent)
		{
			CharacterBody body = componentInParent.body;
			if ((bool)body)
			{
				equipmentSlot = body.equipmentSlot;
			}
		}
	}

	private void FixedUpdate()
	{
		if (!equipmentSlot)
		{
			return;
		}
		float cooldownTimer = equipmentSlot.cooldownTimer;
		if (lastCooldownTimer - cooldownTimer >= 0.5f && (bool)itemFollower.followerInstance)
		{
			if (killEffects == null || killEffects.Length == 0 || killEffects[0] == null)
			{
				killEffects = itemFollower.followerInstance.GetComponentsInChildren<ParticleSystem>();
			}
			ParticleSystem[] array = killEffects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Play();
			}
		}
		lastCooldownTimer = cooldownTimer;
	}
}
