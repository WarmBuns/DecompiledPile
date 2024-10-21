using Assets.RoR2.Scripts.Platform;

namespace RoR2.Achievements.Huntress;

[RegisterAchievement("HuntressCollectCrowbars", "Skills.Huntress.MiniBlink", null, 3u, null)]
public class HuntressCollectCrowbarsAchievement : BaseAchievement
{
	private Inventory currentInventory;

	private static readonly int requirement = 12;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("HuntressBody");
	}

	public override void OnInstall()
	{
		base.OnInstall();
	}

	public override void OnUninstall()
	{
		SetCurrentInventory(null);
		base.OnUninstall();
	}

	private void UpdateInventory()
	{
		Inventory inventory = null;
		if ((bool)base.localUser.cachedMasterController)
		{
			inventory = base.localUser.cachedMasterController.master.inventory;
		}
		SetCurrentInventory(inventory);
	}

	private void SetCurrentInventory(Inventory newInventory)
	{
		if ((object)currentInventory != newInventory)
		{
			if ((object)currentInventory != null)
			{
				currentInventory.onInventoryChanged -= OnInventoryChanged;
			}
			currentInventory = newInventory;
			if ((object)currentInventory != null)
			{
				currentInventory.onInventoryChanged += OnInventoryChanged;
				OnInventoryChanged();
			}
		}
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		base.localUser.onMasterChanged += UpdateInventory;
		UpdateInventory();
	}

	protected override void OnBodyRequirementBroken()
	{
		base.localUser.onMasterChanged -= UpdateInventory;
		SetCurrentInventory(null);
		base.OnBodyRequirementBroken();
	}

	private void OnInventoryChanged()
	{
		if (requirement <= currentInventory.GetItemCount(RoR2Content.Items.Crowbar))
		{
			Grant();
			TryToCompleteActivity();
		}
	}

	public override void TryToCompleteActivity()
	{
		bool flag = base.localUser.id == LocalUserManager.GetFirstLocalUser().id;
		if (shouldGrant && flag)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "HuntressCollectCrowbars";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}
