using UnityEngine;

namespace RoR2;

public class PickupDef
{
	public struct GrantContext
	{
		public CharacterBody body;

		public GenericPickupController controller;

		public bool shouldDestroy;

		public bool shouldNotify;
	}

	public delegate void AttemptGrantDelegate(ref GrantContext context);

	public string internalName;

	public GameObject displayPrefab;

	public GameObject dropletDisplayPrefab;

	public string nameToken = "???";

	public Color baseColor;

	public Color darkColor;

	public ItemIndex itemIndex = ItemIndex.None;

	public EquipmentIndex equipmentIndex = EquipmentIndex.None;

	public ArtifactIndex artifactIndex = ArtifactIndex.None;

	public MiscPickupIndex miscPickupIndex = MiscPickupIndex.None;

	public ItemTier itemTier = ItemTier.NoTier;

	public uint coinValue;

	public UnlockableDef unlockableDef;

	public string interactContextToken;

	public bool isLunar;

	public bool isBoss;

	public Texture iconTexture;

	public Sprite iconSprite;

	public AttemptGrantDelegate attemptGrant;

	public PickupIndex pickupIndex { get; set; } = PickupIndex.none;
}
