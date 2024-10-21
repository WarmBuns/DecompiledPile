using System;

namespace RoR2;

[Flags]
public enum LimbFlags
{
	None = 0,
	Head = 1,
	RightArm = 2,
	LeftArm = 4,
	RightCalf = 8,
	LeftLeg = 0x10,
	Count = 5
}
