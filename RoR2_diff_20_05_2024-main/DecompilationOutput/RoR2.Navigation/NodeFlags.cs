using System;

namespace RoR2.Navigation;

[Flags]
public enum NodeFlags : byte
{
	None = 0,
	NoCeiling = 1,
	TeleporterOK = 2,
	NoCharacterSpawn = 4,
	NoChestSpawn = 8,
	NoShrineSpawn = 0x10
}
