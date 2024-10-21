using RoR2;
using RoR2.Audio;
using UnityEngine;

namespace EntityStates.Railgunner.Backpack;

public abstract class BaseBackpack : BaseState
{
	[SerializeField]
	public LoopSoundDef loopSoundDef;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public string mecanimBoolName;

	private LoopSoundManager.SoundLoopPtr loopPtr;

	protected bool isSoundScaledByAttackSpeed;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)loopSoundDef)
		{
			if (isSoundScaledByAttackSpeed)
			{
				loopPtr = LoopSoundManager.PlaySoundLoopLocalRtpc(base.gameObject, loopSoundDef, "attackSpeed", Util.CalculateAttackSpeedRtpcValue(attackSpeedStat));
			}
			else
			{
				loopPtr = LoopSoundManager.PlaySoundLoopLocal(base.gameObject, loopSoundDef);
			}
		}
		if (!string.IsNullOrEmpty(enterSoundString))
		{
			if (isSoundScaledByAttackSpeed)
			{
				Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, attackSpeedStat);
			}
			else
			{
				Util.PlaySound(enterSoundString, base.gameObject);
			}
		}
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator && !string.IsNullOrEmpty(mecanimBoolName))
		{
			modelAnimator.SetBool(mecanimBoolName, value: true);
		}
	}

	public override void OnExit()
	{
		if (loopPtr.isValid)
		{
			LoopSoundManager.StopSoundLoopLocal(loopPtr);
		}
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator && !string.IsNullOrEmpty(mecanimBoolName))
		{
			modelAnimator.SetBool(mecanimBoolName, value: false);
		}
		base.OnExit();
	}
}
