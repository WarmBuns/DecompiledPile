using UnityEngine;

namespace RoR2.Skills;

public class ToolbotWeaponSkillDef : SkillDef
{
	public string stanceName;

	public string entrySound;

	public string entryAnimState;

	public string enterGestureAnimState;

	public string exitAnimState;

	public string exitGestureAnimState;

	public int animatorWeaponIndex;

	public GameObject crosshairPrefab;

	public AnimationCurve crosshairSpreadCurve;
}
