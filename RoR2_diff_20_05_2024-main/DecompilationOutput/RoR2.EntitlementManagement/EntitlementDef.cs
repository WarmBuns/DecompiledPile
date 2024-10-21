using UnityEngine;

namespace RoR2.EntitlementManagement;

[CreateAssetMenu(menuName = "RoR2/EntitlementDef")]
public class EntitlementDef : ScriptableObject
{
	[HideInInspector]
	[SerializeField]
	public EntitlementIndex entitlementIndex = EntitlementIndex.None;

	[Tooltip("The localization token of the display name.")]
	public string nameToken;

	[Header("PC")]
	public uint steamAppId;

	[Tooltip("This is an EOS Item Id, not Offer Id.")]
	public string eosItemId;

	[Header("XBox")]
	public string GamecoreID;

	[Header("Playstation")]
	public string SonyID;

	[Header("Nintendo")]
	public ulong switchNsUid;

	[Tooltip("Nintendo DLC is an index. ie first DLC is 0, etc")]
	public uint switchDLCIndex;

	private void OnDisable()
	{
		entitlementIndex = EntitlementIndex.None;
	}
}
