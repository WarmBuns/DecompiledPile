using System.Collections;
using RoR2.ContentManagement;

namespace RoR2;

public class DLC1Content : IContentPackProvider
{
	public static class Items
	{
		public static ItemDef MoveSpeedOnKill;

		public static ItemDef HealingPotion;

		public static ItemDef HealingPotionConsumed;

		public static ItemDef PermanentDebuffOnHit;

		public static ItemDef AttackSpeedAndMoveSpeed;

		public static ItemDef CritDamage;

		public static ItemDef BearVoid;

		public static ItemDef MushroomVoid;

		public static ItemDef CloverVoid;

		public static ItemDef StrengthenBurn;

		public static ItemDef GummyCloneIdentifier;

		public static ItemDef RegeneratingScrap;

		public static ItemDef RegeneratingScrapConsumed;

		public static ItemDef BleedOnHitVoid;

		public static ItemDef CritGlassesVoid;

		public static ItemDef TreasureCacheVoid;

		public static ItemDef SlowOnHitVoid;

		public static ItemDef MissileVoid;

		public static ItemDef ChainLightningVoid;

		public static ItemDef ExtraLifeVoid;

		public static ItemDef ExtraLifeVoidConsumed;

		public static ItemDef EquipmentMagazineVoid;

		public static ItemDef ExplodeOnDeathVoid;

		public static ItemDef FragileDamageBonus;

		public static ItemDef FragileDamageBonusConsumed;

		public static ItemDef OutOfCombatArmor;

		public static ItemDef ScrapWhiteSuppressed;

		public static ItemDef ScrapGreenSuppressed;

		public static ItemDef ScrapRedSuppressed;

		public static ItemDef MoreMissile;

		public static ItemDef ImmuneToDebuff;

		public static ItemDef RandomEquipmentTrigger;

		public static ItemDef PrimarySkillShuriken;

		public static ItemDef RandomlyLunar;

		public static ItemDef GoldOnHurt;

		public static ItemDef HalfAttackSpeedHalfCooldowns;

		public static ItemDef HalfSpeedDoubleHealth;

		public static ItemDef FreeChest;

		public static ItemDef ConvertCritChanceToCritDamage;

		public static ItemDef ElementalRingVoid;

		public static ItemDef LunarSun;

		public static ItemDef DroneWeapons;

		public static ItemDef DroneWeaponsBoost;

		public static ItemDef DroneWeaponsDisplay1;

		public static ItemDef DroneWeaponsDisplay2;

		public static ItemDef VoidmanPassiveItem;

		public static ItemDef MinorConstructOnKill;

		public static ItemDef VoidMegaCrabItem;
	}

	public static class ItemRelationshipTypes
	{
		public static ItemRelationshipType ContagiousItem;
	}

	public static class Equipment
	{
		public static EquipmentDef Molotov;

		public static EquipmentDef VendingMachine;

		public static EquipmentDef BossHunter;

		public static EquipmentDef BossHunterConsumed;

		public static EquipmentDef GummyClone;

		public static EquipmentDef MultiShopCard;

		public static EquipmentDef LunarPortalOnUse;

		public static EquipmentDef EliteVoidEquipment;
	}

	public static class Buffs
	{
		public static BuffDef ElementalRingVoidReady;

		public static BuffDef ElementalRingVoidCooldown;

		public static BuffDef BearVoidReady;

		public static BuffDef BearVoidCooldown;

		public static BuffDef KillMoveSpeed;

		public static BuffDef PermanentDebuff;

		public static BuffDef MushroomVoidActive;

		public static BuffDef StrongerBurn;

		public static BuffDef Fracture;

		public static BuffDef OutOfCombatArmorBuff;

		public static BuffDef PrimarySkillShurikenBuff;

		public static BuffDef Blinded;

		public static BuffDef EliteEarth;

		public static BuffDef EliteVoid;

		public static BuffDef JailerTether;

		public static BuffDef JailerSlow;

		public static BuffDef VoidRaidCrabWardWipeFog;

		public static BuffDef VoidSurvivorCorruptMode;

		public static BuffDef ImmuneToDebuffReady;

		public static BuffDef ImmuneToDebuffCooldown;
	}

	public static class Elites
	{
		public static EliteDef Earth;

		public static EliteDef EarthHonor;

		public static EliteDef Void;
	}

	public static class GameEndings
	{
		public static GameEndingDef VoidEnding;
	}

	public static class Survivors
	{
		public static SurvivorDef Railgunner;
	}

	public static class MiscPickups
	{
		public static MiscPickupDef VoidCoin;
	}

	private ContentPack contentPack = new ContentPack();

	private static readonly string addressablesLabel = "ContentPack:RoR2.DLC1";

	public string identifier => "RoR2.DLC1";

	public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
	{
		contentPack.identifier = identifier;
		AddressablesLoadHelper loadHelper = AddressablesLoadHelper.CreateUsingDefaultResourceLocator(addressablesLabel);
		yield return loadHelper.AddContentPackLoadOperationWithYields(contentPack);
		loadHelper.AddGenericOperation(delegate
		{
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
