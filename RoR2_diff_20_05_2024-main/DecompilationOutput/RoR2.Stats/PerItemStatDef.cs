using System.Collections.Generic;

namespace RoR2.Stats;

public class PerItemStatDef
{
	private readonly string prefix;

	private readonly StatRecordType recordType;

	private readonly StatDataType dataType;

	private readonly StatDef[] keyToStatDef = ItemCatalog.GetPerItemBuffer<StatDef>();

	private static readonly List<PerItemStatDef> instancesList;

	public static readonly PerItemStatDef totalCollected;

	public static readonly PerItemStatDef highestCollected;

	static PerItemStatDef()
	{
		instancesList = new List<PerItemStatDef>();
		totalCollected = Register("totalCollected", StatRecordType.Sum, StatDataType.ULong);
		highestCollected = Register("highestCollected", StatRecordType.Max, StatDataType.ULong);
	}

	public static void RegisterStatDefs()
	{
		foreach (PerItemStatDef instances in instancesList)
		{
			foreach (ItemIndex allItem in ItemCatalog.allItems)
			{
				ItemDef itemDef = ItemCatalog.GetItemDef(allItem);
				StatDef statDef = StatDef.Register(instances.prefix + "." + itemDef.name, instances.recordType, instances.dataType, 0.0);
				instances.keyToStatDef[(int)allItem] = statDef;
			}
		}
	}

	private PerItemStatDef(string prefix, StatRecordType recordType, StatDataType dataType)
	{
		this.prefix = prefix;
		this.recordType = recordType;
		this.dataType = dataType;
	}

	private static PerItemStatDef Register(string prefix, StatRecordType recordType, StatDataType dataType)
	{
		PerItemStatDef perItemStatDef = new PerItemStatDef(prefix, recordType, dataType);
		instancesList.Add(perItemStatDef);
		return perItemStatDef;
	}

	public StatDef FindStatDef(ItemIndex key)
	{
		return keyToStatDef[(int)key];
	}
}
