using RoR2;
using UnityEngine;

namespace EntityStates.VagrantNovaItem;

public class BaseVagrantNovaItemState : BaseBodyAttachmentState
{
	protected ParticleSystem chargeSparks;

	protected int GetItemStack()
	{
		if (!base.attachedBody || !base.attachedBody.inventory)
		{
			return 1;
		}
		return base.attachedBody.inventory.GetItemCount(RoR2Content.Items.NovaOnLowHealth);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		ChildLocator component = GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		chargeSparks = component.FindChild("ChargeSparks")?.GetComponent<ParticleSystem>();
		if ((bool)chargeSparks)
		{
			ParticleSystem.ShapeModule shape = chargeSparks.shape;
			SkinnedMeshRenderer skinnedMeshRenderer = FindAttachedBodyMainRenderer();
			if ((bool)skinnedMeshRenderer)
			{
				shape.skinnedMeshRenderer = FindAttachedBodyMainRenderer();
				ParticleSystem.MainModule main = chargeSparks.main;
				float x = skinnedMeshRenderer.transform.lossyScale.x;
				main.startSize = 0.5f / x;
			}
		}
	}

	protected void SetChargeSparkEmissionRateMultiplier(float multiplier)
	{
		if ((bool)chargeSparks)
		{
			ParticleSystem.EmissionModule emission = chargeSparks.emission;
			emission.rateOverTimeMultiplier = multiplier * 20f;
		}
	}

	private SkinnedMeshRenderer FindAttachedBodyMainRenderer()
	{
		if (!base.attachedBody)
		{
			return null;
		}
		CharacterModel.RendererInfo[] array = base.attachedBody.modelLocator?.modelTransform.GetComponent<CharacterModel>()?.baseRendererInfos;
		if (array == null)
		{
			return null;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].renderer is SkinnedMeshRenderer result)
			{
				return result;
			}
		}
		return null;
	}
}
