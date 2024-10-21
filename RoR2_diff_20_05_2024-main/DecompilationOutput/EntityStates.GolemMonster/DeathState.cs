using RoR2;
using UnityEngine;

namespace EntityStates.GolemMonster;

public class DeathState : GenericCharacterDeath
{
	public static GameObject initialDeathExplosionPrefab;

	public override void OnEnter()
	{
		base.OnEnter();
		Transform modelTransform = GetModelTransform();
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		Transform transform = component.FindChild("Head");
		if ((bool)transform && (bool)initialDeathExplosionPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(initialDeathExplosionPrefab))
			{
				Object.Instantiate(initialDeathExplosionPrefab, transform.position, Quaternion.identity).transform.parent = transform;
			}
			else
			{
				EffectManager.GetAndActivatePooledEffect(initialDeathExplosionPrefab, transform.position, Quaternion.identity, transform);
			}
		}
	}
}
