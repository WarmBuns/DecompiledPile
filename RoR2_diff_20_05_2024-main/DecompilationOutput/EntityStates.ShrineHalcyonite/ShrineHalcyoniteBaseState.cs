using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ShrineHalcyonite;

public class ShrineHalcyoniteBaseState : BaseState
{
	protected HalcyoniteShrineInteractable parentShrineReference;

	protected GameObject crystalObject;

	protected ParticleSystem ps;

	protected GameObject shrineHalcyoniteBubble;

	protected GameObject shrineBase;

	private Xoroshiro128Plus rng;

	private GameObject modelBase;

	protected bool visualsDone;

	protected float timer;

	private int sliceHeightNameId;

	private Renderer shrineBaseRenderer;

	public override void OnEnter()
	{
		base.OnEnter();
		parentShrineReference = GetComponent<HalcyoniteShrineInteractable>();
		if ((bool)parentShrineReference)
		{
			shrineHalcyoniteBubble = parentShrineReference.shrineHalcyoniteBubble;
			crystalObject = parentShrineReference.crystalObject;
			crystalObject.GetComponent<Renderer>();
			shrineBase = parentShrineReference.shrineBaseObject;
		}
		shrineBaseRenderer = parentShrineReference.shrineGoldTop.GetComponent<Renderer>();
		shrineBaseRenderer.material.EnableKeyword("_SLICEHEIGHT");
		int propertyIndex = shrineBaseRenderer.material.shader.FindPropertyIndex("_SliceHeight");
		sliceHeightNameId = shrineBaseRenderer.material.shader.GetPropertyNameId(propertyIndex);
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	protected void TierChange(float tierChangeVFXModifier)
	{
		Transform transform = parentShrineReference.modelChildLocator.FindChild("ModelBase");
		if ((bool)transform)
		{
			modelBase = transform.gameObject;
		}
		Vector3 origin = modelBase.gameObject.transform.position + new Vector3(0f, tierChangeVFXModifier, 0f);
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/HalcyonShrineTierReachVFX"), new EffectData
		{
			origin = origin
		}, transmit: false);
		Util.PlaySound("Play_obj_shrineHalcyonite_tierChange", base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		parentShrineReference.shrineGoldTop.GetComponent<Renderer>().material.SetFloat(sliceHeightNameId, parentShrineReference.goldMaterialModifier);
	}

	protected void ModifyVisuals()
	{
		if (!NetworkServer.active || visualsDone || !parentShrineReference.isDraining)
		{
			return;
		}
		float num = 4.1f;
		float num2 = 3.5f;
		float num3 = 4f;
		float num4 = (float)parentShrineReference.goldDrainValue * num / (float)parentShrineReference.lowGoldCost;
		if (parentShrineReference.goldDrained < parentShrineReference.lowGoldCost || parentShrineReference.goldMaterialModifier < 1.9f)
		{
			HalcyoniteShrineInteractable halcyoniteShrineInteractable = parentShrineReference;
			halcyoniteShrineInteractable.NetworkgoldMaterialModifier = halcyoniteShrineInteractable.goldMaterialModifier + num4;
		}
		if ((parentShrineReference.goldDrained > parentShrineReference.lowGoldCost && parentShrineReference.goldDrained < parentShrineReference.midGoldCost) || (parentShrineReference.goldMaterialModifier < 6f && parentShrineReference.goldMaterialModifier >= 1.9f))
		{
			num4 = (float)parentShrineReference.goldDrainValue * num2 / (float)(parentShrineReference.midGoldCost - parentShrineReference.lowGoldCost);
			if (parentShrineReference.goldMaterialModifier >= 5f)
			{
				num4 = (float)parentShrineReference.goldDrainValue * num3 / (float)(parentShrineReference.maxGoldCost - parentShrineReference.midGoldCost);
			}
			HalcyoniteShrineInteractable halcyoniteShrineInteractable2 = parentShrineReference;
			halcyoniteShrineInteractable2.NetworkgoldMaterialModifier = halcyoniteShrineInteractable2.goldMaterialModifier + num4;
		}
		if ((parentShrineReference.goldDrained > parentShrineReference.midGoldCost && parentShrineReference.goldDrained < parentShrineReference.maxGoldCost) || parentShrineReference.goldMaterialModifier >= 6f)
		{
			num4 = (float)parentShrineReference.goldDrainValue * num3 / (float)(parentShrineReference.maxGoldCost - parentShrineReference.midGoldCost);
			HalcyoniteShrineInteractable halcyoniteShrineInteractable3 = parentShrineReference;
			halcyoniteShrineInteractable3.NetworkgoldMaterialModifier = halcyoniteShrineInteractable3.goldMaterialModifier + num4;
		}
		if (parentShrineReference.goldDrained >= parentShrineReference.maxGoldCost)
		{
			visualsDone = true;
		}
	}
}
