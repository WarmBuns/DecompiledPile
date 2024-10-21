using System.Runtime.InteropServices;
using RoR2;
using RoR2.Networking;
using UnityEngine.Networking;

namespace Unity;

[StructLayout(LayoutKind.Auto, CharSet = CharSet.Auto)]
public class GeneratedNetworkCode
{
	public static void _ReadStructSyncListPickupIndex_VoidSuppressorBehavior(NetworkReader reader, VoidSuppressorBehavior.SyncListPickupIndex instance)
	{
		ushort num = reader.ReadUInt16();
		instance.Clear();
		for (ushort num2 = 0; num2 < num; num2++)
		{
			instance.AddInternal(instance.DeserializeItem(reader));
		}
	}

	public static void _WriteStructSyncListPickupIndex_VoidSuppressorBehavior(NetworkWriter writer, VoidSuppressorBehavior.SyncListPickupIndex value)
	{
		ushort count = value.Count;
		writer.Write(count);
		for (ushort num = 0; num < count; num++)
		{
			value.SerializeItem(writer, value.GetItem(num));
		}
	}

	public static void _ReadStructSyncListUserVote_None(NetworkReader reader, SyncListUserVote instance)
	{
		ushort num = reader.ReadUInt16();
		instance.Clear();
		for (ushort num2 = 0; num2 < num; num2++)
		{
			instance.AddInternal(instance.DeserializeItem(reader));
		}
	}

	public static void _WriteStructSyncListUserVote_None(NetworkWriter writer, SyncListUserVote value)
	{
		ushort count = value.Count;
		writer.Write(count);
		for (ushort num = 0; num < count; num++)
		{
			value.SerializeItem(writer, value.GetItem(num));
		}
	}

	public static void _WriteArrayString_None(NetworkWriter writer, string[] value)
	{
		if (value == null)
		{
			writer.Write((ushort)0);
			return;
		}
		ushort value2 = (ushort)value.Length;
		writer.Write(value2);
		for (ushort num = 0; num < value.Length; num++)
		{
			writer.Write(value[num]);
		}
	}

	public static string[] _ReadArrayString_None(NetworkReader reader)
	{
		int num = reader.ReadUInt16();
		if (num == 0)
		{
			return new string[0];
		}
		string[] array = new string[num];
		for (int i = 0; i < num; i++)
		{
			ref string reference = ref array[i];
			reference = reader.ReadString();
		}
		return array;
	}

	public static sbyte[] _ReadArraySByte_None(NetworkReader reader)
	{
		int num = reader.ReadUInt16();
		if (num == 0)
		{
			return new sbyte[0];
		}
		sbyte[] array = new sbyte[num];
		for (int i = 0; i < num; i++)
		{
			ref sbyte reference = ref array[i];
			reference = (sbyte)reader.ReadPackedUInt32();
		}
		return array;
	}

	public static void _WriteArraySByte_None(NetworkWriter writer, sbyte[] value)
	{
		if (value == null)
		{
			writer.Write((ushort)0);
			return;
		}
		ushort value2 = (ushort)value.Length;
		writer.Write(value2);
		for (ushort num = 0; num < value.Length; num++)
		{
			writer.WritePackedUInt32((uint)value[num]);
		}
	}

	public static void _WriteNetworkGuid_None(NetworkWriter writer, NetworkGuid value)
	{
		writer.WritePackedUInt64(value._a);
		writer.WritePackedUInt64(value._b);
	}

	public static void _WriteNetworkDateTime_None(NetworkWriter writer, NetworkDateTime value)
	{
		writer.WritePackedUInt64((ulong)value._binaryValue);
	}

	public static void _WriteRunStopwatch_Run(NetworkWriter writer, Run.RunStopwatch value)
	{
		writer.Write(value.offsetFromFixedTime);
		writer.Write(value.isPaused);
	}

	public static NetworkGuid _ReadNetworkGuid_None(NetworkReader reader)
	{
		NetworkGuid result = default(NetworkGuid);
		result._a = reader.ReadPackedUInt64();
		result._b = reader.ReadPackedUInt64();
		return result;
	}

	public static NetworkDateTime _ReadNetworkDateTime_None(NetworkReader reader)
	{
		NetworkDateTime result = default(NetworkDateTime);
		result._binaryValue = (long)reader.ReadPackedUInt64();
		return result;
	}

	public static Run.RunStopwatch _ReadRunStopwatch_Run(NetworkReader reader)
	{
		Run.RunStopwatch result = default(Run.RunStopwatch);
		result.offsetFromFixedTime = reader.ReadSingle();
		result.isPaused = reader.ReadBoolean();
		return result;
	}

	public static CharacterBody.NetworkItemBehaviorData _ReadNetworkItemBehaviorData_CharacterBody(NetworkReader reader)
	{
		CharacterBody.NetworkItemBehaviorData result = default(CharacterBody.NetworkItemBehaviorData);
		result.intItemValue = (int)reader.ReadPackedUInt32();
		result.floatValue = reader.ReadSingle();
		return result;
	}

	public static void _WriteNetworkItemBehaviorData_CharacterBody(NetworkWriter writer, CharacterBody.NetworkItemBehaviorData value)
	{
		writer.WritePackedUInt32((uint)value.intItemValue);
		writer.Write(value.floatValue);
	}

	public static void _WriteNetworkMasterIndex_MasterCatalog(NetworkWriter writer, MasterCatalog.NetworkMasterIndex value)
	{
		writer.WritePackedUInt32(value.i);
	}

	public static MasterCatalog.NetworkMasterIndex _ReadNetworkMasterIndex_MasterCatalog(NetworkReader reader)
	{
		MasterCatalog.NetworkMasterIndex result = default(MasterCatalog.NetworkMasterIndex);
		result.i = reader.ReadPackedUInt32();
		return result;
	}

	public static void _WritePickupIndex_None(NetworkWriter writer, PickupIndex value)
	{
		writer.WritePackedUInt32((uint)value.value);
	}

	public static PickupIndex _ReadPickupIndex_None(NetworkReader reader)
	{
		PickupIndex result = default(PickupIndex);
		result.value = (int)reader.ReadPackedUInt32();
		return result;
	}

	public static PhysForceInfo _ReadPhysForceInfo_None(NetworkReader reader)
	{
		PhysForceInfo result = default(PhysForceInfo);
		result.force = reader.ReadVector3();
		return result;
	}

	public static void _WritePhysForceInfo_None(NetworkWriter writer, PhysForceInfo value)
	{
		writer.Write(value.force);
	}

	public static CharacterMotor.HitGroundInfo _ReadHitGroundInfo_CharacterMotor(NetworkReader reader)
	{
		CharacterMotor.HitGroundInfo result = default(CharacterMotor.HitGroundInfo);
		result.velocity = reader.ReadVector3();
		result.position = reader.ReadVector3();
		result.isValidForEffect = reader.ReadBoolean();
		return result;
	}

	public static void _WriteHitGroundInfo_CharacterMotor(NetworkWriter writer, CharacterMotor.HitGroundInfo value)
	{
		writer.Write(value.velocity);
		writer.Write(value.position);
		writer.Write(value.isValidForEffect);
	}

	public static void _WriteFixedTimeStamp_Run(NetworkWriter writer, Run.FixedTimeStamp value)
	{
		writer.Write(value.t);
	}

	public static Run.FixedTimeStamp _ReadFixedTimeStamp_Run(NetworkReader reader)
	{
		Run.FixedTimeStamp result = default(Run.FixedTimeStamp);
		result.t = reader.ReadSingle();
		return result;
	}

	public static void _WriteHurtBoxReference_None(NetworkWriter writer, HurtBoxReference value)
	{
		writer.Write(value.rootObject);
		writer.WritePackedUInt32(value.hurtBoxIndexPlusOne);
	}

	public static HurtBoxReference _ReadHurtBoxReference_None(NetworkReader reader)
	{
		HurtBoxReference result = default(HurtBoxReference);
		result.rootObject = reader.ReadGameObject();
		result.hurtBoxIndexPlusOne = (byte)reader.ReadPackedUInt32();
		return result;
	}

	public static void _WriteParentIdentifier_NetworkParent(NetworkWriter writer, NetworkParent.ParentIdentifier value)
	{
		writer.WritePackedUInt32(value.indexInParentChildLocatorPlusOne);
		writer.Write(value.parentNetworkInstanceId);
	}

	public static NetworkParent.ParentIdentifier _ReadParentIdentifier_NetworkParent(NetworkReader reader)
	{
		NetworkParent.ParentIdentifier result = default(NetworkParent.ParentIdentifier);
		result.indexInParentChildLocatorPlusOne = (byte)reader.ReadPackedUInt32();
		result.parentNetworkInstanceId = reader.ReadNetworkId();
		return result;
	}

	public static UnlockableIndex[] _ReadArrayUnlockableIndex_None(NetworkReader reader)
	{
		int num = reader.ReadUInt16();
		if (num == 0)
		{
			return new UnlockableIndex[0];
		}
		UnlockableIndex[] array = new UnlockableIndex[num];
		for (int i = 0; i < num; i++)
		{
			ref UnlockableIndex reference = ref array[i];
			reference = (UnlockableIndex)reader.ReadInt32();
		}
		return array;
	}

	public static void _WriteArrayUnlockableIndex_None(NetworkWriter writer, UnlockableIndex[] value)
	{
		if (value == null)
		{
			writer.Write((ushort)0);
			return;
		}
		ushort value2 = (ushort)value.Length;
		writer.Write(value2);
		for (ushort num = 0; num < value.Length; num++)
		{
			writer.Write((int)value[num]);
		}
	}

	public static void _WriteNetworkUserId_None(NetworkWriter writer, NetworkUserId value)
	{
		writer.WritePackedUInt64(value.value);
		writer.Write(value.strValue);
		writer.WritePackedUInt32(value.subId);
	}

	public static NetworkUserId _ReadNetworkUserId_None(NetworkReader reader)
	{
		NetworkUserId result = default(NetworkUserId);
		result.value = reader.ReadPackedUInt64();
		result.strValue = reader.ReadString();
		result.subId = (byte)reader.ReadPackedUInt32();
		return result;
	}

	public static PingerController.PingInfo _ReadPingInfo_PingerController(NetworkReader reader)
	{
		PingerController.PingInfo result = default(PingerController.PingInfo);
		result.active = reader.ReadBoolean();
		result.origin = reader.ReadVector3();
		result.normal = reader.ReadVector3();
		result.targetNetworkIdentity = reader.ReadNetworkIdentity();
		return result;
	}

	public static void _WritePingInfo_PingerController(NetworkWriter writer, PingerController.PingInfo value)
	{
		writer.Write(value.active);
		writer.Write(value.origin);
		writer.Write(value.normal);
		writer.Write(value.targetNetworkIdentity);
	}

	public static WokController.WokIngredientData _ReadWokIngredientData_WokController(NetworkReader reader)
	{
		WokController.WokIngredientData wokIngredientData = new WokController.WokIngredientData();
		wokIngredientData.playerNetId = reader.ReadNetworkId();
		wokIngredientData.unlockedChef = reader.ReadBoolean();
		wokIngredientData.previouslyStoredIngredient1 = reader.ReadBoolean();
		wokIngredientData.previouslyStoredIngredient2 = reader.ReadBoolean();
		wokIngredientData.previouslyStoredIngredient3 = reader.ReadBoolean();
		return wokIngredientData;
	}

	public static void _WriteWokIngredientData_WokController(NetworkWriter writer, WokController.WokIngredientData value)
	{
		writer.Write(value.playerNetId);
		writer.Write(value.unlockedChef);
		writer.Write(value.previouslyStoredIngredient1);
		writer.Write(value.previouslyStoredIngredient2);
		writer.Write(value.previouslyStoredIngredient3);
	}

	public static CubicBezier3 _ReadCubicBezier3_None(NetworkReader reader)
	{
		CubicBezier3 result = default(CubicBezier3);
		result.a = reader.ReadVector3();
		result.b = reader.ReadVector3();
		result.c = reader.ReadVector3();
		result.d = reader.ReadVector3();
		return result;
	}

	public static WormBodyPositions2.KeyFrame _ReadKeyFrame_WormBodyPositions2(NetworkReader reader)
	{
		WormBodyPositions2.KeyFrame result = default(WormBodyPositions2.KeyFrame);
		result.curve = _ReadCubicBezier3_None(reader);
		result.length = reader.ReadSingle();
		result.time = reader.ReadSingle();
		return result;
	}

	public static void _WriteCubicBezier3_None(NetworkWriter writer, CubicBezier3 value)
	{
		writer.Write(value.a);
		writer.Write(value.b);
		writer.Write(value.c);
		writer.Write(value.d);
	}

	public static void _WriteKeyFrame_WormBodyPositions2(NetworkWriter writer, WormBodyPositions2.KeyFrame value)
	{
		_WriteCubicBezier3_None(writer, value.curve);
		writer.Write(value.length);
		writer.Write(value.time);
	}

	public static float[] _ReadArraySingle_None(NetworkReader reader)
	{
		int num = reader.ReadUInt16();
		if (num == 0)
		{
			return new float[0];
		}
		float[] array = new float[num];
		for (int i = 0; i < num; i++)
		{
			ref float reference = ref array[i];
			reference = reader.ReadSingle();
		}
		return array;
	}

	public static void _WriteArraySingle_None(NetworkWriter writer, float[] value)
	{
		if (value == null)
		{
			writer.Write((ushort)0);
			return;
		}
		ushort value2 = (ushort)value.Length;
		writer.Write(value2);
		for (ushort num = 0; num < value.Length; num++)
		{
			writer.Write(value[num]);
		}
	}

	public static void _WritePlatformID_None(NetworkWriter writer, PlatformID value)
	{
		writer.WritePackedUInt64(value.ID);
		writer.WritePackedUInt64(value.ID2);
		writer.Write(value.stringID);
	}

	public static PlatformID _ReadPlatformID_None(NetworkReader reader)
	{
		PlatformID result = default(PlatformID);
		result.ID = reader.ReadPackedUInt64();
		result.ID2 = reader.ReadPackedUInt64();
		result.stringID = reader.ReadString();
		return result;
	}

	public static void _WriteDamageTypeCombo_None(NetworkWriter writer, DamageTypeCombo value)
	{
		writer.WritePackedUInt64(value.damageTypeCombined);
		writer.Write((int)value.damageType);
		writer.Write((int)value.damageTypeExtended);
	}

	public static DamageTypeCombo _ReadDamageTypeCombo_None(NetworkReader reader)
	{
		DamageTypeCombo result = default(DamageTypeCombo);
		result.damageTypeCombined = reader.ReadPackedUInt64();
		result.damageType = (DamageType)reader.ReadInt32();
		result.damageTypeExtended = (DamageTypeExtended)reader.ReadInt32();
		return result;
	}

	public static ServerAchievementIndex _ReadServerAchievementIndex_None(NetworkReader reader)
	{
		ServerAchievementIndex result = default(ServerAchievementIndex);
		result.intValue = (int)reader.ReadPackedUInt32();
		return result;
	}

	public static void _WriteServerAchievementIndex_None(NetworkWriter writer, ServerAchievementIndex value)
	{
		writer.WritePackedUInt32((uint)value.intValue);
	}
}
