using System;
using JetBrains.Annotations;
using UnityEngine.Networking;

namespace RoR2;

public struct EquipmentState : IEquatable<EquipmentState>
{
	public readonly EquipmentIndex equipmentIndex;

	public readonly Run.FixedTimeStamp chargeFinishTime;

	public readonly byte charges;

	public bool isDisabled;

	public bool dirty;

	[CanBeNull]
	public readonly EquipmentDef equipmentDef;

	public static readonly EquipmentState empty = new EquipmentState(EquipmentIndex.None, Run.FixedTimeStamp.negativeInfinity, 0);

	public bool isPerfomingRecharge => !chargeFinishTime.isPositiveInfinity;

	public EquipmentState(EquipmentIndex equipmentIndex, Run.FixedTimeStamp chargeFinishTime, byte charges, bool isDisabled = false)
	{
		this.equipmentIndex = equipmentIndex;
		this.chargeFinishTime = chargeFinishTime;
		this.charges = charges;
		this.isDisabled = isDisabled;
		dirty = true;
		equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
	}

	public bool Equals(EquipmentState other)
	{
		if (equipmentIndex == other.equipmentIndex && chargeFinishTime.Equals(other.chargeFinishTime))
		{
			return charges == other.charges;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is EquipmentState)
		{
			return Equals((EquipmentState)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((int)equipmentIndex * 397) ^ chargeFinishTime.GetHashCode();
	}

	public static EquipmentState Deserialize(NetworkReader reader)
	{
		EquipmentIndex num = reader.ReadEquipmentIndex();
		Run.FixedTimeStamp fixedTimeStamp = reader.ReadFixedTimeStamp();
		byte b = reader.ReadByte();
		return new EquipmentState(num, fixedTimeStamp, b);
	}

	public static void Serialize(NetworkWriter writer, EquipmentState equipmentState)
	{
		writer.Write(equipmentState.equipmentIndex);
		writer.Write(equipmentState.chargeFinishTime);
		writer.Write(equipmentState.charges);
	}
}
