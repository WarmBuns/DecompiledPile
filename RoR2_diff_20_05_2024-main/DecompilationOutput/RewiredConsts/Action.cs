using Rewired.Dev;

namespace RewiredConsts;

public static class Action
{
	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Move Horizontal")]
	public const int MoveHorizontal = 0;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Move Vertical")]
	public const int MoveVertical = 1;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Aim Horizontal Mouse")]
	public const int AimHorizontalMouse = 2;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Aim Vertical Mouse")]
	public const int AimVerticalMouse = 3;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Aim Horizontal Stick")]
	public const int AimHorizontalStick = 16;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Aim Vertical Stick")]
	public const int AimVerticalStick = 17;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Jump")]
	public const int Jump = 4;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Sprint")]
	public const int Sprint = 18;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Interact")]
	public const int Interact = 5;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Activate Equipment")]
	public const int Equipment = 6;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Primary Skill")]
	public const int PrimarySkill = 7;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Secondary Skill")]
	public const int SecondarySkill = 8;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Utility Skill")]
	public const int UtilitySkill = 9;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Special Skill")]
	public const int SpecialSkill = 10;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Start")]
	public const int Start = 11;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Info")]
	public const int Info = 19;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UIHorizontal")]
	public const int UIHorizontal = 12;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UIVertical")]
	public const int UIVertical = 13;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UISubmit")]
	public const int UISubmit = 14;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UICancel")]
	public const int UICancel = 15;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UI Left Mouse Button")]
	public const int UILMB = 20;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UI Right Mouse Button")]
	public const int UIRMB = 21;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UI Middle Mouse Button")]
	public const int UIMMB = 22;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UICursorX")]
	public const int UICursorX = 23;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UICursorY")]
	public const int UICursorY = 24;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UIScrollWheel")]
	public const int UIScrollWheel = 26;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Pause")]
	public const int Pause = 25;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Ping")]
	public const int Ping = 28;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UITabLeft")]
	public const int UITabLeft = 29;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UITabRight")]
	public const int UITabRight = 30;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UISubmitAlt")]
	public const int UISubmitAlt = 31;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UISubmenuLeft")]
	public const int UISubmenuLeft = 32;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UISubmenuRight")]
	public const int UISubmenuRight = 33;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UISubmenuUp")]
	public const int UISubmenuUp = 34;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UISubmenuDown")]
	public const int UISubmenuDown = 35;
}
