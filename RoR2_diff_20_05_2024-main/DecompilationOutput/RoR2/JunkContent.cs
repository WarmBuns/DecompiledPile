using System.Collections;
using RoR2.ContentManagement;

namespace RoR2;

public class JunkContent : IContentPackProvider
{
	public static class Items
	{
		public static ItemDef AACannon;

		public static ItemDef PlasmaCore;

		public static ItemDef CooldownOnCrit;

		public static ItemDef TempestOnKill;

		public static ItemDef WarCryOnCombat;

		public static ItemDef PlantOnHit;

		public static ItemDef MageAttunement;

		public static ItemDef BurnNearby;

		public static ItemDef CritHeal;

		public static ItemDef Incubator;

		public static ItemDef SkullCounter;
	}

	public static class Equipment
	{
		public static EquipmentDef SoulJar;

		public static EquipmentDef EliteYellowEquipment;

		public static EquipmentDef EliteGoldEquipment;

		public static EquipmentDef GhostGun;

		public static EquipmentDef OrbitalLaser;

		public static EquipmentDef Enigma;

		public static EquipmentDef SoulCorruptor;
	}

	public static class Buffs
	{
		public static BuffDef EnrageAncientWisp;

		public static BuffDef LightningShield;

		public static BuffDef Slow30;

		public static BuffDef EngiTeamShield;

		public static BuffDef GoldEmpowered;

		public static BuffDef LoaderOvercharged;

		public static BuffDef LoaderPylonPowered;

		public static BuffDef Deafened;

		public static BuffDef MeatRegenBoost;

		public static BuffDef BodyArmor;
	}

	public static class Elites
	{
		public static EliteDef Gold;
	}

	private ContentPack contentPack = new ContentPack();

	public string identifier => "RoR2.Junk";

	public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
	{
		contentPack.identifier = identifier;
		AddressablesLoadHelper loadHelper = AddressablesLoadHelper.CreateUsingDefaultResourceLocator("ContentPack:RoR2.Junk");
		yield return loadHelper.AddContentPackLoadOperationWithYields(contentPack);
		loadHelper.AddGenericOperation(delegate
		{
			ContentLoadHelper.PopulateTypeFields(typeof(Items), contentPack.itemDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(Equipment), contentPack.equipmentDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(Buffs), contentPack.buffDefs, (string fieldName) => "bd" + fieldName);
			ContentLoadHelper.PopulateTypeFields(typeof(Elites), contentPack.eliteDefs, (string fieldName) => "ed" + fieldName);
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
