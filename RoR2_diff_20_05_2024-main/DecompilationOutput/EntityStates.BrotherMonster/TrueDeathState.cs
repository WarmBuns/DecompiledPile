using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.BrotherMonster;

public class TrueDeathState : GenericCharacterDeath
{
	public static float durationBeforeDissolving;

	public static float dissolveDuration;

	public static GameObject deathEffectPrefab;

	private static int TrueDeathStateHash = Animator.StringToHash("TrueDeath");

	private bool dissolving;

	protected override bool shouldAutoDestroy => base.fixedAge > durationBeforeDissolving + dissolveDuration + 1f;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			Util.CleanseBody(base.characterBody, removeDebuffs: true, removeBuffs: true, removeCooldownBuffs: true, removeDots: true, removeStun: false, removeNearbyProjectiles: false);
			ReturnStolenItemsOnGettingHit component = GetComponent<ReturnStolenItemsOnGettingHit>();
			if ((bool)component && (bool)component.itemStealController)
			{
				EntityState.Destroy(component.itemStealController.gameObject);
			}
		}
	}

	protected override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
	{
		PlayAnimation("FullBody Override", TrueDeathStateHash);
		base.characterDirection.moveVector = base.characterDirection.forward;
		EffectManager.SimpleMuzzleFlash(deathEffectPrefab, base.gameObject, "MuzzleCenter", transmit: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= durationBeforeDissolving)
		{
			Dissolve();
		}
	}

	private void Dissolve()
	{
		if (!dissolving)
		{
			dissolving = true;
			Transform modelTransform = GetModelTransform();
			if ((bool)modelTransform)
			{
				modelTransform.gameObject.GetComponent<CharacterModel>();
				PrintController component = base.modelLocator.modelTransform.gameObject.GetComponent<PrintController>();
				component.enabled = false;
				component.printTime = dissolveDuration;
				component.enabled = true;
			}
			Transform transform = FindModelChild("TrueDeathEffect");
			if ((bool)transform)
			{
				transform.gameObject.SetActive(value: true);
				transform.GetComponent<ScaleParticleSystemDuration>().newDuration = dissolveDuration;
			}
		}
	}
}
