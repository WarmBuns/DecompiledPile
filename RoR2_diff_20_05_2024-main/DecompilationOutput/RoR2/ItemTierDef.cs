using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/ItemTierDef")]
public class ItemTierDef : ScriptableObject
{
	public enum PickupRules
	{
		Default,
		ConfirmFirst,
		ConfirmAll
	}

	[FormerlySerializedAs("tier")]
	[SerializeField]
	private ItemTier _tier;

	public Texture bgIconTexture;

	public ColorCatalog.ColorIndex colorIndex;

	public ColorCatalog.ColorIndex darkColorIndex;

	public bool isDroppable;

	public bool canScrap;

	public bool canRestack;

	public PickupRules pickupRules;

	public GameObject highlightPrefab;

	public GameObject dropletDisplayPrefab;

	public ItemTier tier
	{
		get
		{
			if (_tier == ItemTier.AssignedAtRuntime)
			{
				Debug.LogError("ItemTierDef '" + base.name + "' has a tier of 'AssignedAtRuntime'.  Attempting to fix...");
				_tier = ItemTierCatalog.FindTierDef(base.name)?._tier ?? _tier;
				if (_tier != ItemTier.AssignedAtRuntime)
				{
					Debug.LogError($"Able to fix ItemTierDef '{base.name}' (_tier = {_tier}).  This is probably because the asset is being duplicated across bundles.");
				}
			}
			return _tier;
		}
		set
		{
			_tier = value;
		}
	}
}
