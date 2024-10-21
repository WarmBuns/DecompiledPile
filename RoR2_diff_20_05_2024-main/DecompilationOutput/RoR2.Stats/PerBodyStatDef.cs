using System.Collections.Generic;
using System.Globalization;
using HG;

namespace RoR2.Stats;

public class PerBodyStatDef
{
	private readonly string prefix;

	private readonly StatRecordType recordType;

	private readonly StatDataType dataType;

	private readonly StatDef.DisplayValueFormatterDelegate displayValueFormatter;

	private readonly Dictionary<string, StatDef> bodyNameToStatDefDictionary = new Dictionary<string, StatDef>();

	public readonly string nameToken;

	private StatDef[] bodyIndexToStatDef;

	private static readonly List<PerBodyStatDef> instancesList;

	public static readonly PerBodyStatDef totalTimeAlive;

	public static readonly PerBodyStatDef totalWins;

	public static readonly PerBodyStatDef longestRun;

	public static readonly PerBodyStatDef damageDealtTo;

	public static readonly PerBodyStatDef damageDealtAs;

	public static readonly PerBodyStatDef minionDamageDealtAs;

	public static readonly PerBodyStatDef damageTakenFrom;

	public static readonly PerBodyStatDef damageTakenAs;

	public static readonly PerBodyStatDef killsAgainst;

	public static readonly PerBodyStatDef killsAgainstElite;

	public static readonly PerBodyStatDef deathsFrom;

	public static readonly PerBodyStatDef killsAs;

	public static readonly PerBodyStatDef minionKillsAs;

	public static readonly PerBodyStatDef deathsAs;

	public static readonly PerBodyStatDef timesPicked;

	public static readonly PerBodyStatDef highestInfiniteTowerWaveReachedEasy;

	public static readonly PerBodyStatDef highestInfiniteTowerWaveReachedNormal;

	public static readonly PerBodyStatDef highestInfiniteTowerWaveReachedHard;

	static PerBodyStatDef()
	{
		instancesList = new List<PerBodyStatDef>();
		totalTimeAlive = Register("totalTimeAlive", StatRecordType.Sum, StatDataType.Double);
		totalWins = Register("totalWins", StatRecordType.Sum, StatDataType.ULong);
		longestRun = Register("longestRun", StatRecordType.Max, StatDataType.Double, StatDef.TimeMMSSDisplayValueFormatter);
		damageDealtTo = Register("damageDealtTo", StatRecordType.Sum, StatDataType.ULong);
		damageDealtAs = Register("damageDealtAs", StatRecordType.Sum, StatDataType.ULong);
		minionDamageDealtAs = Register("minionDamageDealtAs", StatRecordType.Sum, StatDataType.ULong);
		damageTakenFrom = Register("damageTakenFrom", StatRecordType.Sum, StatDataType.ULong);
		damageTakenAs = Register("damageTakenAs", StatRecordType.Sum, StatDataType.ULong);
		killsAgainst = Register("killsAgainst", StatRecordType.Sum, StatDataType.ULong);
		killsAgainstElite = Register("killsAgainstElite", StatRecordType.Sum, StatDataType.ULong);
		deathsFrom = Register("deathsFrom", StatRecordType.Sum, StatDataType.ULong);
		killsAs = Register("killsAs", StatRecordType.Sum, StatDataType.ULong);
		minionKillsAs = Register("minionKillsAs", StatRecordType.Sum, StatDataType.ULong);
		deathsAs = Register("deathsAs", StatRecordType.Sum, StatDataType.ULong);
		timesPicked = Register("timesPicked", StatRecordType.Sum, StatDataType.ULong);
		highestInfiniteTowerWaveReachedEasy = Register("highestInfiniteTowerWaveReachedEasy", StatRecordType.Max, StatDataType.ULong);
		highestInfiniteTowerWaveReachedNormal = Register("highestInfiniteTowerWaveReachedNormal", StatRecordType.Max, StatDataType.ULong);
		highestInfiniteTowerWaveReachedHard = Register("highestInfiniteTowerWaveReachedHard", StatRecordType.Max, StatDataType.ULong);
	}

	public static void RegisterStatDefs()
	{
		foreach (PerBodyStatDef instances in instancesList)
		{
			instances.bodyIndexToStatDef = new StatDef[BodyCatalog.bodyCount];
			for (BodyIndex bodyIndex = (BodyIndex)0; (int)bodyIndex < BodyCatalog.bodyCount; bodyIndex++)
			{
				string bodyName = BodyCatalog.GetBodyName(bodyIndex);
				StatDef statDef = StatDef.Register(instances.prefix + "." + bodyName, instances.recordType, instances.dataType, 0.0, instances.displayValueFormatter);
				instances.bodyNameToStatDefDictionary.Add(bodyName, statDef);
				instances.bodyNameToStatDefDictionary.Add(bodyName + "(Clone)", statDef);
				instances.bodyIndexToStatDef[(int)bodyIndex] = statDef;
			}
		}
	}

	private PerBodyStatDef(string prefix, StatRecordType recordType, StatDataType dataType, StatDef.DisplayValueFormatterDelegate displayValueFormatter = null)
	{
		this.prefix = prefix;
		this.recordType = recordType;
		this.dataType = dataType;
		this.displayValueFormatter = displayValueFormatter;
		nameToken = "PERBODYSTATNAME_" + prefix.ToUpper(CultureInfo.InvariantCulture);
	}

	private static PerBodyStatDef Register(string prefix, StatRecordType recordType, StatDataType dataType, StatDef.DisplayValueFormatterDelegate displayValueFormatter = null)
	{
		PerBodyStatDef perBodyStatDef = new PerBodyStatDef(prefix, recordType, dataType, displayValueFormatter);
		instancesList.Add(perBodyStatDef);
		return perBodyStatDef;
	}

	public StatDef FindStatDef(string bodyName)
	{
		bodyNameToStatDefDictionary.TryGetValue(bodyName, out var value);
		return value;
	}

	public StatDef FindStatDef(BodyIndex bodyIndex)
	{
		return ArrayUtils.GetSafe(bodyIndexToStatDef, (int)bodyIndex);
	}
}
