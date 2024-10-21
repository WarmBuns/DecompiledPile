using System;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/ArtifactDef")]
public class ArtifactDef : ScriptableObject
{
	public string nameToken;

	public string descriptionToken;

	public UnlockableDef unlockableDef;

	public ExpansionDef requiredExpansion;

	public Sprite smallIconSelectedSprite;

	public Sprite smallIconDeselectedSprite;

	public GameObject extraUIDisplayPrefab;

	[FormerlySerializedAs("worldModelPrefab")]
	public GameObject pickupModelPrefab;

	private string _cachedName;

	public ArtifactIndex artifactIndex { get; set; }

	[Obsolete(".name should not be used. Use .cachedName instead. If retrieving the value from the engine is absolutely necessary, cast to ScriptableObject first.", true)]
	public new string name
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public string cachedName
	{
		get
		{
			return _cachedName;
		}
		set
		{
			base.name = value;
			_cachedName = value;
		}
	}

	public static void AttemptGrant(ref PickupDef.GrantContext context)
	{
		ArtifactDef artifactDef = ArtifactCatalog.GetArtifactDef(PickupCatalog.GetPickupDef(context.controller.pickupIndex).artifactIndex);
		Run.instance.GrantUnlockToAllParticipatingPlayers(artifactDef.unlockableDef);
		context.shouldNotify = true;
		context.shouldDestroy = true;
	}

	public virtual PickupDef CreatePickupDef()
	{
		PickupDef obj = new PickupDef
		{
			internalName = "ArtifactIndex." + cachedName,
			artifactIndex = artifactIndex,
			displayPrefab = pickupModelPrefab,
			nameToken = nameToken,
			baseColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Artifact)
		};
		obj.darkColor = obj.baseColor;
		obj.unlockableDef = unlockableDef;
		obj.interactContextToken = "ITEM_PICKUP_CONTEXT";
		obj.iconTexture = (smallIconSelectedSprite ? smallIconSelectedSprite.texture : null);
		obj.iconSprite = (smallIconSelectedSprite ? smallIconSelectedSprite : null);
		obj.attemptGrant = AttemptGrant;
		return obj;
	}

	private void Awake()
	{
		_cachedName = base.name;
	}

	private void OnValidate()
	{
		_cachedName = base.name;
	}
}
