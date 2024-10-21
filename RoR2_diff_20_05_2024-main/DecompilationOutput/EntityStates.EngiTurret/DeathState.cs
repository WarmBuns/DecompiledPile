using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.EngiTurret;

public class DeathState : GenericCharacterDeath
{
	[SerializeField]
	public GameObject initialExplosion;

	[SerializeField]
	public GameObject deathExplosion;

	private float deathDuration;

	protected override bool shouldAutoDestroy => false;

	protected override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
	{
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			int layerIndex = modelAnimator.GetLayerIndex("Body");
			modelAnimator.PlayInFixedTime("Death", layerIndex);
			modelAnimator.Update(0f);
			deathDuration = modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).length;
			if ((bool)initialExplosion)
			{
				Object.Instantiate(initialExplosion, base.transform.position, base.transform.rotation, base.transform);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > deathDuration && NetworkServer.active && (bool)deathExplosion)
		{
			EffectManager.SpawnEffect(deathExplosion, new EffectData
			{
				origin = base.transform.position,
				scale = 2f
			}, transmit: true);
			EntityState.Destroy(base.gameObject);
		}
	}

	public override void OnExit()
	{
		DestroyModel();
		base.OnExit();
	}
}
