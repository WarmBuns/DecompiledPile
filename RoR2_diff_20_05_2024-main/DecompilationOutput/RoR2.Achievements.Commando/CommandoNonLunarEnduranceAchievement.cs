using System;
using Assets.RoR2.Scripts.Platform;
using RoR2.Stats;

namespace RoR2.Achievements.Commando;

[RegisterAchievement("CommandoNonLunarEndurance", "Skills.Commando.ThrowGrenade", null, 3u, null)]
public class CommandoNonLunarEnduranceAchievement : BaseAchievement
{
	private static readonly ulong requirement = 20uL;

	private PlayerCharacterMasterController cachedMasterController;

	private PlayerStatsComponent cachedStatsComponent;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("CommandoBody");
	}

	private static bool EverPickedUpLunarItems(StatSheet statSheet)
	{
		foreach (ItemIndex lunarItem in ItemCatalog.lunarItemList)
		{
			if (statSheet.GetStatValueULong(PerItemStatDef.totalCollected.FindStatDef(lunarItem)) != 0)
			{
				return true;
			}
		}
		foreach (EquipmentIndex equipment in EquipmentCatalog.equipmentList)
		{
			if (EquipmentCatalog.GetEquipmentDef(equipment).isLunar && statSheet.GetStatValueDouble(PerEquipmentStatDef.totalTimeHeld.FindStatDef(equipment)) > 0.0)
			{
				return true;
			}
		}
		return false;
	}

	private void OnMasterChanged()
	{
		if (base.localUser != null)
		{
			SetMasterController(base.localUser.cachedMasterController);
		}
	}

	private void SetMasterController(PlayerCharacterMasterController playerCharacterMasterController)
	{
		if ((object)base.localUser.cachedMasterController == cachedMasterController)
		{
			return;
		}
		bool num = (object)cachedStatsComponent != null;
		cachedMasterController = base.localUser.cachedMasterController;
		cachedStatsComponent = (cachedMasterController ? cachedMasterController.GetComponent<PlayerStatsComponent>() : null);
		bool flag = (object)cachedStatsComponent != null;
		if (num != flag && base.userProfile != null)
		{
			if (flag)
			{
				UserProfile obj = base.userProfile;
				obj.onStatsReceived = (Action)Delegate.Combine(obj.onStatsReceived, new Action(OnStatsChanged));
			}
			else
			{
				UserProfile obj2 = base.userProfile;
				obj2.onStatsReceived = (Action)Delegate.Remove(obj2.onStatsReceived, new Action(OnStatsChanged));
			}
		}
	}

	private void OnStatsChanged()
	{
		if ((object)cachedStatsComponent != null && requirement <= cachedStatsComponent.currentStats.GetStatValueULong(StatDef.totalStagesCompleted))
		{
			if (EverPickedUpLunarItems(cachedStatsComponent.currentStats))
			{
				SetMasterController(null);
				return;
			}
			Grant();
			TryToCompleteActivity();
		}
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		base.localUser.onMasterChanged += OnMasterChanged;
		OnMasterChanged();
	}

	protected override void OnBodyRequirementBroken()
	{
		base.localUser.onMasterChanged -= OnMasterChanged;
		SetMasterController(null);
		base.OnBodyRequirementBroken();
	}

	public override void TryToCompleteActivity()
	{
		bool flag = base.localUser.id == LocalUserManager.GetFirstLocalUser().id;
		if (shouldGrant && flag)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "CommandoNonLunarEndurance";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}
