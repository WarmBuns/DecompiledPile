using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using HG;
using RoR2.ContentManagement;
using RoR2.Modding;

namespace RoR2;

public static class EquipmentCatalog
{
	public struct AllEquipmentEnumerator : IEnumerator<EquipmentIndex>, IEnumerator, IDisposable
	{
		private EquipmentIndex position;

		public EquipmentIndex Current => position;

		object IEnumerator.Current => Current;

		public bool MoveNext()
		{
			position++;
			return (int)position < equipmentCount;
		}

		public void Reset()
		{
			position = EquipmentIndex.None;
		}

		void IDisposable.Dispose()
		{
		}
	}

	private static EquipmentDef[] equipmentDefs = Array.Empty<EquipmentDef>();

	public static List<EquipmentIndex> equipmentList = new List<EquipmentIndex>();

	public static List<EquipmentIndex> enigmaEquipmentList = new List<EquipmentIndex>();

	public static List<EquipmentIndex> randomTriggerEquipmentList = new List<EquipmentIndex>();

	private static readonly Dictionary<string, EquipmentIndex> equipmentNameToIndex = new Dictionary<string, EquipmentIndex>();

	public static ResourceAvailability availability = default(ResourceAvailability);

	public static readonly GenericStaticEnumerable<EquipmentIndex, AllEquipmentEnumerator> allEquipment;

	public static int equipmentCount => equipmentDefs.Length;

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<EquipmentDef>> getAdditionalEntries
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.EquipmentCatalog.getAdditionalEntries", value, LegacyModContentPackProvider.instance.registrationContentPack.equipmentDefs);
		}
		remove
		{
		}
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		SetEquipmentDefs(ContentManager.equipmentDefs);
		availability.MakeAvailable();
	}

	private static void SetEquipmentDefs(EquipmentDef[] newEquipmentDefs)
	{
		EquipmentDef[] array = equipmentDefs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].equipmentIndex = EquipmentIndex.None;
		}
		equipmentNameToIndex.Clear();
		equipmentList.Clear();
		enigmaEquipmentList.Clear();
		randomTriggerEquipmentList.Clear();
		ArrayUtils.CloneTo(newEquipmentDefs, ref equipmentDefs);
		Array.Sort(equipmentDefs, (EquipmentDef a, EquipmentDef b) => string.CompareOrdinal(a.name, b.name));
		for (EquipmentIndex equipmentIndex = (EquipmentIndex)0; (int)equipmentIndex < equipmentDefs.Length; equipmentIndex++)
		{
			RegisterEquipment(equipmentIndex, equipmentDefs[(int)equipmentIndex]);
		}
	}

	private static void RegisterEquipment(EquipmentIndex equipmentIndex, EquipmentDef equipmentDef)
	{
		equipmentDef.equipmentIndex = equipmentIndex;
		if (equipmentDef.canDrop)
		{
			equipmentList.Add(equipmentIndex);
			if (equipmentDef.enigmaCompatible)
			{
				enigmaEquipmentList.Add(equipmentIndex);
			}
			if (equipmentDef.canBeRandomlyTriggered)
			{
				randomTriggerEquipmentList.Add(equipmentIndex);
			}
		}
		string name = equipmentDef.name;
		equipmentNameToIndex[name] = equipmentIndex;
	}

	public static EquipmentDef GetEquipmentDef(EquipmentIndex equipmentIndex)
	{
		return ArrayUtils.GetSafe(equipmentDefs, (int)equipmentIndex);
	}

	public static EquipmentIndex FindEquipmentIndex(string equipmentName)
	{
		if (equipmentNameToIndex.TryGetValue(equipmentName, out var value))
		{
			return value;
		}
		return EquipmentIndex.None;
	}

	public static T[] GetPerEquipmentBuffer<T>()
	{
		return new T[equipmentCount];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsIndexValid(in EquipmentIndex equipmentIndex)
	{
		return (uint)equipmentIndex < (uint)equipmentCount;
	}

	[ConCommand(commandName = "equipment_list", flags = ConVarFlags.None, helpText = "Lists internal names of all equipment registered to the equipment catalog.")]
	private static void CCEquipmentList(ConCommandArgs args)
	{
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		EquipmentDef[] array = equipmentDefs;
		foreach (EquipmentDef equipmentDef in array)
		{
			stringBuilder.AppendLine(equipmentDef.name + "  (" + Language.GetString(equipmentDef.nameToken) + ")");
		}
		args.Log(stringBuilder.ToString());
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
	}
}
