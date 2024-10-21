using UnityEngine;

namespace RoR2.Skills;

[CreateAssetMenu(menuName = "RoR2/SkillDef/SeekerWeaponSkillDef", fileName = "New SeekerSkillDef.asset")]
public class SeekerWeaponSkillDef : SteppedSkillDef
{
	public bool targetTrackingIndicator;

	public GameObject crosshairOverridePrefab;
}
