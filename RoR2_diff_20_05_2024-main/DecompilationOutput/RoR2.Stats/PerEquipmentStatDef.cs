using System.Collections.Generic;

namespace RoR2.Stats;

public class PerEquipmentStatDef
{
	private readonly string prefix;

	private readonly StatRecordType recordType;

	private readonly StatDataType dataType;

	private readonly StatDef[] keyToStatDef = EquipmentCatalog.GetPerEquipmentBuffer<StatDef>();

	private StatDef.DisplayValueFormatterDelegate displayValueFormatter;

	private static readonly List<PerEquipmentStatDef> instancesList;

	public static readonly PerEquipmentStatDef totalTimeHeld;

	public static readonly PerEquipmentStatDef totalTimesFired;

	static PerEquipmentStatDef()
	{
		instancesList = new List<PerEquipmentStatDef>();
		totalTimeHeld = Register("totalTimeHeld", StatRecordType.Sum, StatDataType.Double, StatDef.TimeMMSSDisplayValueFormatter);
		totalTimesFired = Register("totalTimesFired", StatRecordType.Sum, StatDataType.ULong);
	}

	public static void RegisterStatDefs()
	{
		foreach (PerEquipmentStatDef instances in instancesList)
		{
			foreach (EquipmentIndex item in EquipmentCatalog.allEquipment)
			{
				EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(item);
				StatDef statDef = StatDef.Register(instances.prefix + "." + equipmentDef.name, instances.recordType, instances.dataType, 0.0, instances.displayValueFormatter);
				instances.keyToStatDef[(int)item] = statDef;
			}
		}
	}

	private PerEquipmentStatDef(string prefix, StatRecordType recordType, StatDataType dataType, StatDef.DisplayValueFormatterDelegate displayValueFormatter)
	{
		this.prefix = prefix;
		this.recordType = recordType;
		this.dataType = dataType;
		this.displayValueFormatter = displayValueFormatter ?? new StatDef.DisplayValueFormatterDelegate(StatDef.DefaultDisplayValueFormatter);
	}

	private static PerEquipmentStatDef Register(string prefix, StatRecordType recordType, StatDataType dataType, StatDef.DisplayValueFormatterDelegate displayValueFormatter = null)
	{
		PerEquipmentStatDef perEquipmentStatDef = new PerEquipmentStatDef(prefix, recordType, dataType, displayValueFormatter);
		instancesList.Add(perEquipmentStatDef);
		return perEquipmentStatDef;
	}

	public StatDef FindStatDef(EquipmentIndex key)
	{
		return keyToStatDef[(int)key];
	}
}
