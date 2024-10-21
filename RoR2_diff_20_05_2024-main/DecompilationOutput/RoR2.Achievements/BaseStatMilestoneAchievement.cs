using System;
using RoR2.Stats;

namespace RoR2.Achievements;

public abstract class BaseStatMilestoneAchievement : BaseAchievement
{
	protected abstract StatDef statDef { get; }

	protected abstract ulong statRequirement { get; }

	private ulong statProgress => base.userProfile.statSheet.GetStatValueULong(statDef);

	public override void OnInstall()
	{
		base.OnInstall();
		UserProfile obj = base.userProfile;
		obj.onStatsReceived = (Action)Delegate.Combine(obj.onStatsReceived, new Action(Check));
		if (IsProgressiveAchievement())
		{
			StatDef obj2 = statDef;
			obj2.onChangeCallback = (Action)Delegate.Combine(obj2.onChangeCallback, new Action(base.UpdateProgress));
		}
		Check();
	}

	public override void OnUninstall()
	{
		UserProfile obj = base.userProfile;
		obj.onStatsReceived = (Action)Delegate.Remove(obj.onStatsReceived, new Action(Check));
		if (IsProgressiveAchievement())
		{
			StatDef obj2 = statDef;
			obj2.onChangeCallback = (Action)Delegate.Remove(obj2.onChangeCallback, new Action(base.UpdateProgress));
		}
		base.OnUninstall();
	}

	public override float ProgressForAchievement()
	{
		return (float)statProgress / (float)statRequirement;
	}

	public override ulong GetCurrentProgress()
	{
		return statProgress;
	}

	public override ulong GetProgressRequirement()
	{
		return statRequirement;
	}

	private void Check()
	{
		if (statProgress >= statRequirement)
		{
			Grant();
			TryToCompleteActivity();
		}
	}
}
