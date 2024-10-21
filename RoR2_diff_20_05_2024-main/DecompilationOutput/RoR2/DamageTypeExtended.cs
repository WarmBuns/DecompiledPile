using System;

namespace RoR2;

[Flags]
public enum DamageTypeExtended : uint
{
	Generic = 0u,
	ChefSearDamage = 0x4000000u,
	SojournVehicleDamage = 0x8000000u,
	DamagePercentOfMaxHealth = 0x10000000u,
	ApplyBuffPermanently = 0x20000000u,
	DisableAllSkills = 0x40000000u,
	OutOfBounds = 0x80000000u
}
