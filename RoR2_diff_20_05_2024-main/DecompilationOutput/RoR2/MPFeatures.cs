using System;

namespace RoR2;

[Flags]
public enum MPFeatures
{
	None = 0,
	HostGame = 1,
	FindGame = 2,
	Quickplay = 4,
	PrivateGame = 8,
	Invite = 0x10,
	JoinViaID = 0x20,
	EnterGameButton = 0x40,
	AdHoc = 0x80,
	FindFriendSession = 0x100,
	All = -1
}
