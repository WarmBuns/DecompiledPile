using System.Collections.Generic;
using JetBrains.Annotations;

namespace RoR2.Stats;

public class PerStageStatDef
{
	private readonly string prefix;

	private readonly StatRecordType recordType;

	private readonly StatDataType dataType;

	private readonly Dictionary<string, StatDef> keyToStatDef = new Dictionary<string, StatDef>();

	private StatDef.DisplayValueFormatterDelegate displayValueFormatter;

	private static readonly List<PerStageStatDef> instancesList;

	public static readonly PerStageStatDef totalTimesVisited;

	public static readonly PerStageStatDef totalTimesCleared;

	static PerStageStatDef()
	{
		instancesList = new List<PerStageStatDef>();
		totalTimesVisited = Register("totalTimesVisited", StatRecordType.Sum, StatDataType.ULong);
		totalTimesCleared = Register("totalTimesCleared", StatRecordType.Sum, StatDataType.ULong);
	}

	public static void RegisterStatDefs()
	{
		foreach (PerStageStatDef instances in instancesList)
		{
			foreach (string allBaseSceneName in SceneCatalog.allBaseSceneNames)
			{
				StatDef value = StatDef.Register(instances.prefix + "." + allBaseSceneName, instances.recordType, instances.dataType, 0.0, instances.displayValueFormatter);
				instances.keyToStatDef[allBaseSceneName] = value;
			}
		}
	}

	private PerStageStatDef(string prefix, StatRecordType recordType, StatDataType dataType, StatDef.DisplayValueFormatterDelegate displayValueFormatter)
	{
		this.prefix = prefix;
		this.recordType = recordType;
		this.dataType = dataType;
		this.displayValueFormatter = displayValueFormatter ?? new StatDef.DisplayValueFormatterDelegate(StatDef.DefaultDisplayValueFormatter);
	}

	[NotNull]
	private static PerStageStatDef Register(string prefix, StatRecordType recordType, StatDataType dataType, StatDef.DisplayValueFormatterDelegate displayValueFormatter = null)
	{
		PerStageStatDef perStageStatDef = new PerStageStatDef(prefix, recordType, dataType, displayValueFormatter);
		instancesList.Add(perStageStatDef);
		return perStageStatDef;
	}

	[CanBeNull]
	public StatDef FindStatDef(string key)
	{
		if (keyToStatDef.TryGetValue(key, out var value))
		{
			return value;
		}
		return null;
	}
}
