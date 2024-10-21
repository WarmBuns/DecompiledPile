using RoR2;
using UnityEngine;

namespace EntityStates.Toolbot;

public class ToolbotDualWield : ToolbotDualWieldBase
{
	public static string animLayer;

	public static string animStateName;

	public static GameObject coverPrefab;

	public static GameObject coverEjectEffect;

	public static string enterSoundString;

	public static string exitSoundString;

	public static string startLoopSoundString;

	public static string stopLoopSoundString;

	private bool keyReleased;

	private GameObject coverLeftInstance;

	private GameObject coverRightInstance;

	private uint loopSoundID;

	protected override bool shouldAllowPrimarySkills => true;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animLayer, animStateName);
		Util.PlaySound(enterSoundString, base.gameObject);
		loopSoundID = Util.PlaySound(startLoopSoundString, base.gameObject);
		if ((bool)coverPrefab)
		{
			Transform transform = FindModelChild("LowerArmL");
			Transform transform2 = FindModelChild("LowerArmR");
			if ((bool)transform)
			{
				coverLeftInstance = Object.Instantiate(coverPrefab, transform.position, transform.rotation, transform);
			}
			if ((bool)transform2)
			{
				coverRightInstance = Object.Instantiate(coverPrefab, transform2.position, transform2.rotation, transform2);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			bool flag = this.IsKeyDownAuthority(base.skillLocator, base.inputBank);
			keyReleased |= !flag;
			if (keyReleased && flag)
			{
				outer.SetNextState(new ToolbotDualWieldEnd
				{
					activatorSkillSlot = base.activatorSkillSlot
				});
			}
		}
	}

	public override void OnExit()
	{
		Util.PlaySound(exitSoundString, base.gameObject);
		Util.PlaySound(stopLoopSoundString, base.gameObject);
		AkSoundEngine.StopPlayingID(loopSoundID);
		if ((bool)coverLeftInstance)
		{
			EffectManager.SimpleMuzzleFlash(coverEjectEffect, base.gameObject, "LowerArmL", transmit: false);
			EntityState.Destroy(coverLeftInstance);
		}
		if ((bool)coverRightInstance)
		{
			EffectManager.SimpleMuzzleFlash(coverEjectEffect, base.gameObject, "LowerArmR", transmit: false);
			EntityState.Destroy(coverRightInstance);
		}
		base.OnExit();
	}
}
