using System;

namespace RoR2;

[Flags]
public enum MPLobbyFeatures
{
	None = 0,
	CreateLobby = 1,
	SocialIcon = 2,
	HostPromotion = 4,
	Clipboard = 8,
	Invite = 0x10,
	UserIcon = 0x20,
	LeaveLobby = 0x40,
	LobbyDropdownOptions = 0x80,
	Voice = 0x100,
	All = -1
}
