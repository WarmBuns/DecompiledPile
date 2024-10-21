using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ShrineRebirth;

public class RebirthChoice : ShrineRebirthEntityStates
{
	public float rebirthGameEndingTransition = 15f;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound("Play_obj_shrineColossus_locked_loop", base.gameObject);
		Transform transform = _shrineController.childLocator.FindChild("TriggeredVFXSpawnPoint");
		GameObject effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/RebirthOnRebirthVFX");
		EffectData effectData = new EffectData
		{
			origin = transform.position,
			genericFloat = 1.5f
		};
		effectData.SetNetworkedObjectReference(base.gameObject);
		int childIndex = _shrineController.childLocator.FindChildIndex("TriggeredVFXSpawnPoint");
		effectData.SetChildLocatorTransformReference(base.gameObject, childIndex);
		if (NetworkServer.active)
		{
			EffectManager.SpawnEffect(effectPrefab, effectData, transmit: true);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		_ = base.fixedAge;
		_ = rebirthGameEndingTransition;
	}
}
