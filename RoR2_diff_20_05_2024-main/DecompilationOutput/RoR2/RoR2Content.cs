using System.Collections;
using System.Collections.Generic;
using System.Text;
using HG;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

public class RoR2Content : IContentPackProvider
{
	public static class Artifacts
	{
		public static ArtifactDef Glass;

		public static ArtifactDef Bomb;

		public static ArtifactDef Sacrifice;

		public static ArtifactDef Enigma;

		public static ArtifactDef EliteOnly;

		public static ArtifactDef RandomSurvivorOnRespawn;

		public static ArtifactDef WeakAssKnees;

		public static ArtifactDef WispOnDeath;

		public static ArtifactDef SingleMonsterType;

		public static ArtifactDef MixEnemy;

		public static ArtifactDef ShadowClone;

		public static ArtifactDef TeamDeath;

		public static ArtifactDef Swarms;

		public static ArtifactDef Command;

		public static ArtifactDef MonsterTeamGainsItems;

		public static ArtifactDef FriendlyFire;

		public static ArtifactDef glassArtifactDef => Glass;

		public static ArtifactDef bombArtifactDef => Bomb;

		public static ArtifactDef sacrificeArtifactDef => Sacrifice;

		public static ArtifactDef enigmaArtifactDef => Enigma;

		public static ArtifactDef eliteOnlyArtifactDef => EliteOnly;

		public static ArtifactDef randomSurvivorOnRespawnArtifactDef => RandomSurvivorOnRespawn;

		public static ArtifactDef weakAssKneesArtifactDef => WeakAssKnees;

		public static ArtifactDef wispOnDeath => WispOnDeath;

		public static ArtifactDef singleMonsterTypeArtifactDef => SingleMonsterType;

		public static ArtifactDef mixEnemyArtifactDef => MixEnemy;

		public static ArtifactDef shadowCloneArtifactDef => ShadowClone;

		public static ArtifactDef teamDeathArtifactDef => TeamDeath;

		public static ArtifactDef swarmsArtifactDef => Swarms;

		public static ArtifactDef commandArtifactDef => Command;

		public static ArtifactDef monsterTeamGainsItemsArtifactDef => MonsterTeamGainsItems;

		public static ArtifactDef friendlyFireArtifactDef => FriendlyFire;
	}

	public static class Items
	{
		public static ItemDef Syringe;

		public static ItemDef Bear;

		public static ItemDef Behemoth;

		public static ItemDef Missile;

		public static ItemDef ExplodeOnDeath;

		public static ItemDef Dagger;

		public static ItemDef Tooth;

		public static ItemDef CritGlasses;

		public static ItemDef Hoof;

		public static ItemDef Feather;

		public static ItemDef ChainLightning;

		public static ItemDef Seed;

		public static ItemDef Icicle;

		public static ItemDef GhostOnKill;

		public static ItemDef Mushroom;

		public static ItemDef Crowbar;

		public static ItemDef LevelBonus;

		public static ItemDef AttackSpeedOnCrit;

		public static ItemDef BleedOnHit;

		public static ItemDef SprintOutOfCombat;

		public static ItemDef FallBoots;

		public static ItemDef WardOnLevel;

		public static ItemDef Phasing;

		public static ItemDef HealOnCrit;

		public static ItemDef HealWhileSafe;

		public static ItemDef PersonalShield;

		public static ItemDef EquipmentMagazine;

		public static ItemDef NovaOnHeal;

		public static ItemDef ShockNearby;

		public static ItemDef Infusion;

		public static ItemDef Clover;

		public static ItemDef Medkit;

		public static ItemDef Bandolier;

		public static ItemDef BounceNearby;

		public static ItemDef IgniteOnKill;

		public static ItemDef StunChanceOnHit;

		public static ItemDef Firework;

		public static ItemDef LunarDagger;

		public static ItemDef GoldOnHit;

		public static ItemDef WarCryOnMultiKill;

		public static ItemDef BoostHp;

		public static ItemDef BoostDamage;

		public static ItemDef ShieldOnly;

		public static ItemDef AlienHead;

		public static ItemDef Talisman;

		public static ItemDef Knurl;

		public static ItemDef BeetleGland;

		public static ItemDef CrippleWardOnLevel;

		public static ItemDef SprintBonus;

		public static ItemDef SecondarySkillMagazine;

		public static ItemDef StickyBomb;

		public static ItemDef TreasureCache;

		public static ItemDef BossDamageBonus;

		public static ItemDef SprintArmor;

		public static ItemDef IceRing;

		public static ItemDef FireRing;

		public static ItemDef SlowOnHit;

		public static ItemDef ExtraLife;

		public static ItemDef ExtraLifeConsumed;

		public static ItemDef UtilitySkillMagazine;

		public static ItemDef HeadHunter;

		public static ItemDef KillEliteFrenzy;

		public static ItemDef RepeatHeal;

		public static ItemDef Ghost;

		public static ItemDef HealthDecay;

		public static ItemDef AutoCastEquipment;

		public static ItemDef IncreaseHealing;

		public static ItemDef JumpBoost;

		public static ItemDef DrizzlePlayerHelper;

		public static ItemDef ExecuteLowHealthElite;

		public static ItemDef EnergizedOnEquipmentUse;

		public static ItemDef BarrierOnOverHeal;

		public static ItemDef TonicAffliction;

		public static ItemDef TitanGoldDuringTP;

		public static ItemDef SprintWisp;

		public static ItemDef BarrierOnKill;

		public static ItemDef ArmorReductionOnHit;

		public static ItemDef TPHealingNova;

		public static ItemDef NearbyDamageBonus;

		public static ItemDef LunarUtilityReplacement;

		public static ItemDef MonsoonPlayerHelper;

		public static ItemDef Thorns;

		public static ItemDef FlatHealth;

		public static ItemDef Pearl;

		public static ItemDef ShinyPearl;

		public static ItemDef BonusGoldPackOnKill;

		public static ItemDef LaserTurbine;

		public static ItemDef LunarPrimaryReplacement;

		public static ItemDef NovaOnLowHealth;

		public static ItemDef LunarTrinket;

		public static ItemDef InvadingDoppelganger;

		public static ItemDef CutHp;

		public static ItemDef ArtifactKey;

		public static ItemDef ArmorPlate;

		public static ItemDef Squid;

		public static ItemDef DeathMark;

		public static ItemDef Plant;

		public static ItemDef FocusConvergence;

		public static ItemDef BoostAttackSpeed;

		public static ItemDef AdaptiveArmor;

		public static ItemDef CaptainDefenseMatrix;

		public static ItemDef FireballsOnHit;

		public static ItemDef LightningStrikeOnHit;

		public static ItemDef BleedOnHitAndExplode;

		public static ItemDef SiphonOnLowHealth;

		public static ItemDef MonstersOnShrineUse;

		public static ItemDef RandomDamageZone;

		public static ItemDef ScrapWhite;

		public static ItemDef ScrapGreen;

		public static ItemDef ScrapRed;

		public static ItemDef ScrapYellow;

		public static ItemDef LunarBadLuck;

		public static ItemDef BoostEquipmentRecharge;

		public static ItemDef LunarSecondaryReplacement;

		public static ItemDef LunarSpecialReplacement;

		public static ItemDef TeamSizeDamageBonus;

		public static ItemDef RoboBallBuddy;

		public static ItemDef ParentEgg;

		public static ItemDef SummonedEcho;

		public static ItemDef MinionLeash;

		public static ItemDef UseAmbientLevel;

		public static ItemDef TeleportWhenOob;

		public static ItemDef MinHealthPercentage;
	}

	public static class Equipment
	{
		public static EquipmentDef CommandMissile;

		public static EquipmentDef Fruit;

		public static EquipmentDef Meteor;

		[TargetAssetName("EliteFireEquipment")]
		public static EquipmentDef AffixRed;

		[TargetAssetName("EliteLightningEquipment")]
		public static EquipmentDef AffixBlue;

		[TargetAssetName("EliteIceEquipment")]
		public static EquipmentDef AffixWhite;

		[TargetAssetName("ElitePoisonEquipment")]
		public static EquipmentDef AffixPoison;

		public static EquipmentDef Blackhole;

		public static EquipmentDef CritOnUse;

		public static EquipmentDef DroneBackup;

		public static EquipmentDef BFG;

		public static EquipmentDef Jetpack;

		public static EquipmentDef Lightning;

		public static EquipmentDef GoldGat;

		public static EquipmentDef PassiveHealing;

		public static EquipmentDef LunarPotion;

		public static EquipmentDef BurnNearby;

		public static EquipmentDef Scanner;

		public static EquipmentDef CrippleWard;

		public static EquipmentDef Gateway;

		public static EquipmentDef Tonic;

		public static EquipmentDef QuestVolatileBattery;

		public static EquipmentDef Cleanse;

		public static EquipmentDef FireBallDash;

		[TargetAssetName("EliteHauntedEquipment")]
		public static EquipmentDef AffixHaunted;

		public static EquipmentDef GainArmor;

		public static EquipmentDef Saw;

		public static EquipmentDef Recycle;

		public static EquipmentDef LifestealOnHit;

		public static EquipmentDef TeamWarCry;

		public static EquipmentDef DeathProjectile;

		[TargetAssetName("EliteEchoEquipment")]
		public static EquipmentDef AffixEcho;

		[TargetAssetName("EliteLunarEquipment")]
		public static EquipmentDef AffixLunar;
	}

	public static class Buffs
	{
		public static BuffDef Slow50;

		public static BuffDef ArmorBoost;

		public static BuffDef AttackSpeedOnCrit;

		public static BuffDef HiddenInvincibility;

		public static BuffDef OnFire;

		public static BuffDef Warbanner;

		public static BuffDef Cloak;

		public static BuffDef CloakSpeed;

		public static BuffDef FullCrit;

		[TargetAssetName("bdElitePoison")]
		public static BuffDef AffixPoison;

		public static BuffDef EngiShield;

		public static BuffDef TeslaField;

		public static BuffDef WarCryBuff;

		public static BuffDef Energized;

		public static BuffDef BeetleJuice;

		public static BuffDef BugWings;

		public static BuffDef MedkitHeal;

		public static BuffDef ClayGoo;

		public static BuffDef Immune;

		public static BuffDef Cripple;

		public static BuffDef Slow80;

		public static BuffDef Slow60;

		[TargetAssetName("bdEliteFire")]
		public static BuffDef AffixRed;

		[TargetAssetName("bdEliteLightning")]
		public static BuffDef AffixBlue;

		public static BuffDef NoCooldowns;

		[TargetAssetName("bdEliteIce")]
		public static BuffDef AffixWhite;

		public static BuffDef TonicBuff;

		public static BuffDef HealingDisabled;

		public static BuffDef Weak;

		public static BuffDef Entangle;

		[TargetAssetName("bdEliteHaunted")]
		public static BuffDef AffixHaunted;

		public static BuffDef Pulverized;

		public static BuffDef PulverizeBuildup;

		[TargetAssetName("bdEliteHauntedRecipient")]
		public static BuffDef AffixHauntedRecipient;

		public static BuffDef Intangible;

		public static BuffDef ElephantArmorBoost;

		public static BuffDef NullifyStack;

		public static BuffDef Nullified;

		public static BuffDef Bleeding;

		public static BuffDef SuperBleed;

		public static BuffDef Poisoned;

		public static BuffDef WhipBoost;

		public static BuffDef Blight;

		public static BuffDef DeathMark;

		public static BuffDef CrocoRegen;

		public static BuffDef MercExpose;

		public static BuffDef LifeSteal;

		public static BuffDef PowerBuff;

		public static BuffDef LunarShell;

		public static BuffDef TeamWarCry;

		public static BuffDef PermanentCurse;

		public static BuffDef ElementalRingsReady;

		public static BuffDef ElementalRingsCooldown;

		public static BuffDef LunarSecondaryRoot;

		public static BuffDef LunarDetonationCharge;

		public static BuffDef Overheat;

		public static BuffDef Fruiting;

		public static BuffDef BanditSkull;

		[TargetAssetName("bdEliteEcho")]
		public static BuffDef AffixEcho;

		public static BuffDef LaserTurbineKillCharge;

		[TargetAssetName("bdEliteLunar")]
		public static BuffDef AffixLunar;

		public static BuffDef SmallArmorBoost;

		public static BuffDef VoidFogMild;

		public static BuffDef VoidFogStrong;
	}

	public static class Elites
	{
		public static EliteDef Fire;

		public static EliteDef FireHonor;

		public static EliteDef Lightning;

		public static EliteDef LightningHonor;

		public static EliteDef Ice;

		public static EliteDef IceHonor;

		public static EliteDef Poison;

		public static EliteDef Haunted;

		public static EliteDef Echo;

		public static EliteDef Lunar;
	}

	public static class GameEndings
	{
		public static GameEndingDef StandardLoss;

		public static GameEndingDef ObliterationEnding;

		public static GameEndingDef LimboEnding;

		public static GameEndingDef MainEnding;

		public static GameEndingDef PrismaticTrialEnding;
	}

	public static class Survivors
	{
		public static SurvivorDef Commando;

		public static SurvivorDef Engi;

		public static SurvivorDef Huntress;

		public static SurvivorDef Mage;

		public static SurvivorDef Merc;

		public static SurvivorDef Toolbot;

		public static SurvivorDef Treebot;

		public static SurvivorDef Loader;

		public static SurvivorDef Croco;

		public static SurvivorDef Captain;

		public static SurvivorDef Bandit2;
	}

	public static class MiscPickups
	{
		public static MiscPickupDef LunarCoin;
	}

	public static DirectorCardCategorySelection mixEnemyMonsterCards;

	public static bool initialized;

	public static AsyncOperationHandle<DirectorCardCategorySelection> loadOperationHandle;

	private ContentPack contentPack = new ContentPack();

	private Dictionary<SurvivorDef, UnlockableDef[]> eclipseUnlockableCache = new Dictionary<SurvivorDef, UnlockableDef[]>();

	public string identifier => "RoR2.BaseContent";

	public static void Init()
	{
		RoR2Application.Print("RoR2Content init, time = " + RoR2Application.stopwatch.ElapsedMilliseconds + "msec. ");
		AsyncOperationHandle<DirectorCardCategorySelection> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<DirectorCardCategorySelection>("DirectorCardCategorySelections/dccsMixEnemy");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<DirectorCardCategorySelection> x)
		{
			mixEnemyMonsterCards = x.Result;
		};
		initialized = true;
		RoR2Application.Print("RoR2Content init end, time = " + RoR2Application.stopwatch.ElapsedMilliseconds + "msec. ");
	}

	public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
	{
		contentPack.identifier = identifier;
		AddressablesLoadHelper loadHelper = AddressablesLoadHelper.CreateUsingDefaultResourceLocator("ContentPack:RoR2.BaseContent");
		yield return loadHelper.AddContentPackLoadOperationWithYields(contentPack);
		loadHelper.AddGenericOperation(delegate
		{
			ContentLoadHelper.PopulateTypeFields(typeof(Artifacts), contentPack.artifactDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(Items), contentPack.itemDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(Equipment), contentPack.equipmentDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(Buffs), contentPack.buffDefs, (string fieldName) => "bd" + fieldName);
			ContentLoadHelper.PopulateTypeFields(typeof(Elites), contentPack.eliteDefs, (string fieldName) => "ed" + fieldName);
			ContentLoadHelper.PopulateTypeFields(typeof(GameEndings), contentPack.gameEndingDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(Survivors), contentPack.survivorDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(MiscPickups), contentPack.miscPickupDefs);
		}, 0.04f);
		loadHelper.AddGenericOperation(delegate
		{
			contentPack.effectDefs.Find("CoinEmitter").cullMethod = (EffectData effectData) => SettingsConVars.cvExpAndMoneyEffects.value;
		}, 0.01f);
		while (loadHelper.coroutine.MoveNext())
		{
			args.ReportProgress(loadHelper.progress.value);
			yield return loadHelper.coroutine.Current;
		}
	}

	public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
	{
		ContentPack.Copy(contentPack, args.output);
		int minEclipseLevel = 1;
		int maxEclipseLevel = 8;
		List<UnlockableDef> list = new List<UnlockableDef>();
		foreach (ContentPackLoadInfo peerLoadInfo in args.peerLoadInfos)
		{
			ReadOnlyNamedAssetCollection<SurvivorDef> survivorDefs = peerLoadInfo.previousContentPack.survivorDefs;
			_ = args.output.unlockableDefs.Length;
			foreach (SurvivorDef item2 in survivorDefs)
			{
				if (!eclipseUnlockableCache.TryGetValue(item2, out var value))
				{
					value = CreateEclipseUnlockablesForSurvivor(item2, minEclipseLevel, maxEclipseLevel);
					eclipseUnlockableCache[item2] = value;
				}
				UnlockableDef[] array = value;
				foreach (UnlockableDef item in array)
				{
					list.Add(item);
				}
			}
		}
		args.output.unlockableDefs.Add(list.ToArray());
		args.ReportProgress(1f);
		yield break;
	}

	public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
	{
		eclipseUnlockableCache.Clear();
		args.ReportProgress(1f);
		yield break;
	}

	private static UnlockableDef[] CreateEclipseUnlockablesForSurvivor(SurvivorDef survivorDef, int minEclipseLevel, int maxEclipseLevel)
	{
		UnlockableDef[] array = new UnlockableDef[maxEclipseLevel - minEclipseLevel + 1];
		for (int i = minEclipseLevel + 1; i <= maxEclipseLevel + 1; i++)
		{
			array[i - (minEclipseLevel + 1)] = CreateEclipseUnlockableForSurvivor(survivorDef, i);
		}
		return array;
	}

	private static UnlockableDef CreateEclipseUnlockableForSurvivor(SurvivorDef survivorDef, int eclipseLevel)
	{
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		string cachedName = stringBuilder.Clear().Append("Eclipse.").Append(survivorDef.cachedName)
			.Append(".")
			.AppendInt(eclipseLevel)
			.ToString();
		UnlockableDef unlockableDef = ScriptableObject.CreateInstance<UnlockableDef>();
		unlockableDef.cachedName = cachedName;
		unlockableDef.hidden = true;
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		return unlockableDef;
	}
}
