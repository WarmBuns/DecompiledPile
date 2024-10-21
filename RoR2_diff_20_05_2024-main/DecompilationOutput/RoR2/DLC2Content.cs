using System.Collections;
using RoR2.ContentManagement;

namespace RoR2;

public class DLC2Content : IContentPackProvider
{
	public static class Artifacts
	{
		public static ArtifactDef Rebirth;
	}

	public static class Items
	{
		public static ItemDef LowerHealthHigherDamage;

		public static ItemDef NegateAttack;

		public static ItemDef ExtraShrineItem;

		public static ItemDef StunAndPierce;

		public static ItemDef BoostAllStats;

		public static ItemDef KnockBackHitEnemies;

		public static ItemDef TriggerEnemyDebuffs;

		public static ItemDef LowerPricedChests;

		public static ItemDef LowerPricedChestsConsumed;

		public static ItemDef ResetChests;

		public static ItemDef IncreaseDamageOnMultiKill;

		public static ItemDef GoldOnStageStart;

		public static ItemDef IncreasePrimaryDamage;

		public static ItemDef DelayedDamage;

		public static ItemDef TeleportOnLowHealth;

		public static ItemDef TeleportOnLowHealthConsumed;

		public static ItemDef ExtraStatsOnLevelUp;

		public static ItemDef MeteorAttackOnHighDamage;

		public static ItemDef OnLevelUpFreeUnlock;

		public static ItemDef LemurianHarness;
	}

	public static class ItemRelationshipTypes
	{
	}

	public static class Equipment
	{
		public static EquipmentDef HealAndRevive;

		public static EquipmentDef HealAndReviveConsumed;

		public static EquipmentDef EliteBeadEquipment;

		public static EquipmentDef EliteAurelioniteEquipment;
	}

	public static class Buffs
	{
		public static BuffDef KnockUpHitEnemies;

		public static BuffDef KnockDownHitEnemies;

		public static BuffDef BoostAllStatsBuff;

		public static BuffDef LowerHealthHigherDamageBuff;

		public static BuffDef IncreaseDamageBuff;

		public static BuffDef IncreasePrimaryDamageBuff;

		public static BuffDef DelayedDamageBuff;

		public static BuffDef DelayedDamageDebuff;

		public static BuffDef ExtraStatsOnLevelUpBuff;

		public static BuffDef lunarruin;

		public static BuffDef TamperedHeart;

		public static BuffDef CorruptionFesters;

		public static BuffDef WeakenedBeating;

		public static BuffDef StunAndPierceBuff;

		public static BuffDef ChakraBuff;

		public static BuffDef RevitalizeBuff;

		public static BuffDef FreeChest;

		public static BuffDef Boosted;

		public static BuffDef boostedFireEffect;

		public static BuffDef CookingChopped;

		public static BuffDef CookingOiled;

		public static BuffDef CookingRoasted;

		public static BuffDef CookingRolled;

		public static BuffDef CookingFlambe;

		public static BuffDef CookingSearing;

		public static BuffDef CookingRolling;

		public static BuffDef FreeUnlocks;

		public static BuffDef GeodeBuff;

		public static BuffDef DisableAllSkills;

		public static BuffDef SoulCost;

		public static BuffDef ExtraLifeBuff;

		public static BuffDef EliteBead;

		public static BuffDef EliteBeadCorruption;

		public static BuffDef EliteBeadThorns;

		public static BuffDef EliteAurelionite;

		public static BuffDef AurelioniteBlessing;

		public static BuffDef HealAndReviveRegenBuff;

		public static BuffDef ChildFrolicCooldown;

		public static BuffDef TeleportOnLowHealth;

		public static BuffDef TeleportOnLowHealthCooldown;

		public static BuffDef SojournVehicle;

		public static BuffDef SojournHealing;

		public static BuffDef HiddenRejectAllDamage;
	}

	public static class Elites
	{
		public static EliteDef Aurelionite;

		public static EliteDef Bead;
	}

	public static class GameEndings
	{
		public static GameEndingDef RebirthEndingDef;
	}

	public static class Survivors
	{
		public static SurvivorDef Seeker;

		public static SurvivorDef FalseSon;

		public static SurvivorDef Chef;
	}

	public static class MiscPickups
	{
	}

	private ContentPack contentPack = new ContentPack();

	private static readonly string addressablesLabel = "ContentPack:RoR2.DLC2";

	public string identifier => "RoR2.DLC2";

	public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
	{
		contentPack.identifier = identifier;
		AddressablesLoadHelper loadHelper = AddressablesLoadHelper.CreateUsingDefaultResourceLocator(addressablesLabel);
		yield return loadHelper.AddContentPackLoadOperationWithYields(contentPack);
		loadHelper.AddGenericOperation(delegate
		{
			ContentLoadHelper.PopulateTypeFields(typeof(Artifacts), contentPack.artifactDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(Items), contentPack.itemDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(ItemRelationshipTypes), contentPack.itemRelationshipTypes);
			ContentLoadHelper.PopulateTypeFields(typeof(Equipment), contentPack.equipmentDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(Buffs), contentPack.buffDefs, (string fieldName) => "bd" + fieldName);
			ContentLoadHelper.PopulateTypeFields(typeof(Elites), contentPack.eliteDefs, (string fieldName) => "ed" + fieldName);
			ContentLoadHelper.PopulateTypeFields(typeof(Survivors), contentPack.survivorDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(GameEndings), contentPack.gameEndingDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(MiscPickups), contentPack.miscPickupDefs);
		}, 0.05f);
		while (loadHelper.coroutine.MoveNext())
		{
			args.ReportProgress(loadHelper.progress.value);
			yield return loadHelper.coroutine.Current;
		}
	}

	public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
	{
		ContentPack.Copy(contentPack, args.output);
		yield break;
	}

	public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
	{
		yield break;
	}
}
