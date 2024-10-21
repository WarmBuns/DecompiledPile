using System;
using System.Runtime.InteropServices;

namespace RoR2;

[Serializable]
[StructLayout(LayoutKind.Explicit)]
public struct DamageTypeCombo
{
	internal static readonly DamageTypeCombo Generic = new DamageTypeCombo
	{
		damageType = DamageType.Generic,
		damageTypeExtended = DamageTypeExtended.Generic
	};

	[FieldOffset(0)]
	[NonSerialized]
	public ulong damageTypeCombined;

	[FieldOffset(0)]
	public DamageType damageType;

	[FieldOffset(4)]
	public DamageTypeExtended damageTypeExtended;

	public DamageTypeCombo(DamageTypeCombo damageType, DamageTypeExtended damageTypeExtended)
	{
		this = default(DamageTypeCombo);
		this.damageType = damageType;
		this.damageTypeExtended = damageTypeExtended;
	}

	public static implicit operator DamageTypeCombo(DamageType damage)
	{
		DamageTypeCombo result = default(DamageTypeCombo);
		result.damageType = damage;
		return result;
	}

	public static implicit operator DamageTypeCombo(DamageTypeExtended damage)
	{
		DamageTypeCombo result = default(DamageTypeCombo);
		result.damageTypeExtended = damage;
		return result;
	}

	public static implicit operator DamageTypeCombo(ulong damage)
	{
		DamageTypeCombo result = default(DamageTypeCombo);
		result.damageTypeCombined = damage;
		return result;
	}

	public static implicit operator DamageType(DamageTypeCombo combo)
	{
		return combo.damageType;
	}

	public static implicit operator DamageTypeExtended(DamageTypeCombo combo)
	{
		return combo.damageTypeExtended;
	}

	public static implicit operator ulong(DamageTypeCombo combo)
	{
		return combo.damageTypeCombined;
	}

	public static DamageTypeCombo operator &(DamageTypeCombo operand1, DamageTypeCombo operand2)
	{
		return new DamageTypeCombo(operand1.damageType & operand2.damageType, operand1.damageTypeExtended & operand2.damageTypeExtended);
	}

	public static DamageTypeCombo operator |(DamageTypeCombo operand1, DamageTypeCombo operand2)
	{
		return new DamageTypeCombo(operand1.damageType | operand2.damageType, operand1.damageTypeExtended | operand2.damageTypeExtended);
	}

	public static DamageTypeCombo operator ^(DamageTypeCombo operand1, DamageTypeCombo operand2)
	{
		return new DamageTypeCombo(operand1.damageType ^ operand2.damageType, operand1.damageTypeExtended ^ operand2.damageTypeExtended);
	}

	public static DamageTypeCombo operator ~(DamageTypeCombo operand)
	{
		return new DamageTypeCombo(~operand.damageType, ~operand.damageTypeExtended);
	}

	public override string ToString()
	{
		return $"DamageType: {damageType}, DamageTypeExtended: {damageTypeExtended}";
	}
}
