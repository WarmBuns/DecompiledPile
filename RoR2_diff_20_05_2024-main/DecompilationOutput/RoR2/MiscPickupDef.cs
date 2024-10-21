using System;
using UnityEngine;

namespace RoR2;

public abstract class MiscPickupDef : ScriptableObject
{
	[SerializeField]
	public uint coinValue;

	[SerializeField]
	public string nameToken;

	[SerializeField]
	public GameObject displayPrefab;

	[SerializeField]
	public GameObject dropletDisplayPrefab;

	[SerializeField]
	public ColorCatalog.ColorIndex baseColor;

	[SerializeField]
	public ColorCatalog.ColorIndex darkColor;

	[SerializeField]
	public string interactContextToken;

	[SerializeField]
	public string descriptionToken;

	[SerializeField]
	public Sprite visual;

	[NonSerialized]
	public MiscPickupIndex miscPickupIndex;

	public abstract void GrantPickup(ref PickupDef.GrantContext context);

	public virtual string GetInternalName()
	{
		return "MiscPickupIndex." + base.name;
	}

	public virtual PickupDef CreatePickupDef()
	{
		return new PickupDef
		{
			internalName = GetInternalName(),
			coinValue = coinValue,
			nameToken = nameToken,
			displayPrefab = displayPrefab,
			dropletDisplayPrefab = dropletDisplayPrefab,
			baseColor = ColorCatalog.GetColor(baseColor),
			darkColor = ColorCatalog.GetColor(darkColor),
			interactContextToken = interactContextToken,
			attemptGrant = GrantPickup,
			miscPickupIndex = miscPickupIndex
		};
	}
}
