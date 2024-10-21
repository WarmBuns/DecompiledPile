using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine.Networking;

namespace RoR2;

public static class NetworkExtensions
{
	private static int itemMaskBitCount;

	private static int itemMaskByteCount;

	private static byte[] itemMaskByteBuffer;

	public static void WriteAchievementIndex(this NetworkWriter writer, AchievementIndex value)
	{
		writer.WritePackedUInt32((uint)value.intValue);
	}

	public static AchievementIndex ReadAchievementIndex(this NetworkReader reader)
	{
		AchievementIndex result = default(AchievementIndex);
		result.intValue = (int)reader.ReadPackedUInt32();
		return result;
	}

	public static void WriteBodyIndex(this NetworkWriter writer, BodyIndex bodyIndex)
	{
		writer.WritePackedIndex32((int)bodyIndex);
	}

	public static BodyIndex ReadBodyIndex(this NetworkReader reader)
	{
		return (BodyIndex)reader.ReadPackedIndex32();
	}

	public static void WriteBuffIndex(this NetworkWriter writer, BuffIndex buffIndex)
	{
		writer.WritePackedIndex32((int)buffIndex);
	}

	public static BuffIndex ReadBuffIndex(this NetworkReader reader)
	{
		return (BuffIndex)reader.ReadPackedIndex32();
	}

	public static DamageTypeCombo ReadDamageType(this NetworkReader reader)
	{
		return reader.ReadPackedUInt64();
	}

	public static void WriteDamageType(this NetworkWriter writer, DamageTypeCombo damageType)
	{
		writer.WritePackedUInt64(damageType.damageTypeCombined);
	}

	public static DamageColorIndex ReadDamageColorIndex(this NetworkReader reader)
	{
		return (DamageColorIndex)reader.ReadByte();
	}

	public static void Write(this NetworkWriter writer, DamageColorIndex damageColorIndex)
	{
		writer.Write((byte)damageColorIndex);
	}

	public static void Write(this NetworkWriter writer, DamageInfo damageInfo)
	{
		writer.Write(damageInfo.damage);
		writer.Write(damageInfo.crit);
		writer.Write(damageInfo.attacker);
		writer.Write(damageInfo.inflictor);
		writer.Write(damageInfo.position);
		writer.Write(damageInfo.force);
		writer.Write(damageInfo.procChainMask);
		writer.Write(damageInfo.procCoefficient);
		writer.WritePackedUInt32((uint)(ulong)damageInfo.damageType);
		writer.Write((byte)damageInfo.damageColorIndex);
		writer.Write((byte)(damageInfo.dotIndex + 1));
		writer.Write(damageInfo.canRejectForce);
	}

	public static DamageInfo ReadDamageInfo(this NetworkReader reader)
	{
		return new DamageInfo
		{
			damage = reader.ReadSingle(),
			crit = reader.ReadBoolean(),
			attacker = reader.ReadGameObject(),
			inflictor = reader.ReadGameObject(),
			position = reader.ReadVector3(),
			force = reader.ReadVector3(),
			procChainMask = reader.ReadProcChainMask(),
			procCoefficient = reader.ReadSingle(),
			damageType = reader.ReadPackedUInt64(),
			damageColorIndex = (DamageColorIndex)reader.ReadByte(),
			dotIndex = (DotController.DotIndex)(reader.ReadByte() - 1),
			canRejectForce = reader.ReadBoolean()
		};
	}

	public static void WriteEffectIndex(this NetworkWriter writer, EffectIndex effectIndex)
	{
		writer.WritePackedUInt32((uint)(effectIndex + 1));
	}

	public static EffectIndex ReadEffectIndex(this NetworkReader reader)
	{
		return (EffectIndex)(reader.ReadPackedUInt32() - 1);
	}

	public static void Write(this NetworkWriter writer, EffectData effectData)
	{
		effectData.Serialize(writer);
	}

	public static EffectData ReadEffectData(this NetworkReader reader)
	{
		EffectData effectData = new EffectData();
		effectData.Deserialize(reader);
		return effectData;
	}

	public static void ReadEffectData(this NetworkReader reader, EffectData effectData)
	{
		effectData.Deserialize(reader);
	}

	public static void Write(this NetworkWriter writer, EntityStateIndex entityStateIndex)
	{
		writer.WritePackedIndex32((int)entityStateIndex);
	}

	public static EntityStateIndex ReadEntityStateIndex(this NetworkReader reader)
	{
		return (EntityStateIndex)reader.ReadPackedIndex32();
	}

	public static void Write(this NetworkWriter writer, EquipmentIndex equipmentIndex)
	{
		writer.WritePackedUInt32((uint)(equipmentIndex + 1));
	}

	public static EquipmentIndex ReadEquipmentIndex(this NetworkReader reader)
	{
		return (EquipmentIndex)(reader.ReadPackedUInt32() - 1);
	}

	public static void Write(this NetworkWriter writer, Run.TimeStamp timeStamp)
	{
		Run.TimeStamp.Serialize(writer, timeStamp);
	}

	public static Run.TimeStamp ReadTimeStamp(this NetworkReader reader)
	{
		return Run.TimeStamp.Deserialize(reader);
	}

	public static void Write(this NetworkWriter writer, Run.FixedTimeStamp timeStamp)
	{
		Run.FixedTimeStamp.Serialize(writer, timeStamp);
	}

	public static Run.FixedTimeStamp ReadFixedTimeStamp(this NetworkReader reader)
	{
		return Run.FixedTimeStamp.Deserialize(reader);
	}

	public static void Write(this NetworkWriter writer, HurtBoxReference hurtBoxReference)
	{
		hurtBoxReference.Write(writer);
	}

	public static HurtBoxReference ReadHurtBoxReference(this NetworkReader reader)
	{
		HurtBoxReference result = default(HurtBoxReference);
		result.Read(reader);
		return result;
	}

	public static void Write(this NetworkWriter writer, ItemIndex itemIndex)
	{
		writer.WritePackedUInt32((uint)(itemIndex + 1));
	}

	public static ItemIndex ReadItemIndex(this NetworkReader reader)
	{
		return (ItemIndex)(reader.ReadPackedUInt32() - 1);
	}

	[SystemInitializer(new Type[] { typeof(ItemCatalog) })]
	private static void Init()
	{
		itemMaskBitCount = ItemCatalog.itemCount;
		itemMaskByteCount = itemMaskBitCount + 7 >> 3;
		itemMaskByteBuffer = new byte[itemMaskByteCount];
	}

	public static void WriteItemStacks(this NetworkWriter writer, int[] srcItemStacks)
	{
		int num = 0;
		for (int i = 0; i < itemMaskByteCount; i++)
		{
			byte b = 0;
			int num2 = 0;
			while (num2 < 8 && num < itemMaskBitCount && num < srcItemStacks.Length)
			{
				if (srcItemStacks[num] > 0)
				{
					b |= (byte)(1 << num2);
				}
				num2++;
				num++;
			}
			itemMaskByteBuffer[i] = b;
		}
		for (int j = 0; j < itemMaskByteCount && j < itemMaskByteBuffer.Length; j++)
		{
			writer.Write(itemMaskByteBuffer[j]);
		}
		ItemIndex itemIndex = ItemIndex.Count;
		for (ItemIndex itemCount = (ItemIndex)ItemCatalog.itemCount; itemIndex < itemCount && (int)itemIndex < srcItemStacks.Length; itemIndex++)
		{
			int num3 = srcItemStacks[(int)itemIndex];
			if (num3 > 0)
			{
				writer.WritePackedUInt32((uint)num3);
			}
		}
	}

	public static void ReadItemStacks(this NetworkReader reader, int[] destItemStacks)
	{
		for (int i = 0; i < itemMaskByteCount; i++)
		{
			itemMaskByteBuffer[i] = reader.ReadByte();
		}
		int num = 0;
		for (int j = 0; j < itemMaskByteCount; j++)
		{
			byte b = itemMaskByteBuffer[j];
			int num2 = 0;
			while (num2 < 8 && num < itemMaskBitCount)
			{
				destItemStacks[num] = (int)(((b & (byte)(1 << num2)) != 0) ? reader.ReadPackedUInt32() : 0);
				num2++;
				num++;
			}
		}
	}

	public static void WriteBitArray(this NetworkWriter writer, [NotNull] bool[] values)
	{
		writer.WriteBitArray(values, values.Length);
	}

	public static void WriteBitArray(this NetworkWriter writer, [NotNull] bool[] values, int bufferLength)
	{
		int num = bufferLength + 7 >> 3;
		int num2 = num - 1;
		int num3 = bufferLength - (num2 << 3);
		int num4 = 0;
		for (int i = 0; i < num; i++)
		{
			byte b = 0;
			int num5 = ((i < num2) ? 8 : num3);
			int num6 = 0;
			while (num6 < num5)
			{
				if (values[num4])
				{
					b |= (byte)(1 << num6);
				}
				num6++;
				num4++;
			}
			writer.Write(b);
		}
	}

	public static void ReadBitArray(this NetworkReader reader, [NotNull] bool[] values)
	{
		reader.ReadBitArray(values, values.Length);
	}

	public static void ReadBitArray(this NetworkReader reader, [NotNull] bool[] values, int bufferLength)
	{
		int num = bufferLength + 7 >> 3;
		int num2 = num - 1;
		int num3 = bufferLength - (num2 << 3);
		int num4 = 0;
		for (int i = 0; i < num; i++)
		{
			int num5 = ((i < num2) ? 8 : num3);
			byte b = reader.ReadByte();
			int num6 = 0;
			while (num6 < num5)
			{
				values[num4] = (b & (byte)(1 << num6)) != 0;
				num6++;
				num4++;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WritePackedIndex32(this NetworkWriter writer, int index)
	{
		writer.WritePackedUInt32((uint)(index + 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReadPackedIndex32(this NetworkReader reader)
	{
		return (int)(reader.ReadPackedUInt32() - 1);
	}

	public static void Write(this NetworkWriter writer, NetworkPlayerName networkPlayerName)
	{
		networkPlayerName.Serialize(writer);
	}

	public static NetworkPlayerName ReadNetworkPlayerName(this NetworkReader reader)
	{
		NetworkPlayerName result = default(NetworkPlayerName);
		result.Deserialize(reader);
		return result;
	}

	public static void Write(this NetworkWriter writer, PackedUnitVector3 value)
	{
		writer.Write(value.value);
	}

	public static PackedUnitVector3 ReadPackedUnitVector3(this NetworkReader reader)
	{
		return new PackedUnitVector3(reader.ReadUInt16());
	}

	public static void Write(this NetworkWriter writer, PickupIndex value)
	{
		PickupIndex.WriteToNetworkWriter(writer, value);
	}

	public static PickupIndex ReadPickupIndex(this NetworkReader reader)
	{
		return PickupIndex.ReadFromNetworkReader(reader);
	}

	public static void Write(this NetworkWriter writer, PitchYawPair pitchYawPair)
	{
		writer.Write(pitchYawPair.pitch);
		writer.Write(pitchYawPair.yaw);
	}

	public static PitchYawPair ReadPitchYawPair(this NetworkReader reader)
	{
		float pitch = reader.ReadSingle();
		float yaw = reader.ReadSingle();
		return new PitchYawPair(pitch, yaw);
	}

	public static void Write(this NetworkWriter writer, ProcChainMask procChainMask)
	{
		writer.WritePackedUInt32(procChainMask.mask);
	}

	public static ProcChainMask ReadProcChainMask(this NetworkReader reader)
	{
		ProcChainMask result = default(ProcChainMask);
		result.mask = reader.ReadPackedUInt32();
		return result;
	}

	public static void Write(this NetworkWriter writer, RuleBook src)
	{
		src.Serialize(writer);
	}

	public static void ReadRuleBook(this NetworkReader reader, RuleBook dest)
	{
		dest.Deserialize(reader);
	}

	public static void Write(this NetworkWriter writer, RuleMask src)
	{
		src.Serialize(writer);
	}

	public static void ReadRuleMask(this NetworkReader reader, RuleMask dest)
	{
		dest.Deserialize(reader);
	}

	public static void Write(this NetworkWriter writer, RuleChoiceMask src)
	{
		src.Serialize(writer);
	}

	public static void ReadRuleChoiceMask(this NetworkReader reader, RuleChoiceMask dest)
	{
		dest.Deserialize(reader);
	}

	public static void Write(this NetworkWriter writer, TeamIndex teamIndex)
	{
		byte value = (byte)(teamIndex + 1);
		writer.Write(value);
	}

	public static TeamIndex ReadTeamIndex(this NetworkReader reader)
	{
		return (TeamIndex)(reader.ReadByte() - 1);
	}

	public static void Write(this NetworkWriter writer, UnlockableIndex index)
	{
		writer.WritePackedIndex32((int)index);
	}

	public static UnlockableIndex ReadUnlockableIndex(this NetworkReader reader)
	{
		return (UnlockableIndex)reader.ReadPackedIndex32();
	}
}
