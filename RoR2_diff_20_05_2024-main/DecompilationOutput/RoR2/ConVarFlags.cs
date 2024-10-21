using System;

namespace RoR2;

[Flags]
public enum ConVarFlags
{
	None = 0,
	ExecuteOnServer = 1,
	SenderMustBeServer = 2,
	Archive = 4,
	Cheat = 8,
	Engine = 0x10
}
