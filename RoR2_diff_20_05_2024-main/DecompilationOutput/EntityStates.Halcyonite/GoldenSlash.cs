using RoR2;
using UnityEngine;

namespace EntityStates.Halcyonite;

public class GoldenSlash : BasicMeleeAttack
{
	[SerializeField]
	public GameObject swordPrefab;

	protected override void PlayAnimation()
	{
		ChildLocator component = GetModelTransform().GetComponent<ChildLocator>();
		if ((bool)component && (bool)swordPrefab)
		{
			int childIndex = component.FindChildIndex("SwordPoint");
			EffectData effectData = new EffectData();
			effectData.origin = base.characterBody.transform.position;
			effectData.rotation = Util.QuaternionSafeLookRotation(base.characterBody.transform.forward);
			effectData.SetChildLocatorTransformReference(base.characterBody.transform.gameObject, childIndex);
			EffectManager.SpawnEffect(swordPrefab, effectData, transmit: true);
		}
		PlayCrossfade("FullBody Override", "GoldenSlash", "GoldenSlash.playbackRate", duration, 0.1f);
	}

	public override void OnExit()
	{
		PlayAnimation("FullBody Override", "BufferEmpty");
		base.OnExit();
	}
}
