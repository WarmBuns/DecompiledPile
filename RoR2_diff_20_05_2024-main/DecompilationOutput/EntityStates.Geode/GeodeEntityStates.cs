using RoR2;
using UnityEngine;

namespace EntityStates.Geode;

public class GeodeEntityStates : EntityState
{
	protected GeodeController geodeController;

	protected GameObject cleansingIndicator;

	protected GameObject geodeModel;

	protected Renderer geodeTrunkRenderer;

	protected GameObject geodeBase;

	protected GameObject geodeIdleVFX;

	protected int sliceHeightNameId;

	public override void OnEnter()
	{
		base.OnEnter();
		geodeController = base.gameObject.GetComponent<GeodeController>();
		ChildLocator component = base.gameObject.GetComponent<ChildLocator>();
		cleansingIndicator = component.FindChildGameObject(0);
		geodeModel = component.FindChildGameObject(1);
		geodeBase = component.FindChildGameObject(2);
		geodeIdleVFX = component.FindChildGameObject(3);
		geodeTrunkRenderer = geodeModel.GetComponent<Renderer>();
		geodeTrunkRenderer.material.EnableKeyword("_SLICEHEIGHT");
		int propertyIndex = geodeTrunkRenderer.material.shader.FindPropertyIndex("_SliceHeight");
		sliceHeightNameId = geodeTrunkRenderer.material.shader.GetPropertyNameId(propertyIndex);
	}

	public override void OnExit()
	{
		base.OnExit();
	}
}
