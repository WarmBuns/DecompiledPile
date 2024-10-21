using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/EliteDef")]
public class EliteDef : ScriptableObject
{
	public string modifierToken;

	public EquipmentDef eliteEquipmentDef;

	public Color32 color;

	public int shaderEliteRampIndex = -1;

	public float healthBoostCoefficient = 1f;

	public float damageBoostCoefficient = 1f;

	public EliteIndex eliteIndex { get; set; } = EliteIndex.None;

	public bool IsAvailable()
	{
		if ((bool)eliteEquipmentDef)
		{
			if ((bool)Run.instance)
			{
				return !Run.instance.IsEquipmentExpansionLocked(eliteEquipmentDef.equipmentIndex);
			}
			return false;
		}
		return true;
	}

	[ConCommand(commandName = "elites_migrate", flags = ConVarFlags.None, helpText = "Generates EliteDef assets from the existing catalog entries.")]
	private static void CCElitesMigrate(ConCommandArgs args)
	{
		for (EliteIndex eliteIndex = (EliteIndex)0; (int)eliteIndex < EliteCatalog.eliteList.Count; eliteIndex++)
		{
			EditorUtil.CopyToScriptableObject<EliteDef, EliteDef>(EliteCatalog.GetEliteDef(eliteIndex), "Assets/RoR2/Resources/EliteDefs/");
		}
	}
}
