using RoR2.EntitlementManagement;
using UnityEngine;

namespace RoR2.ExpansionManagement;

[CreateAssetMenu(menuName = "RoR2/ExpansionDef")]
public class ExpansionDef : ScriptableObject
{
	[HideInInspector]
	[SerializeField]
	public ExpansionIndex expansionIndex;

	[Tooltip("The entitlement required to use this expansion.")]
	public EntitlementDef requiredEntitlement;

	[Tooltip("The token for the user-facing name of this expansion.")]
	public string nameToken;

	[Tooltip("The token for the user-facing description of this expansion.")]
	public string descriptionToken;

	[ShowThumbnail]
	[Tooltip("The icon for this expansion.")]
	public Sprite iconSprite;

	[ShowThumbnail]
	[Tooltip("The icon to display when this expansion is disabled.")]
	public Sprite disabledIconSprite;

	public RuleChoiceDef enabledChoice;

	[Tooltip("This prefab is instantiated and childed to the run")]
	public GameObject runBehaviorPrefab;
}
