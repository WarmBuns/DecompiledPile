using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.LunarWisp;

public class DeathState : GenericCharacterDeath
{
	public static GameObject deathEffectPrefab;

	public static string deathEffectMuzzleName;

	public static float velocityMagnitude;

	public static float explosionForce;

	private LunarWispFXController FXController;

	protected override bool shouldAutoDestroy => false;

	public override void OnEnter()
	{
		base.OnEnter();
		LunarWispFXController component = base.characterBody.GetComponent<LunarWispFXController>();
		if ((bool)component)
		{
			component.TurnOffFX();
		}
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			RagdollController component2 = modelTransform.GetComponent<RagdollController>();
			Rigidbody component3 = GetComponent<Rigidbody>();
			if ((bool)component2 && (bool)component3)
			{
				component2.BeginRagdoll(component3.velocity * velocityMagnitude);
			}
			ExplodeRigidbodiesOnStart component4 = modelTransform.GetComponent<ExplodeRigidbodiesOnStart>();
			if ((bool)component4)
			{
				component4.force = explosionForce;
				component4.enabled = true;
			}
		}
		if ((bool)base.modelLocator)
		{
			base.modelLocator.autoUpdateModelTransform = false;
		}
		FindModelChild("StandableSurface").gameObject.SetActive(value: false);
		if (NetworkServer.active)
		{
			EffectData effectData = new EffectData
			{
				origin = FindModelChild(deathEffectMuzzleName).position
			};
			EffectManager.SpawnEffect(deathEffectPrefab, effectData, transmit: true);
			DestroyBodyAsapServer();
		}
	}
}
