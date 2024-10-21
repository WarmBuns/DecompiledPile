using System;
using System.Linq;
using System.Xml.Linq;
using HG;
using JetBrains.Annotations;
using UnityEngine.Networking;

namespace RoR2.Stats;

public class StatSheet
{
	private static StatField[] fieldsTemplate;

	private static bool[] nonDefaultFieldsBuffer;

	public readonly StatField[] fields;

	private UnlockableIndex[] unlockables = Array.Empty<UnlockableIndex>();

	public void SetStatValueFromString([CanBeNull] StatDef statDef, string value)
	{
		if (statDef != null)
		{
			fields[statDef.index].SetFromString(value);
		}
	}

	public void PushStatValue([CanBeNull] StatDef statDef, ulong statValue)
	{
		if (statDef != null)
		{
			int index = statDef.index;
			fields[index].PushStatValue(statValue);
		}
	}

	public void PushStatValue([CanBeNull] StatDef statDef, double statValue)
	{
		if (statDef != null)
		{
			fields[statDef.index].PushStatValue(statValue);
		}
	}

	public void PushStatValue([NotNull] PerBodyStatDef perBodyStatDef, BodyIndex bodyIndex, ulong statValue)
	{
		PushStatValue(perBodyStatDef.FindStatDef(bodyIndex), statValue);
	}

	public void PushStatValue([NotNull] PerBodyStatDef perBodyStatDef, BodyIndex bodyIndex, double statValue)
	{
		PushStatValue(perBodyStatDef.FindStatDef(bodyIndex), statValue);
	}

	public ulong GetStatValueULong([CanBeNull] StatDef statDef)
	{
		if (statDef == null)
		{
			return 0uL;
		}
		return fields[statDef.index].GetULongValue();
	}

	public double GetStatValueDouble([CanBeNull] StatDef statDef)
	{
		if (statDef == null)
		{
			return 0.0;
		}
		return fields[statDef.index].GetDoubleValue();
	}

	public double GetStatValueAsDouble([CanBeNull] StatDef statDef)
	{
		if (statDef == null)
		{
			return 0.0;
		}
		return fields[statDef.index].GetValueAsDouble();
	}

	[Obsolete]
	public decimal GetStatValueDecimal([CanBeNull] StatDef statDef)
	{
		if (statDef == null)
		{
			return 0m;
		}
		return fields[statDef.index].GetDecimalValue();
	}

	[NotNull]
	public string GetStatValueString([CanBeNull] StatDef statDef)
	{
		if (statDef == null)
		{
			return "INVALID_STAT";
		}
		return fields[statDef.index].ToString();
	}

	[NotNull]
	public string GetStatDisplayValue([CanBeNull] StatDef statDef)
	{
		if (statDef == null)
		{
			return "INVALID_STAT";
		}
		return statDef.displayValueFormatter(ref fields[statDef.index]);
	}

	public ulong GetStatPointValue([NotNull] StatDef statDef)
	{
		return fields[statDef.index].GetPointValue(statDef.pointValue);
	}

	public ulong GetStatValueULong([NotNull] PerBodyStatDef perBodyStatDef, [NotNull] string bodyName)
	{
		return GetStatValueULong(perBodyStatDef.FindStatDef(bodyName));
	}

	public double GetStatValueDouble([NotNull] PerBodyStatDef perBodyStatDef, [NotNull] string bodyName)
	{
		return GetStatValueDouble(perBodyStatDef.FindStatDef(bodyName));
	}

	[NotNull]
	public string GetStatValueString([NotNull] PerBodyStatDef perBodyStatDef, [NotNull] string bodyName)
	{
		return GetStatValueString(perBodyStatDef.FindStatDef(bodyName));
	}

	public BodyIndex FindBodyWithHighestStat([NotNull] PerBodyStatDef perBodyStatDef)
	{
		StatField statField = fields[perBodyStatDef.FindStatDef((BodyIndex)0).index];
		BodyIndex result = (BodyIndex)0;
		for (BodyIndex bodyIndex = (BodyIndex)1; (int)bodyIndex < BodyCatalog.bodyCount; bodyIndex++)
		{
			ref StatField reference = ref fields[perBodyStatDef.FindStatDef(bodyIndex).index];
			if (statField.CompareTo(in reference) < 0)
			{
				statField = reference;
				result = bodyIndex;
			}
		}
		if (statField.IsDefault())
		{
			return BodyIndex.None;
		}
		return result;
	}

	public EquipmentIndex FindEquipmentWithHighestStat([NotNull] PerEquipmentStatDef perEquipmentStatDef)
	{
		StatField statField = fields[perEquipmentStatDef.FindStatDef((EquipmentIndex)0).index];
		EquipmentIndex result = (EquipmentIndex)0;
		for (int i = 1; i < EquipmentCatalog.equipmentCount; i++)
		{
			ref StatField reference = ref fields[perEquipmentStatDef.FindStatDef((EquipmentIndex)i).index];
			if (statField.CompareTo(in reference) < 0)
			{
				statField = reference;
				result = (EquipmentIndex)i;
			}
		}
		if (statField.IsDefault())
		{
			return EquipmentIndex.None;
		}
		return result;
	}

	[SystemInitializer(new Type[] { typeof(StatDef) })]
	private static void Init()
	{
		OnFieldsFinalized();
	}

	static StatSheet()
	{
		HGXml.Register<StatSheet>(ToXml, FromXml);
	}

	public static void ToXml(XElement element, StatSheet statSheet)
	{
		element.RemoveAll();
		XElement xElement = new XElement("fields");
		element.Add(xElement);
		StatField[] array = statSheet.fields;
		for (int i = 0; i < array.Length; i++)
		{
			ref StatField reference = ref array[i];
			if (!reference.IsDefault())
			{
				xElement.Add(new XElement(reference.name, reference.ToString()));
			}
		}
	}

	public static bool FromXml(XElement element, ref StatSheet statSheet)
	{
		XElement xElement = element.Element("fields");
		if (xElement == null)
		{
			return false;
		}
		StatField[] array = statSheet.fields;
		for (int i = 0; i < array.Length; i++)
		{
			ref StatField reference = ref array[i];
			XElement xElement2 = xElement.Element(reference.name);
			if (xElement2 != null)
			{
				reference.SetFromString(xElement2.Value);
			}
		}
		return true;
	}

	private static void OnFieldsFinalized()
	{
		fieldsTemplate = StatDef.allStatDefs.Select(delegate(StatDef v)
		{
			StatField result = default(StatField);
			result.statDef = v;
			return result;
		}).ToArray();
		nonDefaultFieldsBuffer = new bool[fieldsTemplate.Length];
		PlatformSystems.saveSystem.isXmlReady = true;
	}

	private StatSheet([NotNull] StatField[] fields)
	{
		this.fields = fields;
	}

	public static StatSheet New()
	{
		StatField[] array = new StatField[fieldsTemplate.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = fieldsTemplate[i];
		}
		return new StatSheet(array);
	}

	public int GetUnlockableCount()
	{
		return unlockables.Length;
	}

	public UnlockableIndex GetUnlockableIndex(int index)
	{
		return unlockables[index];
	}

	public UnlockableDef GetUnlockable(int index)
	{
		return UnlockableCatalog.GetUnlockableDef(unlockables[index]);
	}

	public bool HasUnlockable([CanBeNull] UnlockableDef unlockableDef)
	{
		if (!unlockableDef || unlockableDef.index == UnlockableIndex.None)
		{
			return true;
		}
		for (int i = 0; i < unlockables.Length; i++)
		{
			if (unlockables[i] == unlockableDef.index)
			{
				return true;
			}
		}
		return false;
	}

	private void AllocateUnlockables(int desiredCount)
	{
		Array.Resize(ref unlockables, desiredCount);
	}

	public void AddUnlockable([NotNull] UnlockableDef unlockableDef)
	{
		AddUnlockable(unlockableDef.index);
	}

	public void AddUnlockable(UnlockableIndex unlockIndex)
	{
		if (Array.IndexOf(unlockables, unlockIndex) == -1)
		{
			Array.Resize(ref unlockables, unlockables.Length + 1);
			unlockables[unlockables.Length - 1] = unlockIndex;
		}
	}

	public void RemoveUnlockable(UnlockableIndex unlockIndex)
	{
		int num = Array.IndexOf(unlockables, unlockIndex);
		if (num != -1)
		{
			int arraySize = unlockables.Length;
			ArrayUtils.ArrayRemoveAt(unlockables, ref arraySize, num);
			Array.Resize(ref unlockables, arraySize);
		}
	}

	public void Write(NetworkWriter writer)
	{
		for (int i = 0; i < fields.Length; i++)
		{
			nonDefaultFieldsBuffer[i] = fields[i].nonDefaultValue;
		}
		writer.WriteBitArray(nonDefaultFieldsBuffer);
		for (int j = 0; j < fields.Length; j++)
		{
			if (nonDefaultFieldsBuffer[j])
			{
				fields[j].Write(writer);
			}
		}
		writer.Write((byte)unlockables.Length);
		for (int k = 0; k < unlockables.Length; k++)
		{
			writer.Write(unlockables[k]);
		}
	}

	public void WriteChanges(NetworkWriter writer)
	{
		for (int i = 0; i < fields.Length; i++)
		{
			nonDefaultFieldsBuffer[i] = fields[i].hasChanged;
		}
		writer.WriteBitArray(nonDefaultFieldsBuffer);
		for (int j = 0; j < fields.Length; j++)
		{
			if (nonDefaultFieldsBuffer[j])
			{
				fields[j].Write(writer);
			}
		}
		writer.Write((byte)unlockables.Length);
		for (int k = 0; k < unlockables.Length; k++)
		{
			writer.Write(unlockables[k]);
		}
	}

	public void Read(NetworkReader reader)
	{
		reader.ReadBitArray(nonDefaultFieldsBuffer);
		for (int i = 0; i < fields.Length; i++)
		{
			if (nonDefaultFieldsBuffer[i])
			{
				fields[i].Read(reader);
			}
		}
		int num = reader.ReadByte();
		AllocateUnlockables(num);
		for (int j = 0; j < num; j++)
		{
			unlockables[j] = reader.ReadUnlockableIndex();
		}
	}

	public void ReadOrSetDefault(NetworkReader reader)
	{
		reader.ReadBitArray(nonDefaultFieldsBuffer);
		for (int i = 0; i < fields.Length; i++)
		{
			if (nonDefaultFieldsBuffer[i])
			{
				fields[i].Read(reader);
			}
			else
			{
				fields[i].SetDefault();
			}
		}
		int num = reader.ReadByte();
		AllocateUnlockables(num);
		for (int j = 0; j < num; j++)
		{
			unlockables[j] = reader.ReadUnlockableIndex();
		}
	}

	public static void GetDelta(StatSheet result, StatSheet newerStats, StatSheet olderStats)
	{
		StatField[] array = result.fields;
		StatField[] array2 = newerStats.fields;
		StatField[] array3 = olderStats.fields;
		int num = array.Length;
		for (int i = 0; i < num; i++)
		{
			if (!array2[i].hasChanged)
			{
				array[i].hasChanged = false;
			}
			else
			{
				array[i] = StatField.GetDelta(ref array2[i], ref array3[i]);
			}
		}
		UnlockableIndex[] array4 = newerStats.unlockables;
		UnlockableIndex[] array5 = olderStats.unlockables;
		int num2 = 0;
		foreach (UnlockableIndex unlockableIndex in array4)
		{
			bool flag = false;
			for (int k = 0; k < array5.Length; k++)
			{
				if (array5[k] == unlockableIndex)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				num2++;
			}
		}
		result.AllocateUnlockables(num2);
		UnlockableIndex[] array6 = result.unlockables;
		int num3 = 0;
		foreach (UnlockableIndex unlockableIndex2 in array4)
		{
			bool flag2 = false;
			for (int m = 0; m < array5.Length; m++)
			{
				if (array5[m] == unlockableIndex2)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				array6[num3++] = unlockableIndex2;
			}
		}
	}

	public void ApplyDelta(StatSheet deltaSheet)
	{
		StatField[] array = deltaSheet.fields;
		for (int i = 0; i < fields.Length; i++)
		{
			if (array[i].hasChanged)
			{
				fields[i].PushDelta(ref array[i]);
				fields[i].hasChanged = false;
			}
		}
		for (int j = 0; j < deltaSheet.unlockables.Length; j++)
		{
			AddUnlockable(deltaSheet.unlockables[j]);
		}
	}

	public void ClearHasChangedFlags()
	{
		int num = fields.Length;
		for (int i = 0; i < num; i++)
		{
			fields[i].hasChanged = false;
		}
	}

	public void SetAllFieldsToMaxValue()
	{
		for (int i = 0; i < fields.Length; i++)
		{
			fields[i].SetToMaxValue();
		}
	}

	public static void Copy([NotNull] StatSheet src, [NotNull] StatSheet dest)
	{
		Array.Copy(src.fields, dest.fields, src.fields.Length);
		dest.AllocateUnlockables(src.unlockables.Length);
		Array.Copy(src.unlockables, dest.unlockables, src.unlockables.Length);
	}

	public void DebugDisplay(string _prefix = "")
	{
		string text = _prefix + "\n";
		for (int i = 0; i < 5; i++)
		{
			text = text + fields[i].name + ": " + ((fields[i].dataType == StatDataType.ULong) ? ((double)fields[i].GetULongValue()) : fields[i].GetDoubleValue());
		}
	}
}
