using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class DeathRewards : MonoBehaviour, IOnKilledServerReceiver
{
	[Obsolete("'logUnlockableName' is discontinued. Use 'logUnlockableDef' instead.", true)]
	[Tooltip("'logUnlockableName' is discontinued. Use 'logUnlockableDef' instead.")]
	public string logUnlockableName = "";

	public UnlockableDef logUnlockableDef;

	public PickupDropTable bossDropTable;

	private uint fallbackGold;

	private CharacterBody characterBody;

	private static GameObject coinEffectPrefab;

	private static GameObject logbookPrefab;

	[Header("Deprecated")]
	public SerializablePickupIndex bossPickup;

	public uint goldReward
	{
		get
		{
			if (!characterBody.master)
			{
				return fallbackGold;
			}
			return characterBody.master.money;
		}
		set
		{
			if ((bool)characterBody.master)
			{
				characterBody.master.money = value;
			}
			else
			{
				fallbackGold = value;
			}
		}
	}

	public uint expReward { get; set; }

	public int spawnValue { get; set; }

	[InitDuringStartup]
	private static void LoadAssets()
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/CoinEmitter");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
		{
			coinEffectPrefab = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/NetworkedObjects/LogPickup");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
		{
			logbookPrefab = x.Result;
		};
	}

	private void Awake()
	{
		characterBody = GetComponent<CharacterBody>();
	}

	public void OnKilledServer(DamageReport damageReport)
	{
		CharacterBody attackerBody = damageReport.attackerBody;
		if (!attackerBody)
		{
			return;
		}
		Vector3 corePosition = characterBody.corePosition;
		uint num = goldReward;
		if (Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse6)
		{
			num = (uint)((float)num * 0.8f);
		}
		TeamManager.instance.GiveTeamMoney(damageReport.attackerTeamIndex, num);
		EffectManager.SpawnEffect(coinEffectPrefab, new EffectData
		{
			origin = corePosition,
			genericFloat = goldReward,
			scale = characterBody.radius
		}, transmit: true);
		float num2 = 1f + (characterBody.level - 1f) * 0.3f;
		int num3 = 0;
		double num4 = 0.0;
		if (attackerBody.GetBuffCount(DLC2Content.Buffs.IncreasePrimaryDamageBuff) > 0 && attackerBody.luminousShotReady)
		{
			int itemCount = attackerBody.inventory.GetItemCount(DLC2Content.Items.IncreasePrimaryDamage);
			num3 = attackerBody.GetBuffCount(DLC2Content.Buffs.IncreasePrimaryDamageBuff);
			num4 = (float)expReward * num2 * (0.1f * (float)itemCount) * (float)num3;
			num4 = Math.Round(num4);
			if (num4 < 1.0)
			{
				num4 = 1.0;
			}
			_ = expReward;
		}
		ExperienceManager.instance.AwardExperience(corePosition, attackerBody, (uint)((double)((float)expReward * num2) + num4));
		_ = expReward;
		if ((bool)logUnlockableDef && Run.instance.CanUnlockableBeGrantedThisRun(logUnlockableDef) && Util.CheckRoll(characterBody.isChampion ? 3f : 1f, damageReport.attackerMaster))
		{
			GameObject obj = UnityEngine.Object.Instantiate(logbookPrefab, corePosition, UnityEngine.Random.rotation);
			obj.GetComponentInChildren<UnlockPickup>().unlockableDef = logUnlockableDef;
			obj.GetComponent<TeamFilter>().teamIndex = TeamIndex.Player;
			NetworkServer.Spawn(obj);
		}
	}

	[ConCommand(commandName = "migrate_death_rewards_unlockables", flags = ConVarFlags.Cheat, helpText = "Migrates CharacterDeath component .logUnlockableName to .LogUnlockableDef for all instances.")]
	private static void CCMigrateDeathRewardUnlockables(ConCommandArgs args)
	{
		DeathRewards[] array = Resources.FindObjectsOfTypeAll<DeathRewards>();
		foreach (DeathRewards deathRewards in array)
		{
			FieldInfo field = typeof(DeathRewards).GetField("logUnlockableName");
			string text = ((string)field.GetValue(deathRewards)) ?? string.Empty;
			UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(text);
			if (!unlockableDef && text != string.Empty)
			{
				args.Log("DeathRewards component on object " + deathRewards.gameObject.name + " has a defined value for 'logUnlockableName' but it doesn't map to any known unlockable. Migration skipped. logUnlockableName='logUnlockableName'");
			}
			else if (deathRewards.logUnlockableDef == unlockableDef)
			{
				field.SetValue(deathRewards, string.Empty);
				EditorUtil.SetDirty(deathRewards);
				EditorUtil.SetDirty(deathRewards.gameObject);
			}
			else if ((bool)deathRewards.logUnlockableDef)
			{
				args.Log($"DeathRewards component on object {deathRewards.gameObject.name} has a 'logUnlockableDef' field value which differs from the 'logUnlockableName' lookup. Migration skipped. logUnlockableDef={deathRewards.logUnlockableDef} logUnlockableName={text}");
			}
			else
			{
				deathRewards.logUnlockableDef = unlockableDef;
				field.SetValue(deathRewards, string.Empty);
				EditorUtil.SetDirty(deathRewards);
				EditorUtil.SetDirty(deathRewards.gameObject);
				args.Log($"DeathRewards component on object {deathRewards.gameObject.name} migrated. logUnlockableDef={deathRewards.logUnlockableDef} logUnlockableName={text}");
			}
		}
	}
}
