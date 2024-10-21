using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using EntityStates;
using EntityStates.Railgunner.Scope;
using HG;
using RoR2.Audio;
using RoR2.Items;
using RoR2.Navigation;
using RoR2.Networking;
using RoR2.PostProcessing;
using RoR2.Projectile;
using RoR2.Skills;
using Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace RoR2;

[DisallowMultipleComponent]
[RequireComponent(typeof(SkillLocator))]
[RequireComponent(typeof(TeamComponent))]
public class CharacterBody : NetworkBehaviour, ILifeBehavior, IDisplayNameProvider, IOnTakeDamageServerReceiver, IOnKilledOtherServerReceiver
{
	public static class CommonAssets
	{
		public static SkillDef lunarUtilityReplacementSkillDef;

		public static SkillDef lunarPrimaryReplacementSkillDef;

		public static SkillDef lunarSecondaryReplacementSkillDef;

		public static SkillDef lunarSpecialReplacementSkillDef;

		public static NetworkSoundEventDef nullifiedBuffAppliedSound;

		public static NetworkSoundEventDef pulverizeBuildupBuffAppliedSound;

		public static NetworkSoundEventDef[] procCritAttackSpeedSounds;

		public static GameObject thornExplosionEffect;

		public static GameObject teleportOnLowHealthVFX;

		public static GameObject teleportOnLowHealthExplosion;

		public static GameObject teleportOnLowHealthExplosionNova;

		public static GameObject prayerBeadEffect;

		public static GameObject goldOnStageStartEffect;

		public static void Load()
		{
			LegacyResourcesAPI.LoadAsyncCallback("NetworkSoundEventDefs/nseNullifiedBuffApplied", delegate(NetworkSoundEventDef asset)
			{
				nullifiedBuffAppliedSound = asset;
			});
			LegacyResourcesAPI.LoadAsyncCallback("NetworkSoundEventDefs/nsePulverizeBuildupBuffApplied", delegate(NetworkSoundEventDef asset)
			{
				pulverizeBuildupBuffAppliedSound = asset;
			});
			procCritAttackSpeedSounds = new NetworkSoundEventDef[3];
			LegacyResourcesAPI.LoadAsyncCallback("NetworkSoundEventDefs/nseProcCritAttackSpeed1", delegate(NetworkSoundEventDef asset)
			{
				procCritAttackSpeedSounds[0] = asset;
			});
			LegacyResourcesAPI.LoadAsyncCallback("NetworkSoundEventDefs/nseProcCritAttackSpeed2", delegate(NetworkSoundEventDef asset)
			{
				procCritAttackSpeedSounds[1] = asset;
			});
			LegacyResourcesAPI.LoadAsyncCallback("NetworkSoundEventDefs/nseProcCritAttackSpeed3", delegate(NetworkSoundEventDef asset)
			{
				procCritAttackSpeedSounds[2] = asset;
			});
			AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/NoxiousThornExplosion");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				thornExplosionEffect = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/TeleportOnLowHealthVFX");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				teleportOnLowHealthVFX = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/TeleportOnLowHealthExplosion");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				teleportOnLowHealthExplosion = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/VagrantNovaExplosion");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				teleportOnLowHealthExplosionNova = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ExtraStatsOnLevelUpScrapEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				prayerBeadEffect = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/GoldOnStageStartCoinGain");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				goldOnStageStartEffect = x.Result;
			};
			SkillCatalog.skillsDefined.CallWhenAvailable(delegate
			{
				lunarUtilityReplacementSkillDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("LunarUtilityReplacement"));
				lunarPrimaryReplacementSkillDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("LunarPrimaryReplacement"));
				lunarSecondaryReplacementSkillDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("LunarSecondaryReplacement"));
				lunarSpecialReplacementSkillDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("LunarDetonatorSpecialReplacement"));
			});
		}
	}

	private class TimedBuff
	{
		public BuffIndex buffIndex;

		public float timer;
	}

	public delegate void JumpDelegate();

	[Flags]
	public enum BodyFlags : uint
	{
		None = 0u,
		IgnoreFallDamage = 1u,
		Mechanical = 2u,
		Masterless = 4u,
		ImmuneToGoo = 8u,
		ImmuneToExecutes = 0x10u,
		SprintAnyDirection = 0x20u,
		ResistantToAOE = 0x40u,
		HasBackstabPassive = 0x80u,
		HasBackstabImmunity = 0x100u,
		OverheatImmune = 0x200u,
		Void = 0x400u,
		ImmuneToVoidDeath = 0x800u,
		IgnoreItemUpdates = 0x1000u,
		Devotion = 0x2000u,
		IgnoreKnockback = 0x4000u,
		ImmuneToLava = 0x8000u
	}

	private struct ItemAvailability
	{
		public int helfireCount;

		public bool hasFireTrail;

		public bool hasAffixLunar;

		public bool hasAffixPoison;
	}

	[Serializable]
	public struct NetworkItemBehaviorData
	{
		public int intItemValue;

		public float floatValue;

		public ItemIndex itemIndex
		{
			get
			{
				return (ItemIndex)intItemValue;
			}
			set
			{
				intItemValue = (int)value;
			}
		}

		public NetworkItemBehaviorData(ItemIndex _index, float _floatValue)
		{
			intItemValue = (int)_index;
			floatValue = _floatValue;
		}

		public NetworkItemBehaviorData(int _index, float _floatVal)
		{
			intItemValue = _index;
			floatValue = _floatVal;
		}
	}

	public class ItemBehavior : MonoBehaviour
	{
		public CharacterBody body;

		public int stack;
	}

	public class AffixHauntedBehavior : ItemBehavior
	{
		private GameObject affixHauntedWard;

		private void FixedUpdate()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			bool flag = stack > 0;
			if ((bool)affixHauntedWard != flag)
			{
				if (flag)
				{
					affixHauntedWard = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/AffixHauntedWard"));
					affixHauntedWard.GetComponent<TeamFilter>().teamIndex = body.teamComponent.teamIndex;
					affixHauntedWard.GetComponent<BuffWard>().Networkradius = 30f + body.radius;
					affixHauntedWard.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject);
				}
				else
				{
					UnityEngine.Object.Destroy(affixHauntedWard);
					affixHauntedWard = null;
				}
			}
		}

		private void OnDisable()
		{
			if ((bool)affixHauntedWard)
			{
				UnityEngine.Object.Destroy(affixHauntedWard);
			}
		}
	}

	public class QuestVolatileBatteryBehaviorServer : ItemBehavior
	{
		private NetworkedBodyAttachment attachment;

		private void Start()
		{
			attachment = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/QuestVolatileBatteryAttachment")).GetComponent<NetworkedBodyAttachment>();
			attachment.AttachToGameObjectAndSpawn(body.gameObject);
		}

		private void OnDestroy()
		{
			if ((bool)attachment)
			{
				UnityEngine.Object.Destroy(attachment.gameObject);
				attachment = null;
			}
		}
	}

	public class TimeBubbleItemBehaviorServer : ItemBehavior
	{
		private void OnDestroy()
		{
			if ((bool)body.timeBubbleWardInstance)
			{
				UnityEngine.Object.Destroy(body.timeBubbleWardInstance);
			}
		}
	}

	public class ElementalRingsBehavior : ItemBehavior
	{
		private void OnDisable()
		{
			if ((bool)body)
			{
				if (body.HasBuff(RoR2Content.Buffs.ElementalRingsReady))
				{
					body.RemoveBuff(RoR2Content.Buffs.ElementalRingsReady);
				}
				if (body.HasBuff(RoR2Content.Buffs.ElementalRingsCooldown))
				{
					body.RemoveBuff(RoR2Content.Buffs.ElementalRingsCooldown);
				}
			}
		}

		private void FixedUpdate()
		{
			bool flag = body.HasBuff(RoR2Content.Buffs.ElementalRingsCooldown);
			bool flag2 = body.HasBuff(RoR2Content.Buffs.ElementalRingsReady);
			if (!flag && !flag2)
			{
				body.AddBuff(RoR2Content.Buffs.ElementalRingsReady);
			}
			if (flag2 && flag)
			{
				body.RemoveBuff(RoR2Content.Buffs.ElementalRingsReady);
			}
		}
	}

	public class AffixEchoBehavior : ItemBehavior
	{
		private DeployableMinionSpawner echoSpawner1;

		private DeployableMinionSpawner echoSpawner2;

		private CharacterSpawnCard spawnCard;

		private List<CharacterMaster> spawnedEchoes = new List<CharacterMaster>();

		private void FixedUpdate()
		{
			spawnCard.nodeGraphType = (body.isFlying ? MapNodeGroup.GraphType.Air : MapNodeGroup.GraphType.Ground);
		}

		private void Awake()
		{
			base.enabled = false;
		}

		private void OnEnable()
		{
			MasterCatalog.MasterIndex masterIndex = MasterCatalog.FindAiMasterIndexForBody(body.bodyIndex);
			spawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
			spawnCard.prefab = MasterCatalog.GetMasterPrefab(masterIndex);
			spawnCard.inventoryToCopy = body.inventory;
			spawnCard.equipmentToGrant = new EquipmentDef[1];
			spawnCard.itemsToGrant = new ItemCountPair[1]
			{
				new ItemCountPair
				{
					itemDef = RoR2Content.Items.SummonedEcho,
					count = 1
				}
			};
			CreateSpawners();
		}

		private void OnDisable()
		{
			UnityEngine.Object.Destroy(spawnCard);
			spawnCard = null;
			for (int num = spawnedEchoes.Count - 1; num >= 0; num--)
			{
				if ((bool)spawnedEchoes[num])
				{
					spawnedEchoes[num].TrueKill();
				}
			}
			DestroySpawners();
		}

		private void CreateSpawners()
		{
			Xoroshiro128Plus rng = new Xoroshiro128Plus(Run.instance.seed ^ (ulong)GetInstanceID());
			CreateSpawner(ref echoSpawner1, DeployableSlot.RoboBallRedBuddy, spawnCard);
			CreateSpawner(ref echoSpawner2, DeployableSlot.RoboBallGreenBuddy, spawnCard);
			void CreateSpawner(ref DeployableMinionSpawner buddySpawner, DeployableSlot deployableSlot, SpawnCard spawnCard)
			{
				buddySpawner = new DeployableMinionSpawner(body.master, deployableSlot, rng)
				{
					respawnInterval = 30f,
					spawnCard = spawnCard
				};
				buddySpawner.onMinionSpawnedServer += OnMinionSpawnedServer;
			}
		}

		private void DestroySpawners()
		{
			echoSpawner1?.Dispose();
			echoSpawner1 = null;
			echoSpawner2?.Dispose();
			echoSpawner2 = null;
		}

		private void OnMinionSpawnedServer(SpawnCard.SpawnResult spawnResult)
		{
			GameObject spawnedInstance = spawnResult.spawnedInstance;
			if (!spawnedInstance)
			{
				return;
			}
			CharacterMaster spawnedMaster = spawnedInstance.GetComponent<CharacterMaster>();
			if ((bool)spawnedMaster)
			{
				spawnedEchoes.Add(spawnedMaster);
				OnDestroyCallback.AddCallback(spawnedMaster.gameObject, delegate
				{
					spawnedEchoes.Remove(spawnedMaster);
				});
			}
		}
	}

	private static class AssetReferences
	{
		public static GameObject engiShieldTempEffectPrefab;

		public static GameObject bucklerShieldTempEffectPrefab;

		public static GameObject slowDownTimeTempEffectPrefab;

		public static GameObject crippleEffectPrefab;

		public static GameObject tonicBuffEffectPrefab;

		public static GameObject weakTempEffectPrefab;

		public static GameObject energizedTempEffectPrefab;

		public static GameObject barrierTempEffectPrefab;

		public static GameObject nullifyStack1EffectPrefab;

		public static GameObject nullifyStack2EffectPrefab;

		public static GameObject nullifyStack3EffectPrefab;

		public static GameObject regenBoostEffectPrefab;

		public static GameObject elephantDefenseEffectPrefab;

		public static GameObject healingDisabledEffectPrefab;

		public static GameObject noCooldownEffectPrefab;

		public static GameObject doppelgangerEffectPrefab;

		public static GameObject deathmarkEffectPrefab;

		public static GameObject crocoRegenEffectPrefab;

		public static GameObject mercExposeEffectPrefab;

		public static GameObject lifestealOnHitEffectPrefab;

		public static GameObject teamWarCryEffectPrefab;

		public static GameObject randomDamageEffectPrefab;

		public static GameObject lunarGolemShieldEffectPrefab;

		public static GameObject warbannerEffectPrefab;

		public static GameObject teslaFieldEffectPrefab;

		public static GameObject lunarSecondaryRootEffectPrefab;

		public static GameObject lunarDetonatorEffectPrefab;

		public static GameObject fruitingEffectPrefab;

		public static GameObject mushroomVoidTempEffectPrefab;

		public static GameObject bearVoidTempEffectPrefab;

		public static GameObject outOfCombatArmorEffectPrefab;

		public static GameObject voidFogMildEffectPrefab;

		public static GameObject voidFogStrongEffectPrefab;

		public static GameObject voidJailerSlowEffectPrefab;

		public static GameObject voidRaidcrabWardWipeFogEffectPrefab;

		public static GameObject aurelioniteBlessingEffectInstance;

		public static GameObject permanentDebuffEffectPrefab;

		public static void Resolve()
		{
			AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/EngiShield");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				engiShieldTempEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/BucklerDefense");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				bucklerShieldTempEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/SlowDownTime");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				slowDownTimeTempEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/CrippleEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				crippleEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/TonicBuffEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				tonicBuffEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/WeakEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				weakTempEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/EnergizedEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				energizedTempEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/BarrierEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				barrierTempEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/RegenBoostEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				regenBoostEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/ElephantDefense");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				elephantDefenseEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/HealingDisabledEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				healingDisabledEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/NoCooldownEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				noCooldownEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/DoppelgangerEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				doppelgangerEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/NullifyStack1Effect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				nullifyStack1EffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/NullifyStack2Effect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				nullifyStack2EffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/NullifyStack3Effect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				nullifyStack3EffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/DeathMarkEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				deathmarkEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/CrocoRegenEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				crocoRegenEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/MercExposeEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				mercExposeEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/LifeStealOnHitAura");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				lifestealOnHitEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/TeamWarCryAura");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				teamWarCryEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/LunarDefense");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				lunarGolemShieldEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/RandomDamageBuffEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				randomDamageEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/WarbannerBuffEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				warbannerEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/TeslaFieldBuffEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				teslaFieldEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/LunarSecondaryRootEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				lunarSecondaryRootEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/LunarDetonatorEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				lunarDetonatorEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/FruitingEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				fruitingEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/MushroomVoidEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				mushroomVoidTempEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/BearVoidEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				bearVoidTempEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/VoidFogMildEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				voidFogMildEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/VoidFogStrongEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				voidFogStrongEffectPrefab = x.Result;
			};
			asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidRaidCrab/VoidRaidCrabWardWipeFogEffect.prefab");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				voidRaidcrabWardWipeFogEffectPrefab = x.Result;
			};
			asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OutOfCombatArmor/OutOfCombatArmorEffect.prefab");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				outOfCombatArmorEffectPrefab = x.Result;
			};
			asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidJailer/VoidJailerTetherDebuff.prefab");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				voidJailerSlowEffectPrefab = x.Result;
			};
			asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Elites/EliteAurelionite/AffixAurelioniteArmorBubble.prefab");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				aurelioniteBlessingEffectInstance = x.Result;
			};
		}
	}

	private class ConstructTurretMessage : MessageBase
	{
		public GameObject builder;

		public Vector3 position;

		public Quaternion rotation;

		public MasterCatalog.NetworkMasterIndex turretMasterIndex;

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write(builder);
			writer.Write(position);
			writer.Write(rotation);
			GeneratedNetworkCode._WriteNetworkMasterIndex_MasterCatalog(writer, turretMasterIndex);
		}

		public override void Deserialize(NetworkReader reader)
		{
			builder = reader.ReadGameObject();
			position = reader.ReadVector3();
			rotation = reader.ReadQuaternion();
			turretMasterIndex = GeneratedNetworkCode._ReadNetworkMasterIndex_MasterCatalog(reader);
		}
	}

	private class DelayedDamageInfo
	{
		public DamageInfo halfDamage = new DamageInfo();

		public float timeUntilDamage = 3f;
	}

	public class TeleportOnLowHealthBehavior : ItemBehavior
	{
		private CharacterBody characterBody;

		private bool hasActivated;

		private bool hasItem;

		private bool hasBuff;

		private bool hasCooldown;

		private void Start()
		{
			if (!hasActivated)
			{
				characterBody = GetComponent<CharacterBody>();
				characterBody.RemoveBuff(DLC2Content.Buffs.TeleportOnLowHealth);
				characterBody.RemoveOldestTimedBuff(DLC2Content.Buffs.TeleportOnLowHealthCooldown);
				characterBody.RemoveBuff(DLC2Content.Buffs.TeleportOnLowHealthCooldown);
				characterBody.AddBuff(DLC2Content.Buffs.TeleportOnLowHealth);
				characterBody.hasTeleported = false;
				hasActivated = true;
			}
		}

		private void Update()
		{
			hasItem = characterBody.inventory.GetItemCount(DLC2Content.Items.TeleportOnLowHealth) > 0;
			hasBuff = characterBody.HasBuff(DLC2Content.Buffs.TeleportOnLowHealth);
			hasCooldown = characterBody.HasBuff(DLC2Content.Buffs.TeleportOnLowHealthCooldown);
			if (!hasItem)
			{
				if (hasBuff)
				{
					characterBody.RemoveBuff(DLC2Content.Buffs.TeleportOnLowHealth);
				}
				if (hasCooldown)
				{
					characterBody.RemoveBuff(DLC2Content.Buffs.TeleportOnLowHealthCooldown);
				}
			}
			else if (!hasBuff && !hasCooldown && characterBody.hasTeleported)
			{
				characterBody.AddBuff(DLC2Content.Buffs.TeleportOnLowHealth);
				characterBody.hasTeleported = false;
			}
		}

		private void OnDisable()
		{
			characterBody.RemoveBuff(DLC2Content.Buffs.TeleportOnLowHealth);
			characterBody.RemoveOldestTimedBuff(DLC2Content.Buffs.TeleportOnLowHealthCooldown);
			characterBody.RemoveBuff(DLC2Content.Buffs.TeleportOnLowHealthCooldown);
		}
	}

	public class DelayedDamageBehavior : ItemBehavior
	{
		private CharacterBody characterBody;

		private bool hasActivated;

		private bool hasItem;

		private bool hasBuff;

		private bool hasCooldown;

		private void Start()
		{
			if (!hasActivated)
			{
				characterBody = GetComponent<CharacterBody>();
				hasActivated = true;
			}
		}

		private void Update()
		{
			hasItem = characterBody.inventory.GetItemCount(DLC2Content.Items.DelayedDamage) > 0;
			hasBuff = characterBody.HasBuff(DLC2Content.Buffs.DelayedDamageBuff);
			hasCooldown = characterBody.HasBuff(DLC2Content.Buffs.DelayedDamageBuff);
			if (!hasItem)
			{
				if (hasBuff)
				{
					characterBody.RemoveBuff(DLC2Content.Buffs.DelayedDamageBuff);
					characterBody.RemoveOldestTimedBuff(DLC2Content.Buffs.DelayedDamageBuff);
				}
				if (hasCooldown)
				{
					characterBody.RemoveBuff(DLC2Content.Buffs.DelayedDamageBuff);
					characterBody.RemoveOldestTimedBuff(DLC2Content.Buffs.DelayedDamageBuff);
					characterBody.RemoveBuff(DLC2Content.Buffs.DelayedDamageDebuff);
				}
			}
		}
	}

	public class MeteorAttackOnHighDamageBehavior : ItemBehavior
	{
		private CharacterBody characterBody;

		private bool hasActivated;

		private void FixedUpdate()
		{
			if (!hasActivated)
			{
				characterBody = GetComponent<CharacterBody>();
				hasActivated = true;
			}
			if (characterBody.runicLensMeteorReady)
			{
				characterBody.runicLensStartTime = Run.instance.time;
				if (characterBody.runicLensStartTime >= characterBody.runicLensImpactTime && characterBody.runicLensMeteorReady)
				{
					characterBody.detonateRunicLensMeteor();
					characterBody.runicLensMeteorReady = false;
				}
			}
		}
	}

	[Serializable]
	public class CharacterBodyUnityEvent : UnityEvent<CharacterBody>
	{
	}

	[HideInInspector]
	[Tooltip("This is assigned to the prefab automatically by BodyCatalog at runtime. Do not set this value manually.")]
	public BodyIndex bodyIndex = BodyIndex.None;

	public bool CharacterIsVisible;

	[Tooltip("How much the combat director paid for this character. If this enemy is culled, then we refund the combat director for the cost it paid to spawn us.")]
	public float cost = -1f;

	public bool dontCull;

	[Tooltip("The baseline importance for this character. 0 is standard, elites get a +2 modifier, bosses get +10, players get +5000. Characters of low importance are culled in some circumstances.")]
	public int BaseImportance;

	[Tooltip("The dynamically calculated level of importance for this enemy.")]
	public int Importance = -1;

	public bool inLava;

	[Tooltip("The language token to use as the base name of this character.")]
	public string baseNameToken;

	public string subtitleNameToken;

	private BuffIndex[] activeBuffsList;

	private int activeBuffsListCount;

	private int[] buffs;

	private int eliteBuffCount;

	private List<TimedBuff> timedBuffs = new List<TimedBuff>();

	[NonSerialized]
	public int pendingTonicAfflictionCount;

	public JumpDelegate onJump;

	private int previousMultiKillBuffCount;

	private GameObject warCryEffectInstance;

	[EnumMask(typeof(BodyFlags))]
	public BodyFlags bodyFlags;

	private NetworkInstanceId masterObjectId;

	private GameObject _masterObject;

	private CharacterMaster _master;

	private bool linkedToMaster;

	private bool disablingHurtBoxes;

	private EquipmentIndex previousEquipmentIndex = EquipmentIndex.None;

	private new Transform transform;

	private SfxLocator sfxLocator;

	public float lavaCooldown = 0.2f;

	private float lavaTimer;

	private ItemAvailability itemAvailability;

	private static List<CharacterBody> instancesList;

	public static readonly ReadOnlyCollection<CharacterBody> readOnlyInstancesList;

	private bool _isSprinting;

	private const float outOfCombatDelay = 5f;

	private const float outOfDangerDelay = 7f;

	private float outOfCombatStopwatch;

	private float outOfDangerStopwatch;

	private bool _outOfDanger = true;

	private Vector3 previousPosition;

	private const float notMovingWait = 1f;

	private float notMovingStopwatch;

	public bool rootMotionInMainState;

	public float mainRootSpeed;

	public float baseMaxHealth;

	public float baseRegen;

	public float baseMaxShield;

	public float baseMoveSpeed;

	public float baseAcceleration;

	public float baseJumpPower;

	public float baseDamage;

	public float baseAttackSpeed;

	public float baseCrit;

	public float baseArmor;

	public float baseVisionDistance = float.PositiveInfinity;

	public int baseJumpCount = 1;

	public float sprintingSpeedMultiplier = 1.45f;

	public bool autoCalculateLevelStats;

	public float levelMaxHealth;

	public float levelRegen;

	public float levelMaxShield;

	public float levelMoveSpeed;

	public float levelJumpPower;

	public float levelDamage;

	public float levelAttackSpeed;

	public float levelCrit;

	public float levelArmor;

	private float m_surfaceSpeedBoost;

	private bool statsDirty;

	private int currentHealthLevel;

	private int oldHealthLevel;

	private float damageFromRecalculateStats;

	private int numberOfKills;

	private bool canCleanInventory = true;

	public int extraSecondaryFromSkill;

	public int extraSpecialFromSkill;

	private GameObject lowerHealthHigherDamageSteam;

	[HideInInspector]
	public LowerHealthHigherDamageEffectUpdater lowerHealthHigherDamageEffectUpdater;

	private bool boostAllStatsTimerStarted;

	private bool boostAllStatsCoolDownTimerStarted;

	private float boostAllStatsTimer;

	private float boostAllStatsTriggerTimer = 5f;

	private float boostAllStatsCoolDownTriggerTimer = 10f;

	private float boostAllStatsCoolDownStartTimer;

	private float boostAllStatsCoolDownTimer;

	private float boostAllStatsStartTimer;

	private float boostAllStatsTriggerChance;

	public bool canBoostAllStats;

	private float boostAllStatsDefaultTriggerChance = 0.25f;

	private float boostAllStatsTriggerIncrease = 0.1f;

	private float boostAllStatsMultiplier = 0.2f;

	private int extraStatsOnLevelUpCountModifier;

	[HideInInspector]
	public IncreaseDamageOnMultiKillItemDisplayUpdater increaseDamageOnMultiKillItemDisplayUpdater;

	private bool lowerPricedChestsActive;

	private bool canPurchaseLoweredPricedChests;

	public bool allSkillsDisabled;

	private ScreenDamageCalculatorDisabledSkills screenDamageSkillsDisabled;

	private int oldComboMeter;

	public bool isTeleporting;

	public int solitudeLevelCount;

	private float aimTimer;

	private const uint masterDirtyBit = 1u;

	private const uint buffsDirtyBit = 2u;

	private const uint outOfCombatBit = 4u;

	private const uint outOfDangerBit = 8u;

	private const uint sprintingBit = 16u;

	private const uint allDirtyBits = 31u;

	public Action<NetworkItemBehaviorData> OnNetworkItemBehaviorUpdate;

	private HelfireController helfireController;

	private float helfireLifetime;

	private DamageTrail fireTrail;

	public bool wasLucky;

	private const float poisonballAngle = 25f;

	private const float poisonballDamageCoefficient = 1f;

	private const float poisonballRefreshTime = 6f;

	private float poisonballTimer;

	private const float lunarMissileDamageCoefficient = 0.3f;

	private const float lunarMissileRefreshTime = 10f;

	private const float lunarMissileDelayBetweenShots = 0.1f;

	private float lunarMissileRechargeTimer = 10f;

	private float lunarMissileTimerBetweenShots;

	private int remainingMissilesToFire;

	private GameObject lunarMissilePrefab;

	private GameObject timeBubbleWardInstance;

	private TemporaryVisualEffect engiShieldTempEffectInstance;

	private TemporaryVisualEffect bucklerShieldTempEffectInstance;

	private TemporaryVisualEffect slowDownTimeTempEffectInstance;

	private TemporaryVisualEffect crippleEffectInstance;

	private TemporaryVisualEffect tonicBuffEffectInstance;

	private TemporaryVisualEffect weakTempEffectInstance;

	private TemporaryVisualEffect energizedTempEffectInstance;

	private TemporaryVisualEffect barrierTempEffectInstance;

	private TemporaryVisualEffect nullifyStack1EffectInstance;

	private TemporaryVisualEffect nullifyStack2EffectInstance;

	private TemporaryVisualEffect nullifyStack3EffectInstance;

	private TemporaryVisualEffect regenBoostEffectInstance;

	private TemporaryVisualEffect elephantDefenseEffectInstance;

	private TemporaryVisualEffect healingDisabledEffectInstance;

	private TemporaryVisualEffect noCooldownEffectInstance;

	private TemporaryVisualEffect doppelgangerEffectInstance;

	private TemporaryVisualEffect deathmarkEffectInstance;

	private TemporaryVisualEffect crocoRegenEffectInstance;

	private TemporaryVisualEffect mercExposeEffectInstance;

	private TemporaryVisualEffect lifestealOnHitEffectInstance;

	private TemporaryVisualEffect teamWarCryEffectInstance;

	private TemporaryVisualEffect randomDamageEffectInstance;

	private TemporaryVisualEffect lunarGolemShieldEffectInstance;

	private TemporaryVisualEffect warbannerEffectInstance;

	private TemporaryVisualEffect teslaFieldEffectInstance;

	private TemporaryVisualEffect lunarSecondaryRootEffectInstance;

	private TemporaryVisualEffect lunarDetonatorEffectInstance;

	private TemporaryVisualEffect fruitingEffectInstance;

	private TemporaryVisualEffect mushroomVoidTempEffectInstance;

	private TemporaryVisualEffect bearVoidTempEffectInstance;

	private TemporaryVisualEffect outOfCombatArmorEffectInstance;

	private TemporaryVisualEffect voidFogMildEffectInstance;

	private TemporaryVisualEffect voidFogStrongEffectInstance;

	private TemporaryVisualEffect voidJailerSlowEffectInstance;

	private TemporaryVisualEffect voidRaidcrabWardWipeFogEffectInstance;

	private TemporaryVisualEffect aurelioniteBlessingEffectInstance;

	[Tooltip("How long it takes for spread bloom to reset from full.")]
	public float spreadBloomDecayTime = 0.45f;

	[Tooltip("The spread bloom interpretation curve.")]
	public AnimationCurve spreadBloomCurve;

	private float spreadBloomInternal;

	[FormerlySerializedAs("crosshairPrefab")]
	[SerializeField]
	[Tooltip("The crosshair prefab used for this body.")]
	private GameObject _defaultCrosshairPrefab;

	[HideInInspector]
	public bool hideCrosshair;

	private const float multiKillMaxInterval = 1f;

	private float multiKillTimer;

	private const int multiKillThresholdForWarcry = 4;

	private const float increasedDamageMultiKillMaxInterval = 5f;

	private float increasedDamageKillTimer;

	private int oldKillcount;

	private const int multiKillThresholdForIncreasedDamage = 5;

	private int secondarySkillStacks;

	[HideInInspector]
	public IncreasePrimaryDamageEffectUpdater increasePrimaryDamageEffectUpdater;

	private float delayedDamageRefreshTime = 10f;

	private float secondHalfOfDamageTime = 3f;

	private int oldDelayedDamageCount;

	private bool halfDamageReady;

	private float halfDamageTimer;

	public bool protectFromOneShot;

	private DamageInfo secondHalfOfDamage;

	private List<DelayedDamageInfo> incomingDamageList = new List<DelayedDamageInfo>();

	public bool hasTeleported;

	private Vector3 runicLensPosition;

	private EffectData runicLensEffectData = new EffectData();

	private DamageInfo runicLensDamageInfo = new DamageInfo();

	private float runicLensDamage;

	private float runicLensBlastForce;

	private float runicLensBlastRadius;

	private float runicLensStartTime;

	private float runicLensImpactTime;

	public bool runicLensMeteorReady;

	private bool hasStackableDebuff;

	private bool tamperedHeartActive;

	private float tamperedHeartRegenBonus;

	private float tamperedHeartMSpeedBonus;

	private float tamperedHeartDamageBonus;

	private float tamperedHeartAttackSpeedBonus;

	private float tamperedHeartArmorBonus;

	[Tooltip("The child transform to be used as the aiming origin.")]
	public Transform aimOriginTransform;

	[Tooltip("The hull size to use when pathfinding for this object.")]
	public HullClassification hullClassification;

	[Tooltip("The icon displayed for ally healthbars")]
	public Texture portraitIcon;

	[Tooltip("The main color of the body. Currently only used in the logbook.")]
	public Color bodyColor = Color.clear;

	[Tooltip("By default, players and enemies are assigned to either PlayerBody or EnemyBody, based on team. But some enemies (Gups) want a specific layer - this will skip that assignment.")]
	public bool doNotReassignToTeamBasedCollisionLayer;

	[FormerlySerializedAs("isBoss")]
	[Tooltip("Whether or not this is a boss for dropping items on death.")]
	public bool isChampion;

	public VehicleSeat currentVehicle;

	[Tooltip("The pod prefab to use for handling this character's first-time spawn animation.")]
	public GameObject preferredPodPrefab;

	[Tooltip("The preferred state to use for handling the character's first-time spawn animation. Only used with no preferred pod prefab.")]
	public SerializableEntityStateType preferredInitialStateType = new SerializableEntityStateType(typeof(Uninitialized));

	public uint skinIndex;

	public string customKillTotalStatName;

	public Transform overrideCoreTransform;

	private static int kCmdCmdAddTimedBuff;

	private static int kCmdCmdSetInLava;

	private static int kCmdCmdUpdateSprint;

	private static int kCmdCmdOnSkillActivated;

	private static int kCmdCmdTransmitItemBehavior;

	private static int kRpcRpcTransmitItemBehavior;

	private static int kCmdCmdSpawnDelayedDamageEffect;

	private static int kRpcRpcTeleportCharacterToSafety;

	private static int kRpcRpcBark;

	private static int kCmdCmdRequestVehicleEjection;

	private static int kRpcRpcUsePreferredInitialStateType;

	public CharacterMaster master
	{
		get
		{
			if (!masterObject)
			{
				return null;
			}
			return _master;
		}
	}

	public Inventory inventory { get; private set; }

	public bool isPlayerControlled { get; private set; }

	public float executeEliteHealthFraction { get; private set; }

	public GameObject masterObject
	{
		get
		{
			if (!_masterObject)
			{
				if (NetworkServer.active)
				{
					_masterObject = NetworkServer.FindLocalObject(masterObjectId);
				}
				else if (NetworkClient.active)
				{
					_masterObject = ClientScene.FindLocalObject(masterObjectId);
				}
				_master = (_masterObject ? _masterObject.GetComponent<CharacterMaster>() : null);
				if ((bool)_master)
				{
					PlayerCharacterMasterController component = _masterObject.GetComponent<PlayerCharacterMasterController>();
					isPlayerControlled = component;
					if ((bool)inventory)
					{
						inventory.onInventoryChanged -= OnInventoryChanged;
					}
					inventory = _master.inventory;
					if ((bool)inventory)
					{
						inventory.onInventoryChanged += OnInventoryChanged;
						OnInventoryChanged();
					}
					statsDirty = true;
				}
			}
			return _masterObject;
		}
		set
		{
			masterObjectId = value.GetComponent<NetworkIdentity>().netId;
			statsDirty = true;
		}
	}

	public Rigidbody rigidbody { get; private set; }

	public NetworkIdentity networkIdentity { get; private set; }

	public CharacterMotor characterMotor { get; private set; }

	public CharacterDirection characterDirection { get; private set; }

	public TeamComponent teamComponent { get; private set; }

	public HealthComponent healthComponent { get; private set; }

	public EquipmentSlot equipmentSlot { get; private set; }

	public InputBankTest inputBank { get; private set; }

	public SkillLocator skillLocator { get; private set; }

	public ModelLocator modelLocator { get; private set; }

	public HurtBoxGroup hurtBoxGroup { get; private set; }

	public HurtBox mainHurtBox { get; private set; }

	public Transform coreTransform { get; private set; }

	public bool hasEffectiveAuthority { get; private set; }

	public bool isSprinting
	{
		get
		{
			return _isSprinting;
		}
		set
		{
			if (_isSprinting != value)
			{
				_isSprinting = value;
				RecalculateStats();
				if (value)
				{
					OnSprintStart();
				}
				else
				{
					OnSprintStop();
				}
				if (NetworkServer.active)
				{
					SetDirtyBit(16u);
				}
				else if (hasEffectiveAuthority)
				{
					CallCmdUpdateSprint(value);
				}
			}
		}
	}

	public bool outOfCombat { get; private set; } = true;

	public bool outOfDanger
	{
		get
		{
			return _outOfDanger;
		}
		private set
		{
			if (_outOfDanger != value)
			{
				_outOfDanger = value;
				OnOutOfDangerChanged();
			}
		}
	}

	public float experience { get; private set; }

	public float level { get; private set; }

	public float maxHealth { get; private set; }

	public float maxBarrier { get; private set; }

	public float barrierDecayRate { get; private set; }

	public float regen { get; private set; }

	public float maxShield { get; private set; }

	public float moveSpeed { get; private set; }

	public float acceleration { get; private set; }

	public float surfaceSpeedBoost
	{
		get
		{
			return m_surfaceSpeedBoost;
		}
		set
		{
			if (!Mathf.Approximately(m_surfaceSpeedBoost, value))
			{
				m_surfaceSpeedBoost = value;
				RecalculateStats();
			}
		}
	}

	public float jumpPower { get; private set; }

	public int maxJumpCount { get; private set; }

	public float maxJumpHeight { get; private set; }

	public float damage { get; private set; }

	public float attackSpeed { get; private set; }

	public float crit { get; private set; }

	public float critMultiplier { get; private set; }

	public float bleedChance { get; private set; }

	public float armor { get; private set; }

	public float visionDistance { get; private set; }

	public float critHeal { get; private set; }

	public float cursePenalty { get; private set; }

	public bool hasOneShotProtection { get; private set; }

	public bool isGlass { get; private set; }

	public float oneShotProtectionFraction { get; private set; }

	public bool canPerformBackstab { get; private set; }

	public bool canReceiveBackstab { get; private set; }

	public float maxBonusHealth { get; private set; }

	public bool shouldAim
	{
		get
		{
			if (aimTimer > 0f)
			{
				return !isSprinting;
			}
			return false;
		}
	}

	public int killCountServer { get; private set; }

	public float bestFitRadius => Mathf.Max(radius, characterMotor ? characterMotor.capsuleHeight : 1f);

	public float bestFitActualRadius => Mathf.Max(radius, characterMotor ? (characterMotor.capsuleHeight * 0.5f) : 1f);

	public bool hasCloakBuff
	{
		get
		{
			if (!HasBuff(RoR2Content.Buffs.Cloak))
			{
				return HasBuff(RoR2Content.Buffs.AffixHauntedRecipient);
			}
			return true;
		}
	}

	public float spreadBloomAngle => spreadBloomCurve.Evaluate(spreadBloomInternal);

	public GameObject defaultCrosshairPrefab => _defaultCrosshairPrefab;

	public int multiKillCount { get; private set; }

	public int increasedDamageKillCount { get; private set; }

	public int increasedDamageKillCountBuff { get; private set; }

	public float luminousShotDamage { get; private set; }

	public bool luminousShotReady { get; private set; }

	public Vector3 corePosition
	{
		get
		{
			if (!coreTransform)
			{
				return transform.position;
			}
			return coreTransform.position;
		}
	}

	public Vector3 footPosition
	{
		get
		{
			Vector3 position = transform.position;
			if ((bool)characterMotor)
			{
				position.y -= characterMotor.capsuleHeight * 0.5f;
			}
			return position;
		}
	}

	public float radius { get; private set; }

	public Vector3 aimOrigin
	{
		get
		{
			if (!aimOriginTransform)
			{
				return corePosition;
			}
			return aimOriginTransform.position;
		}
	}

	public bool isElite { get; private set; }

	public bool isBoss
	{
		get
		{
			if ((bool)master)
			{
				return master.isBoss;
			}
			return false;
		}
	}

	public bool isFlying
	{
		get
		{
			if ((bool)characterMotor)
			{
				return characterMotor.isFlying;
			}
			return true;
		}
	}

	public Run.FixedTimeStamp localStartTime { get; private set; } = Run.FixedTimeStamp.positiveInfinity;

	public bool isEquipmentActivationAllowed
	{
		get
		{
			if ((bool)currentVehicle)
			{
				return currentVehicle.isEquipmentActivationAllowed;
			}
			return true;
		}
	}

	public event Action onInventoryChanged;

	public event Action<GenericSkill> onSkillActivatedServer;

	public event Action<GenericSkill> onSkillActivatedAuthority;

	public static event Action<CharacterBody> onBodyAwakeGlobal;

	public static event Action<CharacterBody> onBodyDestroyGlobal;

	public static event Action<CharacterBody> onBodyStartGlobal;

	public static event Action<CharacterBody> onBodyInventoryChangedGlobal;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CharacterBody GetBody()
	{
		return this;
	}

	public void DebugDrawLine(Vector3 start, Vector3 end, Color color)
	{
		Debug.DrawLine(start, end, color, 5f);
	}

	[InitDuringStartup]
	private static void LoadCommonAssets()
	{
		CommonAssets.Load();
	}

	public string GetDisplayName()
	{
		return Language.GetString(baseNameToken);
	}

	public string GetSubtitle()
	{
		return Language.GetString(subtitleNameToken);
	}

	public string GetUserName()
	{
		string text = "";
		if ((bool)master)
		{
			PlayerCharacterMasterController component = master.GetComponent<PlayerCharacterMasterController>();
			if ((bool)component)
			{
				text = component.GetDisplayName();
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			text = GetDisplayName();
		}
		return text;
	}

	public string GetColoredUserName()
	{
		Color32 color = new Color32(127, 127, 127, byte.MaxValue);
		string text = null;
		if ((bool)master)
		{
			PlayerCharacterMasterController component = master.GetComponent<PlayerCharacterMasterController>();
			if ((bool)component)
			{
				GameObject networkUserObject = component.networkUserObject;
				if ((bool)networkUserObject)
				{
					NetworkUser component2 = networkUserObject.GetComponent<NetworkUser>();
					if ((bool)component2)
					{
						color = component2.userColor;
						text = component2.userName;
					}
				}
			}
		}
		if (text == null)
		{
			text = GetDisplayName();
		}
		return Util.GenerateColoredString(text, color);
	}

	[Server]
	private void WriteBuffs(NetworkWriter writer)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::WriteBuffs(UnityEngine.Networking.NetworkWriter)' called on client");
			return;
		}
		writer.Write((byte)activeBuffsListCount);
		for (int i = 0; i < activeBuffsListCount; i++)
		{
			BuffIndex buffIndex = activeBuffsList[i];
			BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
			writer.WriteBuffIndex(buffIndex);
			if (buffDef.canStack)
			{
				writer.WritePackedUInt32((uint)buffs[(int)buffIndex]);
			}
		}
	}

	[Client]
	private void ReadBuffs(NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.CharacterBody::ReadBuffs(UnityEngine.Networking.NetworkReader)' called on server");
			return;
		}
		if (activeBuffsList == null)
		{
			Debug.LogError("Trying to ReadBuffs, but our activeBuffsList is null");
			return;
		}
		int activeBuffsIndexToCheck = 0;
		int num = reader.ReadByte();
		BuffIndex buffIndex = BuffIndex.None;
		for (int i = 0; i < num; i++)
		{
			BuffIndex buffIndex2 = reader.ReadBuffIndex();
			BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex2);
			if (buffDef != null)
			{
				int num2 = 1;
				if (buffDef.canStack)
				{
					num2 = (int)reader.ReadPackedUInt32();
				}
				if (num2 > 0 && !NetworkServer.active)
				{
					ZeroBuffIndexRange(buffIndex + 1, buffIndex2);
					SetBuffCount(buffIndex2, num2);
				}
				buffIndex = buffIndex2;
			}
			else
			{
				Debug.LogErrorFormat("No BuffDef for index {0}. body={1}, netID={2}", buffIndex2, base.gameObject, base.netId);
			}
		}
		if (!NetworkServer.active)
		{
			ZeroBuffIndexRange(buffIndex + 1, (BuffIndex)BuffCatalog.buffCount);
		}
		void ZeroBuffIndexRange(BuffIndex start, BuffIndex end)
		{
			while (activeBuffsIndexToCheck < activeBuffsListCount)
			{
				BuffIndex buffIndex3 = activeBuffsList[activeBuffsIndexToCheck];
				if (end <= buffIndex3)
				{
					break;
				}
				int num3;
				if (start <= buffIndex3)
				{
					SetBuffCount(buffIndex3, 0);
					num3 = activeBuffsIndexToCheck - 1;
					activeBuffsIndexToCheck = num3;
				}
				num3 = activeBuffsIndexToCheck + 1;
				activeBuffsIndexToCheck = num3;
			}
		}
	}

	[Server]
	public void AddBuff(BuffIndex buffType)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::AddBuff(RoR2.BuffIndex)' called on client");
		}
		else if (buffType != BuffIndex.None)
		{
			SetBuffCount(buffType, buffs[(int)buffType] + 1);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Server]
	public void AddBuff(BuffDef buffDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::AddBuff(RoR2.BuffDef)' called on client");
		}
		else
		{
			AddBuff(buffDef?.buffIndex ?? BuffIndex.None);
		}
	}

	[Server]
	public void RemoveBuff(BuffIndex buffType)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::RemoveBuff(RoR2.BuffIndex)' called on client");
		}
		else
		{
			if (buffType == BuffIndex.None)
			{
				return;
			}
			SetBuffCount(buffType, buffs[(int)buffType] - 1);
			if (buffType == RoR2Content.Buffs.MedkitHeal.buffIndex)
			{
				if (GetBuffCount(RoR2Content.Buffs.MedkitHeal.buffIndex) == 0)
				{
					int itemCount = inventory.GetItemCount(RoR2Content.Items.Medkit);
					float num = 20f;
					float num2 = maxHealth * 0.05f * (float)itemCount;
					healthComponent.Heal(num + num2, default(ProcChainMask));
					EffectData effectData = new EffectData
					{
						origin = transform.position
					};
					effectData.SetNetworkedObjectReference(base.gameObject);
					EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MedkitHealEffect"), effectData, transmit: true);
				}
			}
			else if (buffType == RoR2Content.Buffs.TonicBuff.buffIndex)
			{
				if ((bool)inventory && GetBuffCount(RoR2Content.Buffs.TonicBuff) == 0 && pendingTonicAfflictionCount > 0)
				{
					inventory.GiveItem(RoR2Content.Items.TonicAffliction, pendingTonicAfflictionCount);
					PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.TonicAffliction.itemIndex);
					GenericPickupController.SendPickupMessage(master, pickupIndex);
					pendingTonicAfflictionCount = 0;
				}
			}
			else if (buffType == DLC2Content.Buffs.SoulCost.buffIndex)
			{
				cursePenalty -= 0.1f * (float)GetBuffCount(DLC2Content.Buffs.SoulCost);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Server]
	public void RemoveBuff(BuffDef buffDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::RemoveBuff(RoR2.BuffDef)' called on client");
		}
		else
		{
			RemoveBuff(buffDef?.buffIndex ?? BuffIndex.None);
		}
	}

	public void SetBuffCount(BuffIndex buffType, int newCount)
	{
		newCount = Mathf.Max(newCount, 0);
		ref int reference = ref buffs[(int)buffType];
		if (newCount == reference)
		{
			return;
		}
		int num = reference;
		reference = newCount;
		BuffDef buffDef = BuffCatalog.GetBuffDef(buffType);
		bool flag = true;
		if (!buffDef.canStack)
		{
			flag = num == 0 != (newCount == 0);
		}
		if (flag)
		{
			if (newCount == 0)
			{
				ArrayUtils.ArrayRemoveAt(activeBuffsList, ref activeBuffsListCount, Array.IndexOf(activeBuffsList, buffType));
				OnBuffFinalStackLost(buffDef);
			}
			else if (num == 0)
			{
				int i;
				for (i = 0; i < activeBuffsListCount && buffType >= activeBuffsList[i]; i++)
				{
				}
				ArrayUtils.ArrayInsert(ref activeBuffsList, ref activeBuffsListCount, i, in buffType);
				OnBuffFirstStackGained(buffDef);
			}
			if (NetworkServer.active)
			{
				SetDirtyBit(2u);
			}
		}
		statsDirty = true;
		if (NetworkClient.active)
		{
			OnClientBuffsChanged();
		}
		UpdateItemAvailability();
	}

	private void OnBuffFirstStackGained(BuffDef buffDef)
	{
		if (buffDef.isElite)
		{
			eliteBuffCount++;
		}
		if (buffDef == RoR2Content.Buffs.Intangible)
		{
			UpdateHurtBoxesEnabled();
		}
		else if (buffDef == RoR2Content.Buffs.WarCryBuff)
		{
			if (HasBuff(RoR2Content.Buffs.TeamWarCry))
			{
				ClearTimedBuffs(RoR2Content.Buffs.TeamWarCry);
			}
		}
		else if (buffDef == RoR2Content.Buffs.TeamWarCry)
		{
			if (HasBuff(RoR2Content.Buffs.WarCryBuff))
			{
				ClearTimedBuffs(RoR2Content.Buffs.WarCryBuff);
			}
		}
		else if (buffDef == RoR2Content.Buffs.AffixEcho && NetworkServer.active)
		{
			AddItemBehavior<AffixEchoBehavior>(1);
		}
	}

	private void OnBuffFinalStackLost(BuffDef buffDef)
	{
		if (buffDef.isElite)
		{
			eliteBuffCount--;
		}
		if (buffDef.buffIndex == RoR2Content.Buffs.Intangible.buffIndex)
		{
			UpdateHurtBoxesEnabled();
		}
		else if (buffDef == RoR2Content.Buffs.AffixEcho && NetworkServer.active)
		{
			AddItemBehavior<AffixEchoBehavior>(0);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetBuffCount(BuffIndex buffType)
	{
		return ArrayUtils.GetSafe(buffs, (int)buffType);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetBuffCount(BuffDef buffDef)
	{
		return GetBuffCount(buffDef?.buffIndex ?? BuffIndex.None);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool HasBuff(BuffIndex buffType)
	{
		return GetBuffCount(buffType) > 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool HasBuff(BuffDef buffDef)
	{
		return HasBuff(buffDef?.buffIndex ?? BuffIndex.None);
	}

	public void AddTimedBuffAuthority(BuffIndex buffType, float duration)
	{
		if (NetworkServer.active)
		{
			AddTimedBuff(buffType, duration);
		}
		else
		{
			CallCmdAddTimedBuff(buffType, duration);
		}
	}

	[Command]
	public void CmdAddTimedBuff(BuffIndex buffType, float duration)
	{
		AddTimedBuff(buffType, duration);
	}

	[Server]
	public void AddTimedBuff(BuffDef buffDef, float duration, int maxStacks)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::AddTimedBuff(RoR2.BuffDef,System.Single,System.Int32)' called on client");
		}
		else
		{
			if (ImmuneToDebuffBehavior.OverrideDebuff(buffDef, this))
			{
				return;
			}
			if (GetBuffCount(buffDef) < maxStacks)
			{
				AddTimedBuff(buffDef, duration);
				return;
			}
			int num = -1;
			float num2 = duration;
			for (int i = 0; i < timedBuffs.Count; i++)
			{
				if (timedBuffs[i].buffIndex == buffDef.buffIndex && timedBuffs[i].timer < num2)
				{
					num = i;
					num2 = timedBuffs[i].timer;
				}
			}
			if (num >= 0)
			{
				timedBuffs[num].timer = duration;
			}
		}
	}

	[Server]
	public void AddTimedBuff(BuffDef buffDef, float duration)
	{
		BuffIndex buffType;
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::AddTimedBuff(RoR2.BuffDef,System.Single)' called on client");
		}
		else
		{
			if ((object)buffDef == null || ImmuneToDebuffBehavior.OverrideDebuff(buffDef, this))
			{
				return;
			}
			buffType = buffDef.buffIndex;
			if (buffType == BuffIndex.None)
			{
				return;
			}
			if (buffDef == RoR2Content.Buffs.AttackSpeedOnCrit)
			{
				int num = (inventory ? inventory.GetItemCount(RoR2Content.Items.AttackSpeedOnCrit) : 0);
				int num2 = 1 + num * 2;
				int num3 = 0;
				int num4 = -1;
				float num5 = 999f;
				for (int i = 0; i < timedBuffs.Count; i++)
				{
					if (timedBuffs[i].buffIndex == buffType)
					{
						num3++;
						if (timedBuffs[i].timer < num5)
						{
							num4 = i;
							num5 = timedBuffs[i].timer;
						}
					}
				}
				if (num3 < num2)
				{
					timedBuffs.Add(new TimedBuff
					{
						buffIndex = buffType,
						timer = duration
					});
					AddBuff(buffType);
					ChildLocator component = modelLocator.modelTransform.GetComponent<ChildLocator>();
					if ((bool)component)
					{
						Transform obj = component.FindChild("HandL");
						Transform transform = component.FindChild("HandR");
						GameObject effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/WolfProcEffect");
						if ((bool)obj)
						{
							EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, "HandL", transmit: true);
						}
						if ((bool)transform)
						{
							EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, "HandR", transmit: true);
						}
					}
				}
				else if (num4 > -1)
				{
					timedBuffs[num4].timer = duration;
				}
				EntitySoundManager.EmitSoundServer(CommonAssets.procCritAttackSpeedSounds[Mathf.Min(CommonAssets.procCritAttackSpeedSounds.Length - 1, num3)].index, networkIdentity);
			}
			else if (buffDef == RoR2Content.Buffs.BeetleJuice)
			{
				if (RefreshStacks() < 10)
				{
					timedBuffs.Add(new TimedBuff
					{
						buffIndex = buffType,
						timer = duration
					});
					AddBuff(buffType);
				}
			}
			else if (buffDef == RoR2Content.Buffs.NullifyStack)
			{
				if (HasBuff(RoR2Content.Buffs.Nullified))
				{
					return;
				}
				int num6 = 0;
				for (int j = 0; j < timedBuffs.Count; j++)
				{
					if (timedBuffs[j].buffIndex == buffType)
					{
						num6++;
						if (timedBuffs[j].timer < duration)
						{
							timedBuffs[j].timer = duration;
						}
					}
				}
				if (num6 < 2)
				{
					timedBuffs.Add(new TimedBuff
					{
						buffIndex = buffType,
						timer = duration
					});
					AddBuff(buffType);
				}
				else
				{
					ClearTimedBuffs(RoR2Content.Buffs.NullifyStack.buffIndex);
					AddTimedBuff(RoR2Content.Buffs.Nullified.buffIndex, 3f);
				}
			}
			else if (buffDef == RoR2Content.Buffs.AffixHauntedRecipient)
			{
				if (!HasBuff(RoR2Content.Buffs.AffixHaunted))
				{
					DefaultBehavior();
				}
			}
			else if (buffDef == DLC2Content.Buffs.EliteBeadCorruption)
			{
				if (!HasBuff(DLC2Content.Buffs.EliteBead))
				{
					DefaultBehavior();
				}
			}
			else if (buffDef == RoR2Content.Buffs.LunarDetonationCharge)
			{
				RefreshStacks();
				DefaultBehavior();
			}
			else if (buffDef == RoR2Content.Buffs.Overheat)
			{
				RefreshStacks();
				DefaultBehavior();
			}
			else if (buffDef == DLC2Content.Buffs.BoostAllStatsBuff)
			{
				RefreshStacks();
				DefaultBehavior();
			}
			else
			{
				DefaultBehavior();
			}
		}
		void DefaultBehavior()
		{
			bool flag = false;
			if (!buffDef.canStack)
			{
				for (int l = 0; l < timedBuffs.Count; l++)
				{
					if (timedBuffs[l].buffIndex == buffType)
					{
						flag = true;
						timedBuffs[l].timer = Mathf.Max(timedBuffs[l].timer, duration);
						break;
					}
				}
			}
			if (!flag)
			{
				timedBuffs.Add(new TimedBuff
				{
					buffIndex = buffType,
					timer = duration
				});
				AddBuff(buffType);
			}
			if ((bool)buffDef.startSfx)
			{
				EntitySoundManager.EmitSoundServer(buffDef.startSfx.index, networkIdentity);
			}
		}
		int RefreshStacks()
		{
			int num7 = 0;
			for (int k = 0; k < timedBuffs.Count; k++)
			{
				TimedBuff timedBuff = timedBuffs[k];
				if (timedBuff.buffIndex == buffType)
				{
					num7++;
					if (timedBuff.timer < duration)
					{
						timedBuff.timer = duration;
					}
				}
			}
			return num7;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Server]
	public void AddTimedBuff(BuffIndex buffIndex, float duration)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::AddTimedBuff(RoR2.BuffIndex,System.Single)' called on client");
		}
		else
		{
			AddTimedBuff(BuffCatalog.GetBuffDef(buffIndex), duration);
		}
	}

	[Server]
	public void ClearTimedBuffs(BuffIndex buffType)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::ClearTimedBuffs(RoR2.BuffIndex)' called on client");
			return;
		}
		for (int num = timedBuffs.Count - 1; num >= 0; num--)
		{
			TimedBuff timedBuff = timedBuffs[num];
			if (timedBuff.buffIndex == buffType)
			{
				timedBuffs.RemoveAt(num);
				RemoveBuff(timedBuff.buffIndex);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Server]
	public void ClearTimedBuffs(BuffDef buffDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::ClearTimedBuffs(RoR2.BuffDef)' called on client");
		}
		else
		{
			ClearTimedBuffs(buffDef?.buffIndex ?? BuffIndex.None);
		}
	}

	[Server]
	public void RemoveOldestTimedBuff(BuffIndex buffType)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::RemoveOldestTimedBuff(RoR2.BuffIndex)' called on client");
			return;
		}
		float num = float.NegativeInfinity;
		int num2 = -1;
		for (int num3 = timedBuffs.Count - 1; num3 >= 0; num3--)
		{
			TimedBuff timedBuff = timedBuffs[num3];
			if (timedBuff.buffIndex == buffType && num < timedBuff.timer)
			{
				num = timedBuff.timer;
				num2 = num3;
			}
		}
		if (num2 > 0)
		{
			timedBuffs.RemoveAt(num2);
			RemoveBuff(buffType);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Server]
	public void RemoveOldestTimedBuff(BuffDef buffDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::RemoveOldestTimedBuff(RoR2.BuffDef)' called on client");
		}
		else
		{
			RemoveOldestTimedBuff(buffDef?.buffIndex ?? BuffIndex.None);
		}
	}

	[Server]
	private void UpdateBuffs(float deltaTime)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::UpdateBuffs(System.Single)' called on client");
			return;
		}
		for (int num = timedBuffs.Count - 1; num >= 0; num--)
		{
			TimedBuff timedBuff = timedBuffs[num];
			timedBuff.timer -= deltaTime;
			if (timedBuff.timer <= 0f)
			{
				timedBuffs.RemoveAt(num);
				RemoveBuff(timedBuff.buffIndex);
			}
		}
	}

	[Client]
	private void OnClientBuffsChanged()
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.CharacterBody::OnClientBuffsChanged()' called on server");
			return;
		}
		bool num = HasBuff(RoR2Content.Buffs.WarCryBuff);
		if (!num && (bool)warCryEffectInstance)
		{
			UnityEngine.Object.Destroy(warCryEffectInstance);
		}
		if (num && !warCryEffectInstance)
		{
			Transform transform = (mainHurtBox ? mainHurtBox.transform : this.transform);
			if ((bool)transform)
			{
				warCryEffectInstance = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/WarCryEffect"), transform.position, Quaternion.identity, transform);
			}
		}
		if ((bool)inventory && inventory.GetItemCount(DLC2Content.Items.IncreaseDamageOnMultiKill) > 0)
		{
			int buffCount = GetBuffCount(DLC2Content.Buffs.IncreaseDamageBuff);
			if ((bool)increaseDamageOnMultiKillItemDisplayUpdater)
			{
				increaseDamageOnMultiKillItemDisplayUpdater.UpdateKillCounterText(buffCount);
			}
			if (previousMultiKillBuffCount < buffCount)
			{
				Util.PlaySound("Play_item_proc_increaseDamageMultiKill", base.gameObject);
			}
			previousMultiKillBuffCount = buffCount;
		}
	}

	public NetworkInstanceId GetMasterObjectId()
	{
		return masterObjectId;
	}

	private void GetSelectedCharacterType()
	{
	}

	private void UpdateHurtBoxesEnabled()
	{
		bool flag = ((bool)inventory && inventory.GetItemCount(RoR2Content.Items.Ghost) > 0) || HasBuff(RoR2Content.Buffs.Intangible);
		if (flag == disablingHurtBoxes)
		{
			return;
		}
		if ((bool)hurtBoxGroup)
		{
			if (flag)
			{
				HurtBoxGroup obj = hurtBoxGroup;
				int hurtBoxesDeactivatorCounter = obj.hurtBoxesDeactivatorCounter + 1;
				obj.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
			}
			else
			{
				HurtBoxGroup obj2 = hurtBoxGroup;
				int hurtBoxesDeactivatorCounter = obj2.hurtBoxesDeactivatorCounter - 1;
				obj2.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
			}
		}
		disablingHurtBoxes = flag;
	}

	private void OnInventoryChanged()
	{
		EquipmentIndex currentEquipmentIndex = inventory.currentEquipmentIndex;
		if (currentEquipmentIndex != previousEquipmentIndex)
		{
			EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(previousEquipmentIndex);
			EquipmentDef equipmentDef2 = EquipmentCatalog.GetEquipmentDef(currentEquipmentIndex);
			if (equipmentDef != null)
			{
				OnEquipmentLost(equipmentDef);
			}
			if (equipmentDef2 != null)
			{
				OnEquipmentGained(equipmentDef2);
			}
			previousEquipmentIndex = currentEquipmentIndex;
		}
		statsDirty = true;
		UpdateHurtBoxesEnabled();
		AddItemBehavior<AffixHauntedBehavior>(HasBuff(RoR2Content.Buffs.AffixHaunted) ? 1 : 0);
		AddItemBehavior<AffixEarthBehavior>(HasBuff(DLC1Content.Buffs.EliteEarth) ? 1 : 0);
		AddItemBehavior<AffixVoidBehavior>(HasBuff(DLC1Content.Buffs.EliteVoid) ? 1 : 0);
		AddItemBehavior<AffixBeadBehavior>(HasBuff(DLC2Content.Buffs.EliteBead) ? 1 : 0);
		AddItemBehavior<AffixAurelioniteBehavior>(HasBuff(DLC2Content.Buffs.EliteAurelionite) ? 1 : 0);
		if (NetworkServer.active)
		{
			AddItemBehavior<QuestVolatileBatteryBehaviorServer>((inventory.GetEquipment(inventory.activeEquipmentSlot).equipmentDef == RoR2Content.Equipment.QuestVolatileBattery) ? 1 : 0);
			AddItemBehavior<ElementalRingsBehavior>(inventory.GetItemCount(RoR2Content.Items.IceRing) + inventory.GetItemCount(RoR2Content.Items.FireRing));
			AddItemBehavior<ElementalRingVoidBehavior>(inventory.GetItemCount(DLC1Content.Items.ElementalRingVoid));
			AddItemBehavior<OutOfCombatArmorBehavior>(inventory.GetItemCount(DLC1Content.Items.OutOfCombatArmor));
			AddItemBehavior<PrimarySkillShurikenBehavior>(inventory.GetItemCount(DLC1Content.Items.PrimarySkillShuriken));
			AddItemBehavior<MushroomVoidBehavior>(inventory.GetItemCount(DLC1Content.Items.MushroomVoid));
			AddItemBehavior<BearVoidBehavior>(inventory.GetItemCount(DLC1Content.Items.BearVoid));
			AddItemBehavior<LunarSunBehavior>(inventory.GetItemCount(DLC1Content.Items.LunarSun));
			AddItemBehavior<VoidMegaCrabItemBehavior>(inventory.GetItemCount(DLC1Content.Items.VoidMegaCrabItem));
			AddItemBehavior<DroneWeaponsBehavior>(inventory.GetItemCount(DLC1Content.Items.DroneWeapons));
			AddItemBehavior<DroneWeaponsBoostBehavior>(inventory.GetItemCount(DLC1Content.Items.DroneWeaponsBoost));
			AddItemBehavior<MeteorAttackOnHighDamageBehavior>(inventory.GetItemCount(DLC2Content.Items.MeteorAttackOnHighDamage));
			AddItemBehavior<TeleportOnLowHealthBehavior>(inventory.GetItemCount(DLC2Content.Items.TeleportOnLowHealth));
		}
		executeEliteHealthFraction = Util.ConvertAmplificationPercentageIntoReductionPercentage(13f * (float)inventory.GetItemCount(RoR2Content.Items.ExecuteLowHealthElite)) / 100f;
		if ((bool)skillLocator)
		{
			ReplaceSkillIfItemPresent(skillLocator.primary, RoR2Content.Items.LunarPrimaryReplacement.itemIndex, CommonAssets.lunarPrimaryReplacementSkillDef);
			ReplaceSkillIfItemPresent(skillLocator.secondary, RoR2Content.Items.LunarSecondaryReplacement.itemIndex, CommonAssets.lunarSecondaryReplacementSkillDef);
			ReplaceSkillIfItemPresent(skillLocator.special, RoR2Content.Items.LunarSpecialReplacement.itemIndex, CommonAssets.lunarSpecialReplacementSkillDef);
			ReplaceSkillIfItemPresent(skillLocator.utility, RoR2Content.Items.LunarUtilityReplacement.itemIndex, CommonAssets.lunarUtilityReplacementSkillDef);
		}
		this.onInventoryChanged?.Invoke();
		CharacterBody.onBodyInventoryChangedGlobal?.Invoke(this);
		UpdateItemAvailability();
	}

	private void ReplaceSkillIfItemPresent(GenericSkill skill, ItemIndex itemIndex, SkillDef skillDef)
	{
		if ((bool)skill)
		{
			if (inventory.GetItemCount(itemIndex) > 0 && (bool)skillDef)
			{
				skill.SetSkillOverride(this, skillDef, GenericSkill.SkillOverridePriority.Replacement);
			}
			else
			{
				skill.UnsetSkillOverride(this, skillDef, GenericSkill.SkillOverridePriority.Replacement);
			}
		}
	}

	private void OnEquipmentLost(EquipmentDef equipmentDef)
	{
		if (NetworkServer.active && (object)equipmentDef.passiveBuffDef != null)
		{
			RemoveBuff(equipmentDef.passiveBuffDef);
		}
	}

	private void OnEquipmentGained(EquipmentDef equipmentDef)
	{
		if (NetworkServer.active && (object)equipmentDef.passiveBuffDef != null)
		{
			AddBuff(equipmentDef.passiveBuffDef);
		}
	}

	private void UpdateMasterLink()
	{
		if (!linkedToMaster && (bool)master && (bool)master)
		{
			master.OnBodyStart(this);
			linkedToMaster = true;
			skinIndex = master.loadout.bodyLoadoutManager.GetSkinIndex(bodyIndex);
		}
	}

	public static void Init()
	{
		AssetReferences.Resolve();
	}

	private void Awake()
	{
		transform = base.transform;
		rigidbody = GetComponent<Rigidbody>();
		networkIdentity = GetComponent<NetworkIdentity>();
		teamComponent = GetComponent<TeamComponent>();
		healthComponent = GetComponent<HealthComponent>();
		equipmentSlot = GetComponent<EquipmentSlot>();
		skillLocator = GetComponent<SkillLocator>();
		modelLocator = GetComponent<ModelLocator>();
		characterMotor = GetComponent<CharacterMotor>();
		characterDirection = GetComponent<CharacterDirection>();
		inputBank = GetComponent<InputBankTest>();
		sfxLocator = GetComponent<SfxLocator>();
		activeBuffsList = BuffCatalog.GetPerBuffBuffer<BuffIndex>();
		buffs = BuffCatalog.GetPerBuffBuffer<int>();
		if ((bool)modelLocator)
		{
			modelLocator.onModelChanged += OnModelChanged;
			OnModelChanged(modelLocator.modelTransform);
		}
		radius = 1f;
		CapsuleCollider component = GetComponent<CapsuleCollider>();
		if ((bool)component)
		{
			radius = component.radius;
		}
		else
		{
			SphereCollider component2 = GetComponent<SphereCollider>();
			if ((bool)component2)
			{
				radius = component2.radius;
			}
		}
		try
		{
			CharacterBody.onBodyAwakeGlobal?.Invoke(this);
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
	}

	private void OnModelChanged(Transform modelTransform)
	{
		hurtBoxGroup = null;
		mainHurtBox = null;
		coreTransform = transform;
		if ((bool)modelTransform)
		{
			hurtBoxGroup = modelTransform.GetComponent<HurtBoxGroup>();
			if ((bool)hurtBoxGroup)
			{
				mainHurtBox = hurtBoxGroup.mainHurtBox;
				if ((bool)mainHurtBox)
				{
					coreTransform = mainHurtBox.transform;
				}
			}
		}
		if ((bool)overrideCoreTransform)
		{
			coreTransform = overrideCoreTransform;
		}
	}

	private void Start()
	{
		UpdateAuthority();
		localStartTime = Run.FixedTimeStamp.now;
		bool num = (bodyFlags & BodyFlags.Masterless) != 0;
		outOfCombatStopwatch = float.PositiveInfinity;
		outOfDangerStopwatch = float.PositiveInfinity;
		notMovingStopwatch = 0f;
		if (NetworkServer.active)
		{
			outOfCombat = true;
			outOfDanger = true;
		}
		RecalculateStats();
		UpdateMasterLink();
		if (num)
		{
			healthComponent.Networkhealth = maxHealth;
		}
		if ((bool)sfxLocator && healthComponent.alive)
		{
			Util.PlaySound(sfxLocator.aliveLoopStart, base.gameObject);
		}
		if (!doNotReassignToTeamBasedCollisionLayer)
		{
			base.gameObject.layer = LayerIndex.GetAppropriateLayerForTeam(teamComponent.teamIndex);
			if (characterMotor != null)
			{
				characterMotor.Motor.RebuildCollidableLayers();
			}
		}
		CharacterBody.onBodyStartGlobal?.Invoke(this);
		if ((bool)master && NetworkServer.active)
		{
			if (master.devotionInventoryPrefab == null)
			{
				_ = master.isDevotedMinion;
			}
			if (isPlayerControlled && (bool)master.devotionInventoryPrefab)
			{
				_ = master.devotionInventoryPrefab != null;
			}
			int num2 = ((inventory != null) ? inventory.GetItemCount(DLC2Content.Items.GoldOnStageStart) : 0);
			if (num2 > 0)
			{
				TransmitItemBehavior(new NetworkItemBehaviorData(DLC2Content.Items.GoldOnStageStart.itemIndex, 0f));
				uint difficultyScaledCost = (uint)Run.instance.GetDifficultyScaledCost((int)(50f + level * 1.5f * (float)num2));
				master.GiveMoney(difficultyScaledCost);
			}
		}
	}

	public void Update()
	{
		float deltaTime = Time.deltaTime;
		outOfCombatStopwatch += deltaTime;
		outOfDangerStopwatch += deltaTime;
		aimTimer = Mathf.Max(aimTimer - deltaTime, 0f);
		UpdateOutOfCombatAndDanger();
		UpdateSpreadBloom(deltaTime);
		DoItemUpdates(deltaTime);
		UpdateNotMoving(deltaTime);
		HandleLavaDamage(deltaTime);
	}

	private void UpdateNotMoving(float deltaTime)
	{
		if (NetworkServer.active)
		{
			Vector3 position = transform.position;
			float num = 0.1f * deltaTime;
			if ((position - previousPosition).sqrMagnitude <= num * num)
			{
				notMovingStopwatch += deltaTime;
			}
			else
			{
				notMovingStopwatch = 0f;
			}
			previousPosition = position;
		}
	}

	private void DoItemUpdates(float deltaTime)
	{
		if ((bodyFlags & BodyFlags.IgnoreItemUpdates) == 0)
		{
			if (NetworkServer.active)
			{
				UpdateMultiKill(deltaTime);
				UpdateHelfire(deltaTime);
				UpdateAffixPoison(deltaTime);
				UpdateAffixLunar(deltaTime);
				UpdateLowerHealthHigherDamage();
				UpdateDelayedDamage(deltaTime);
				UpdateSecondHalfOfDamage(deltaTime);
			}
			UpdateFireTrail();
		}
	}

	private void UpdateOutOfCombatAndDanger()
	{
		bool active = NetworkServer.active;
		bool flag = outOfCombat;
		bool flag2 = flag;
		if (active || hasEffectiveAuthority)
		{
			flag2 = outOfCombatStopwatch >= 5f;
			if (outOfCombat != flag2)
			{
				if (NetworkServer.active)
				{
					SetDirtyBit(4u);
				}
				outOfCombat = flag2;
				statsDirty = true;
			}
		}
		if (active)
		{
			bool flag3 = outOfDangerStopwatch >= 7f;
			bool flag4 = outOfDanger;
			bool flag5 = flag && flag4;
			bool num = flag2 && flag3;
			if (outOfDanger != flag3)
			{
				SetDirtyBit(8u);
				outOfDanger = flag3;
				statsDirty = true;
			}
			if (num && !flag5)
			{
				OnOutOfCombatAndDangerServer();
			}
		}
	}

	[Command]
	public void CmdSetInLava(bool b)
	{
		inLava = b;
	}

	public void SetInLava(bool b)
	{
		if (inLava != b)
		{
			if (!NetworkServer.active && base.hasAuthority)
			{
				CallCmdSetInLava(b);
			}
			inLava = b;
		}
	}

	public void InflictLavaDamage()
	{
		DamageInfo damageInfo = new DamageInfo();
		damageInfo.damage = 10f;
		if (isChampion || isBoss)
		{
			damageInfo.damage = healthComponent.fullCombinedHealth / 100f;
		}
		else if (teamComponent.teamIndex == TeamIndex.Monster || teamComponent.teamIndex == TeamIndex.Void)
		{
			damageInfo.damage = healthComponent.fullCombinedHealth / 50f;
		}
		else
		{
			damageInfo.damage = healthComponent.fullCombinedHealth / 10f;
		}
		damageInfo.damageType = DamageType.IgniteOnHit;
		damageInfo.position = footPosition;
		healthComponent.TakeDamage(damageInfo);
	}

	public void HandleLavaDamage(float deltaTime)
	{
		bool flag = (bodyFlags & BodyFlags.ImmuneToLava) != 0;
		if (inLava && NetworkServer.active)
		{
			lavaTimer -= deltaTime;
			if (lavaTimer <= 0f && !flag)
			{
				InflictLavaDamage();
				lavaTimer = lavaCooldown;
			}
		}
		else
		{
			lavaTimer = 0f;
		}
	}

	private void UpdateItemAvailability()
	{
		if ((bool)inventory)
		{
			itemAvailability.helfireCount = inventory.GetItemCount(JunkContent.Items.BurnNearby);
		}
		itemAvailability.hasFireTrail = HasBuff(RoR2Content.Buffs.AffixRed);
		itemAvailability.hasAffixLunar = HasBuff(RoR2Content.Buffs.AffixLunar);
		itemAvailability.hasAffixPoison = HasBuff(RoR2Content.Buffs.AffixPoison);
	}

	public void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		bool flag = (bodyFlags & BodyFlags.IgnoreItemUpdates) == 0;
		if (NetworkServer.active)
		{
			if (flag)
			{
				UpdateBuffs(fixedDeltaTime);
			}
			UpdateOutOfCombatAndDanger();
		}
		if (statsDirty)
		{
			RecalculateStats();
		}
	}

	public void OnDeathStart()
	{
		base.enabled = false;
		if ((bool)sfxLocator)
		{
			Util.PlaySound(sfxLocator.aliveLoopStop, base.gameObject);
		}
		if (NetworkServer.active && (bool)currentVehicle)
		{
			currentVehicle.EjectPassenger(base.gameObject);
		}
		if ((bool)master)
		{
			master.OnBodyDeath(this);
		}
		ModelLocator component = GetComponent<ModelLocator>();
		if (!component)
		{
			return;
		}
		Transform modelTransform = component.modelTransform;
		if ((bool)modelTransform)
		{
			CharacterModel component2 = modelTransform.GetComponent<CharacterModel>();
			if ((bool)component2)
			{
				component2.OnDeath();
			}
		}
	}

	public void OnTakeDamageServer(DamageReport damageReport)
	{
		if (damageReport.damageDealt > 0f)
		{
			outOfDangerStopwatch = 0f;
		}
		if ((bool)master)
		{
			master.OnBodyDamaged(damageReport);
		}
	}

	public void OnSkillActivated(GenericSkill skill)
	{
		if (skill.isCombatSkill)
		{
			outOfCombatStopwatch = 0f;
		}
		if (hasEffectiveAuthority)
		{
			this.onSkillActivatedAuthority?.Invoke(skill);
		}
		if (NetworkServer.active)
		{
			this.onSkillActivatedServer?.Invoke(skill);
			if (inventory.GetItemCount(DLC2Content.Items.IncreasePrimaryDamage) <= 0)
			{
				return;
			}
			if (bodyIndex == BodyCatalog.SpecialCases.RailGunner())
			{
				if ((object)skillLocator.primary == skill)
				{
					if (GetBuffCount(DLC2Content.Buffs.IncreasePrimaryDamageBuff) > 0 && !BaseScopeState.inScope)
					{
						luminousShotReady = true;
					}
				}
				else
				{
					luminousShotReady = false;
				}
				if ((object)skillLocator.primary == skill && BaseScopeState.inScope)
				{
					AddIncreasePrimaryDamageStack();
				}
			}
			else
			{
				if (!skill.skillDef.autoHandleLuminousShot)
				{
					return;
				}
				if ((object)skillLocator.secondary == skill)
				{
					AddIncreasePrimaryDamageStack();
				}
				else if ((object)skillLocator.primary == skill)
				{
					if (GetBuffCount(DLC2Content.Buffs.IncreasePrimaryDamageBuff) > 0)
					{
						luminousShotReady = true;
					}
					else
					{
						luminousShotReady = false;
					}
				}
			}
		}
		else
		{
			CallCmdOnSkillActivated((sbyte)skillLocator.FindSkillSlot(skill));
		}
	}

	public void OnDestroy()
	{
		try
		{
			CharacterBody.onBodyDestroyGlobal?.Invoke(this);
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
		if ((bool)sfxLocator)
		{
			Util.PlaySound(sfxLocator.aliveLoopStop, base.gameObject);
		}
		if ((object)modelLocator != null)
		{
			modelLocator.onModelChanged -= OnModelChanged;
		}
		if ((bool)inventory)
		{
			inventory.onInventoryChanged -= OnInventoryChanged;
		}
		if ((bool)master)
		{
			master.OnBodyDestroyed(this);
		}
	}

	public float GetNormalizedThreatValue()
	{
		if ((bool)Run.instance)
		{
			return (master ? ((float)master.money) : 0f) * Run.instance.oneOverCompensatedDifficultyCoefficientSquared;
		}
		return 0f;
	}

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	private void OnValidate()
	{
		if (autoCalculateLevelStats)
		{
			PerformAutoCalculateLevelStats();
		}
		if (!Application.isPlaying && bodyIndex != BodyIndex.None)
		{
			bodyIndex = BodyIndex.None;
		}
	}

	private void UpdateAuthority()
	{
		hasEffectiveAuthority = Util.HasEffectiveAuthority(base.gameObject);
	}

	public override void OnStartAuthority()
	{
		UpdateAuthority();
	}

	public override void OnStopAuthority()
	{
		UpdateAuthority();
	}

	private void OnSprintStart()
	{
		if ((bool)sfxLocator)
		{
			Util.PlaySound(sfxLocator.sprintLoopStart, base.gameObject);
		}
	}

	private void OnSprintStop()
	{
		if ((bool)sfxLocator)
		{
			Util.PlaySound(sfxLocator.sprintLoopStop, base.gameObject);
		}
	}

	[Command]
	private void CmdUpdateSprint(bool newIsSprinting)
	{
		isSprinting = newIsSprinting;
	}

	[Command]
	private void CmdOnSkillActivated(sbyte skillIndex)
	{
		OnSkillActivated(skillLocator.GetSkill((SkillSlot)skillIndex));
	}

	private void OnOutOfDangerChanged()
	{
		if (outOfDanger && healthComponent.shield != healthComponent.fullShield)
		{
			Util.PlaySound("Play_item_proc_personal_shield_recharge", base.gameObject);
		}
	}

	[Server]
	private void OnOutOfCombatAndDangerServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::OnOutOfCombatAndDangerServer()' called on client");
		}
	}

	[Server]
	public bool GetNotMoving()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.CharacterBody::GetNotMoving()' called on client");
			return false;
		}
		return notMovingStopwatch >= 1f;
	}

	public void PerformAutoCalculateLevelStats()
	{
		levelMaxHealth = Mathf.Round(baseMaxHealth * 0.3f);
		levelMaxShield = Mathf.Round(baseMaxShield * 0.3f);
		levelRegen = baseRegen * 0.2f;
		levelMoveSpeed = 0f;
		levelJumpPower = 0f;
		levelDamage = baseDamage * 0.2f;
		levelAttackSpeed = 0f;
		levelCrit = 0f;
		levelArmor = 0f;
	}

	public void MarkAllStatsDirty()
	{
		statsDirty = true;
	}

	private void UpdateLowerHealthHigherDamageVFX()
	{
		if ((bool)lowerHealthHigherDamageSteam)
		{
			ParticleSystem.EmissionModule emission = lowerHealthHigherDamageSteam.GetComponent<ParticleSystem>().emission;
			emission.rateOverTime = 35f;
		}
	}

	public void UpdateLowerHealthHigherDamage()
	{
		if (!inventory)
		{
			return;
		}
		int itemCount = inventory.GetItemCount(DLC2Content.Items.LowerHealthHigherDamage);
		if (NetworkServer.active)
		{
			if (itemCount > 0)
			{
				if ((bool)inventory && healthComponent.health > 0f && teamComponent.teamIndex == TeamIndex.Player)
				{
					float normalizedHealth = healthComponent.GetNormalizedHealth();
					if ((double)normalizedHealth <= 0.5 && !HasBuff(DLC2Content.Buffs.LowerHealthHigherDamageBuff))
					{
						AddBuff(DLC2Content.Buffs.LowerHealthHigherDamageBuff);
						Util.PlaySound("Play_item_proc_lowerHealthHigherDamage_proc", base.gameObject);
						TransmitItemBehavior(new NetworkItemBehaviorData(DLC2Content.Items.LowerHealthHigherDamage.itemIndex, 1f));
					}
					else if (HasBuff(DLC2Content.Buffs.LowerHealthHigherDamageBuff) && (double)normalizedHealth > 0.5)
					{
						RemoveBuff(DLC2Content.Buffs.LowerHealthHigherDamageBuff);
						TransmitItemBehavior(new NetworkItemBehaviorData(DLC2Content.Items.LowerHealthHigherDamage.itemIndex, 0f));
					}
				}
			}
			else if (HasBuff(DLC2Content.Buffs.LowerHealthHigherDamageBuff))
			{
				RemoveBuff(DLC2Content.Buffs.LowerHealthHigherDamageBuff);
				TransmitItemBehavior(new NetworkItemBehaviorData(DLC2Content.Items.LowerHealthHigherDamage.itemIndex, 0f));
			}
		}
		else if (itemCount == 0 && HasBuff(DLC2Content.Buffs.LowerHealthHigherDamageBuff) && healthComponent.health > 0f && teamComponent.teamIndex == TeamIndex.Player && (double)healthComponent.GetNormalizedHealth() > 0.5)
		{
			RemoveBuff(DLC2Content.Buffs.LowerHealthHigherDamageBuff);
			TransmitItemBehavior(new NetworkItemBehaviorData(DLC2Content.Items.LowerHealthHigherDamage.itemIndex, 0f));
		}
	}

	private void SetBoostAllStatsStartTimer(float t)
	{
		if (!boostAllStatsTimerStarted)
		{
			boostAllStatsStartTimer = t;
			boostAllStatsTimer = boostAllStatsStartTimer;
			boostAllStatsTimerStarted = true;
		}
	}

	private void SetBoostAllStatsCoolDownTimer(float t)
	{
		if (boostAllStatsCoolDownTimerStarted)
		{
			boostAllStatsCoolDownStartTimer = t;
			boostAllStatsCoolDownTimer = boostAllStatsCoolDownStartTimer;
		}
	}

	private float CalculateBoostAllStatsTriggerChance(int i)
	{
		boostAllStatsTriggerChance = boostAllStatsDefaultTriggerChance + boostAllStatsTriggerIncrease * (float)(i - 1);
		if ((double)boostAllStatsTriggerChance >= 0.5)
		{
			boostAllStatsTriggerChance = 0.5f;
		}
		return boostAllStatsTriggerChance;
	}

	private void UpdateBoostAllStatsTimer(float t)
	{
		if (!NetworkServer.active || !inventory)
		{
			return;
		}
		int itemCount = inventory.GetItemCount(DLC2Content.Items.BoostAllStats);
		if (!boostAllStatsCoolDownTimerStarted && itemCount > 0)
		{
			SetBoostAllStatsStartTimer(t);
			boostAllStatsTimer += t;
			if (boostAllStatsTimer >= boostAllStatsStartTimer + boostAllStatsTriggerTimer)
			{
				if (RoR2Application.rng.RangeFloat(0f, 1f) <= CalculateBoostAllStatsTriggerChance(inventory.GetItemCount(DLC2Content.Items.BoostAllStats)))
				{
					AddBuff(DLC2Content.Buffs.BoostAllStatsBuff);
					RecalculateStats();
					boostAllStatsTimerStarted = false;
					boostAllStatsCoolDownTimerStarted = true;
					SetBoostAllStatsCoolDownTimer(t);
				}
				else
				{
					boostAllStatsTimerStarted = false;
					boostAllStatsCoolDownTimerStarted = false;
				}
			}
		}
		else if (boostAllStatsCoolDownTimerStarted)
		{
			boostAllStatsCoolDownTimer += t;
			if (boostAllStatsCoolDownTimer >= boostAllStatsStartTimer + boostAllStatsCoolDownTriggerTimer)
			{
				RemoveBuff(DLC2Content.Buffs.BoostAllStatsBuff);
				RecalculateStats();
				boostAllStatsCoolDownTimerStarted = false;
			}
		}
	}

	public void RecalculateStats()
	{
		if (!Run.instance)
		{
			return;
		}
		float num = level;
		TeamManager.instance.GetTeamExperience(teamComponent.teamIndex);
		float num2 = TeamManager.instance.GetTeamLevel(teamComponent.teamIndex);
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		int num10 = 0;
		int num11 = 0;
		int num12 = 0;
		int num13 = 0;
		int num14 = 0;
		int num15 = 0;
		int num16 = 0;
		int num17 = 0;
		int num18 = 0;
		int num19 = 0;
		int num20 = 0;
		int num21 = 0;
		int num22 = 0;
		int num23 = 0;
		int num24 = 0;
		int num25 = 0;
		int num26 = 0;
		int num27 = 0;
		int num28 = 0;
		int num29 = 0;
		int num30 = 0;
		int num31 = 0;
		int num32 = 0;
		int num33 = 0;
		int num34 = 0;
		int num35 = 0;
		int num36 = 0;
		int num37 = 0;
		int num38 = 0;
		int num39 = 0;
		int num40 = 0;
		int num41 = 0;
		int num42 = 0;
		int num43 = 0;
		int num44 = 0;
		int num45 = 0;
		int num46 = 0;
		int num47 = 0;
		int num48 = 0;
		int num49 = 0;
		int num50 = 0;
		EquipmentIndex equipmentIndex = EquipmentIndex.None;
		uint num51 = 0u;
		int num52 = 0;
		int num53 = 0;
		int num54 = 0;
		int num55 = 0;
		if ((bool)inventory)
		{
			num3 = inventory.GetItemCount(RoR2Content.Items.LevelBonus);
			num4 = inventory.GetItemCount(RoR2Content.Items.Infusion);
			num5 = inventory.GetItemCount(RoR2Content.Items.HealWhileSafe);
			num6 = inventory.GetItemCount(RoR2Content.Items.PersonalShield);
			num7 = inventory.GetItemCount(RoR2Content.Items.Hoof);
			num8 = inventory.GetItemCount(RoR2Content.Items.SprintOutOfCombat);
			num9 = inventory.GetItemCount(RoR2Content.Items.Feather);
			num10 = inventory.GetItemCount(RoR2Content.Items.Syringe);
			num11 = inventory.GetItemCount(RoR2Content.Items.CritGlasses);
			num12 = inventory.GetItemCount(RoR2Content.Items.AttackSpeedOnCrit);
			num13 = inventory.GetItemCount(JunkContent.Items.CooldownOnCrit);
			num14 = inventory.GetItemCount(RoR2Content.Items.HealOnCrit);
			num15 = inventory.GetItemCount(RoR2Content.Items.ShieldOnly);
			num16 = inventory.GetItemCount(RoR2Content.Items.AlienHead);
			num17 = inventory.GetItemCount(RoR2Content.Items.Knurl);
			num18 = inventory.GetItemCount(RoR2Content.Items.BoostHp);
			num19 = inventory.GetItemCount(JunkContent.Items.CritHeal);
			num20 = inventory.GetItemCount(RoR2Content.Items.SprintBonus);
			num21 = inventory.GetItemCount(RoR2Content.Items.SecondarySkillMagazine);
			num23 = inventory.GetItemCount(RoR2Content.Items.SprintArmor);
			num24 = inventory.GetItemCount(RoR2Content.Items.UtilitySkillMagazine);
			num25 = inventory.GetItemCount(RoR2Content.Items.HealthDecay);
			num27 = inventory.GetItemCount(RoR2Content.Items.TonicAffliction);
			num28 = inventory.GetItemCount(RoR2Content.Items.LunarDagger);
			num26 = inventory.GetItemCount(RoR2Content.Items.DrizzlePlayerHelper);
			num29 = inventory.GetItemCount(RoR2Content.Items.MonsoonPlayerHelper);
			num30 = inventory.GetItemCount(RoR2Content.Items.Pearl);
			num31 = inventory.GetItemCount(RoR2Content.Items.ShinyPearl);
			num32 = inventory.GetItemCount(RoR2Content.Items.InvadingDoppelganger);
			num33 = inventory.GetItemCount(RoR2Content.Items.CutHp);
			num34 = inventory.GetItemCount(RoR2Content.Items.BoostAttackSpeed);
			num35 = inventory.GetItemCount(RoR2Content.Items.BleedOnHitAndExplode);
			num36 = inventory.GetItemCount(RoR2Content.Items.LunarBadLuck);
			num37 = inventory.GetItemCount(RoR2Content.Items.FlatHealth);
			num38 = inventory.GetItemCount(RoR2Content.Items.TeamSizeDamageBonus);
			num39 = inventory.GetItemCount(RoR2Content.Items.SummonedEcho);
			num40 = inventory.GetItemCount(RoR2Content.Items.UseAmbientLevel);
			num22 = inventory.GetItemCount(DLC1Content.Items.EquipmentMagazineVoid);
			num44 = inventory.GetItemCount(DLC1Content.Items.HalfAttackSpeedHalfCooldowns);
			num45 = inventory.GetItemCount(DLC1Content.Items.HalfSpeedDoubleHealth);
			num41 = inventory.GetItemCount(RoR2Content.Items.BleedOnHit);
			num42 = inventory.GetItemCount(DLC1Content.Items.AttackSpeedAndMoveSpeed);
			num43 = inventory.GetItemCount(DLC1Content.Items.CritDamage);
			num46 = inventory.GetItemCount(DLC1Content.Items.ConvertCritChanceToCritDamage);
			num47 = inventory.GetItemCount(DLC1Content.Items.DroneWeaponsBoost);
			num48 = inventory.GetItemCount(DLC1Content.Items.MissileVoid);
			equipmentIndex = inventory.currentEquipmentIndex;
			num51 = inventory.infusionBonus;
			num49 = ((equipmentIndex == DLC1Content.Equipment.EliteVoidEquipment.equipmentIndex) ? 1 : 0);
			num50 = inventory.GetItemCount(DLC1Content.Items.OutOfCombatArmor);
			inventory.GetItemCount(DLC1Content.Items.VoidmanPassiveItem);
			num52 = inventory.GetItemCount(DLC2Content.Items.LowerHealthHigherDamage);
			num53 = inventory.GetItemCount(DLC2Content.Items.BoostAllStats);
			inventory.GetItemCount(DLC2Content.Items.TeleportOnLowHealth);
			num54 = inventory.GetItemCount(DLC2Content.Items.IncreaseDamageOnMultiKill);
			inventory.GetItemCount(DLC2Content.Items.GoldOnStageStart);
			num55 = inventory.GetItemCount(DLC2Content.Items.ExtraStatsOnLevelUp);
		}
		level = num2;
		if (num40 > 0)
		{
			level = Math.Max(level, Run.instance.ambientLevelFloor);
		}
		level += num3;
		EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
		float num56 = level - 1f;
		isElite = eliteBuffCount > 0;
		bool flag = HasBuff(RoR2Content.Buffs.TonicBuff);
		bool num57 = HasBuff(RoR2Content.Buffs.Entangle);
		bool flag2 = HasBuff(RoR2Content.Buffs.Nullified);
		bool flag3 = HasBuff(RoR2Content.Buffs.LunarSecondaryRoot);
		bool flag4 = teamComponent.teamIndex == TeamIndex.Player && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.glassArtifactDef);
		bool num58 = num15 > 0 || HasBuff(RoR2Content.Buffs.AffixLunar);
		bool flag5 = (object)equipmentDef != null && (object)equipmentDef == JunkContent.Equipment.EliteYellowEquipment;
		hasOneShotProtection = isPlayerControlled;
		int buffCount = GetBuffCount(RoR2Content.Buffs.BeetleJuice);
		int buffCount2 = GetBuffCount(DLC2Content.Buffs.RevitalizeBuff);
		isGlass = flag4 || num28 > 0;
		canPerformBackstab = (bodyFlags & BodyFlags.HasBackstabPassive) == BodyFlags.HasBackstabPassive;
		canReceiveBackstab = (bodyFlags & BodyFlags.HasBackstabImmunity) != BodyFlags.HasBackstabImmunity;
		float num59 = maxHealth;
		float num60 = maxShield;
		if (num55 > extraStatsOnLevelUpCountModifier && num55 > 0)
		{
			extraStatsOnLevelUpCountModifier = num55;
		}
		if (num55 < extraStatsOnLevelUpCountModifier && (bool)inventory)
		{
			UnityEngine.Object.Instantiate(CommonAssets.prayerBeadEffect, base.gameObject.transform.position, Quaternion.identity).transform.parent = base.gameObject.transform;
			extraStatsOnLevelUpCountModifier -= num55;
			float beadAppliedHealth = inventory.beadAppliedHealth;
			prayerBeadCalculateAppliedStats(levelMaxHealth);
			prayerBeadCalculateAppliedStats(levelMaxShield);
			prayerBeadCalculateAppliedStats(levelRegen);
			prayerBeadCalculateAppliedStats(levelDamage);
			extraStatsOnLevelUpCountModifier = num55;
			if (HasBuff(DLC2Content.Buffs.ExtraStatsOnLevelUpBuff))
			{
				SetBuffCount(DLC2Content.Buffs.ExtraStatsOnLevelUpBuff.buffIndex, 0);
				bool gainedStats = inventory.beadAppliedHealth > beadAppliedHealth;
				master.OnBeadReset(gainedStats);
			}
		}
		HandleDisableAllSkillsDebuff();
		float num61 = baseMaxHealth + levelMaxHealth * num56;
		if (teamComponent.teamIndex == TeamIndex.Player && (bool)inventory && inventory.beadAppliedHealth > 0f)
		{
			num61 += inventory.beadAppliedHealth;
		}
		float num62 = 1f;
		num62 += (float)num18 * 0.1f;
		num62 += (float)(num30 + num31) * 0.1f;
		num62 += (float)buffCount2 * 0.07f;
		num62 += (float)num49 * 0.5f;
		num62 += (float)num45 * 1f;
		if (num4 > 0)
		{
			num61 += (float)num51;
		}
		num61 += (float)num37 * 25f;
		num61 += (float)num17 * 40f;
		num61 *= num62;
		num61 /= (float)(num33 + 1);
		if (num32 > 0)
		{
			num61 *= 10f;
		}
		if (num39 > 0)
		{
			num61 *= 0.1f;
		}
		maxHealth = num61;
		float num63 = baseMaxShield + levelMaxShield * num56;
		if (teamComponent.teamIndex == TeamIndex.Player && (bool)inventory && inventory.beadAppliedShield > 0f)
		{
			num63 += inventory.beadAppliedShield;
		}
		num63 += (float)num6 * 0.08f * maxHealth;
		if (HasBuff(RoR2Content.Buffs.EngiShield))
		{
			num63 += maxHealth * 1f;
		}
		if (HasBuff(JunkContent.Buffs.EngiTeamShield))
		{
			num63 += maxHealth * 0.5f;
		}
		if (num48 > 0)
		{
			num63 += maxHealth * 0.1f;
		}
		if (num58)
		{
			num63 += maxHealth * (1.5f + (float)(num15 - 1) * 0.25f);
			maxHealth = 1f;
		}
		if (HasBuff(RoR2Content.Buffs.AffixBlue))
		{
			float num64 = maxHealth * 0.5f;
			maxHealth -= num64;
			num63 += maxHealth;
		}
		maxShield = num63;
		float num65 = baseRegen + levelRegen * num56;
		if (teamComponent.teamIndex == TeamIndex.Player && (bool)inventory && inventory.beadAppliedRegen > 0f)
		{
			num65 += inventory.beadAppliedRegen;
		}
		float num66 = 1f + num56 * 0.2f;
		float num67 = (float)num17 * 1.6f * num66;
		float num68 = ((outOfDanger && num5 > 0) ? (3f * (float)num5) : 0f) * num66;
		float num69 = (HasBuff(JunkContent.Buffs.MeatRegenBoost) ? 2f : 0f) * num66;
		float num70 = (float)GetBuffCount(RoR2Content.Buffs.CrocoRegen) * maxHealth * 0.1f;
		float num71 = (float)num31 * 0.1f * num66;
		float num72 = (float)buffCount2 * 0.07f * num66;
		float num73 = 1f;
		if (num26 > 0)
		{
			num73 += 0.5f;
		}
		if (num29 > 0)
		{
			num73 -= 0.4f;
		}
		float num74 = (num65 + num67 + num68 + num69 + num71 + num72) * num73;
		if (HasBuff(RoR2Content.Buffs.OnFire) || HasBuff(DLC1Content.Buffs.StrongerBurn))
		{
			num74 = Mathf.Min(0f, num74);
		}
		num74 += num70;
		if (num58)
		{
			num74 = Mathf.Max(num74, 0f);
		}
		if (num25 > 0)
		{
			num74 = Mathf.Min(num74, 0f) - maxHealth / cursePenalty / (float)num25;
		}
		if (tamperedHeartActive)
		{
			num74 += tamperedHeartRegenBonus;
		}
		if (HasBuff(DLC2Content.Buffs.HealAndReviveRegenBuff))
		{
			num74 += num74 * 0.5f;
		}
		regen = num74;
		float num75 = baseMoveSpeed + levelMoveSpeed * num56;
		num75 += surfaceSpeedBoost;
		float num76 = 1f;
		if (flag5)
		{
			num75 += 2f;
		}
		if (isSprinting)
		{
			num75 *= sprintingSpeedMultiplier;
		}
		num76 += (float)num7 * 0.14f;
		num76 += (float)num42 * 0.07f;
		num76 += (float)num31 * 0.1f;
		num76 += (float)buffCount2 * 0.07f;
		num76 += 0.25f * (float)GetBuffCount(DLC1Content.Buffs.KillMoveSpeed);
		if (teamComponent.teamIndex == TeamIndex.Monster && Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse4)
		{
			num76 += 0.4f;
		}
		if (isSprinting && num20 > 0)
		{
			num76 += 0.25f * (float)num20 / sprintingSpeedMultiplier;
		}
		if (num8 > 0 && HasBuff(RoR2Content.Buffs.WhipBoost))
		{
			num76 += (float)num8 * 0.3f;
		}
		if (num39 > 0)
		{
			num76 += 0.66f;
		}
		if (HasBuff(RoR2Content.Buffs.BugWings))
		{
			num76 += 0.2f;
		}
		if (HasBuff(RoR2Content.Buffs.Warbanner))
		{
			num76 += 0.3f;
		}
		if (HasBuff(JunkContent.Buffs.EnrageAncientWisp))
		{
			num76 += 0.4f;
		}
		if (HasBuff(RoR2Content.Buffs.CloakSpeed))
		{
			num76 += 0.4f;
		}
		if (HasBuff(RoR2Content.Buffs.WarCryBuff) || HasBuff(RoR2Content.Buffs.TeamWarCry))
		{
			num76 += 0.5f;
		}
		if (HasBuff(JunkContent.Buffs.EngiTeamShield))
		{
			num76 += 0.3f;
		}
		if (HasBuff(RoR2Content.Buffs.AffixLunar))
		{
			num76 += 0.3f;
		}
		if (tamperedHeartActive)
		{
			num75 += num75 * tamperedHeartMSpeedBonus;
		}
		float num77 = 1f;
		if (HasBuff(RoR2Content.Buffs.Slow50))
		{
			num77 += 0.5f;
		}
		if (HasBuff(RoR2Content.Buffs.Slow60))
		{
			num77 += 0.6f;
		}
		if (HasBuff(RoR2Content.Buffs.Slow80))
		{
			num77 += 0.8f;
		}
		if (HasBuff(RoR2Content.Buffs.ClayGoo))
		{
			num77 += 0.5f;
		}
		if (HasBuff(JunkContent.Buffs.Slow30))
		{
			num77 += 0.3f;
		}
		if (HasBuff(RoR2Content.Buffs.Cripple))
		{
			num77 += 1f;
		}
		if (HasBuff(DLC1Content.Buffs.JailerSlow))
		{
			num77 += 1f;
		}
		num77 += (float)num45 * 1f;
		num75 *= num76 / num77;
		if (buffCount > 0)
		{
			num75 *= 1f - 0.05f * (float)buffCount;
		}
		moveSpeed = num75;
		acceleration = ((baseMoveSpeed == 0f) ? 0f : (moveSpeed / baseMoveSpeed * baseAcceleration));
		if (num57 || flag2 || flag3)
		{
			moveSpeed = 0f;
			acceleration = 80f;
		}
		float num78 = baseJumpPower + levelJumpPower * num56;
		jumpPower = num78;
		maxJumpHeight = Trajectory.CalculateApex(jumpPower);
		maxJumpCount = baseJumpCount + num9;
		oneShotProtectionFraction = 0.1f;
		float num79 = baseDamage + levelDamage * num56;
		if (teamComponent.teamIndex == TeamIndex.Player && (bool)inventory && inventory.beadAppliedDamage > 0f)
		{
			num79 += inventory.beadAppliedDamage;
		}
		float num80 = 1f;
		int num81 = (inventory ? inventory.GetItemCount(RoR2Content.Items.BoostDamage) : 0);
		if (num81 > 0)
		{
			num80 += (float)num81 * 0.1f;
		}
		if (num38 > 0)
		{
			int num82 = Math.Max(TeamComponent.GetTeamMembers(teamComponent.teamIndex).Count - 1, 0);
			num80 += (float)(num82 * num38) * 1f;
		}
		if (buffCount > 0)
		{
			num80 -= 0.05f * (float)buffCount;
		}
		if (HasBuff(JunkContent.Buffs.GoldEmpowered))
		{
			num80 += 1f;
		}
		if (HasBuff(RoR2Content.Buffs.PowerBuff))
		{
			num80 += 0.5f;
		}
		num80 += (float)num31 * 0.1f;
		num80 += (float)buffCount2 * 0.07f;
		num80 += Mathf.Pow(2f, num28) - 1f;
		num80 -= (float)num49 * 0.3f;
		num79 *= num80;
		if (num32 > 0)
		{
			num79 *= 0.04f;
		}
		if (flag4)
		{
			num79 *= 5f;
		}
		damage = num79;
		damageFromRecalculateStats = damage;
		if (HasBuff(DLC2Content.Buffs.LowerHealthHigherDamageBuff))
		{
			damage += baseDamage * (float)currentHealthLevel * 0.1f * (float)num52 * 0.5f;
		}
		_ = tamperedHeartActive;
		if (HasBuff(DLC2Content.Buffs.IncreaseDamageBuff))
		{
			int buffCount3 = GetBuffCount(DLC2Content.Buffs.IncreaseDamageBuff);
			float num83 = 0f;
			if (buffCount3 >= 2)
			{
				num83 = (float)(buffCount3 * (buffCount3 + num54 * 5)) * 0.01f;
			}
			else if (buffCount3 == 1)
			{
				num83 = (float)(1 + num54 * 5) * 0.01f;
			}
			damage += baseDamage * num83;
			if (oldComboMeter < buffCount3)
			{
				oldComboMeter = buffCount3;
			}
		}
		float num84 = baseAttackSpeed + levelAttackSpeed * num56;
		float num85 = 1f;
		num85 += (float)num34 * 0.1f;
		num85 += (float)num10 * 0.15f;
		num85 += (float)num42 * 0.075f;
		num85 += (float)num47 * 0.5f;
		if (flag5)
		{
			num85 += 0.5f;
		}
		num85 += (float)GetBuffCount(RoR2Content.Buffs.AttackSpeedOnCrit) * 0.12f;
		if (HasBuff(RoR2Content.Buffs.Warbanner))
		{
			num85 += 0.3f;
		}
		if (HasBuff(RoR2Content.Buffs.Energized))
		{
			num85 += 0.7f;
		}
		if (HasBuff(RoR2Content.Buffs.WarCryBuff) || HasBuff(RoR2Content.Buffs.TeamWarCry))
		{
			num85 += 1f;
		}
		num85 += (float)num31 * 0.1f;
		num85 += (float)buffCount2 * 0.07f;
		num85 /= (float)(num44 + 1);
		num85 = Mathf.Max(num85, 0.1f);
		num84 *= num85;
		if (buffCount > 0)
		{
			num84 *= 1f - 0.05f * (float)buffCount;
		}
		if (tamperedHeartActive)
		{
			num84 += num84 * tamperedHeartAttackSpeedBonus;
		}
		attackSpeed = num84;
		critMultiplier = 2f + 1f * (float)num43;
		float num86 = baseCrit + levelCrit * num56;
		num86 += (float)num11 * 10f;
		if (num12 > 0)
		{
			num86 += 5f;
		}
		if (num35 > 0)
		{
			num86 += 5f;
		}
		if (num13 > 0)
		{
			num86 += 5f;
		}
		if (num14 > 0)
		{
			num86 += 5f;
		}
		if (num19 > 0)
		{
			num86 += 5f;
		}
		if (HasBuff(RoR2Content.Buffs.FullCrit))
		{
			num86 += 100f;
		}
		num86 += (float)num31 * 10f;
		num86 += (float)buffCount2 * 7f;
		if (num46 == 0)
		{
			crit = num86;
		}
		else
		{
			critMultiplier += num86 * 0.01f;
			crit = 0f;
		}
		armor = baseArmor + levelArmor * num56;
		armor *= 1f + 0.1f * (float)num31;
		armor *= 1f + 0.07f * (float)buffCount2;
		armor += (float)num26 * 70f;
		armor += (HasBuff(RoR2Content.Buffs.ArmorBoost) ? 200f : 0f);
		armor += (HasBuff(RoR2Content.Buffs.SmallArmorBoost) ? 100f : 0f);
		armor += (HasBuff(DLC1Content.Buffs.OutOfCombatArmorBuff) ? (100f * (float)num50) : 0f);
		armor += (HasBuff(RoR2Content.Buffs.ElephantArmorBoost) ? 500f : 0f);
		armor += (HasBuff(DLC1Content.Buffs.VoidSurvivorCorruptMode) ? 100f : 0f);
		armor += (HasBuff(DLC2Content.Buffs.AurelioniteBlessing) ? (60f + 0.1f * (float)master.money) : 0f);
		if (HasBuff(RoR2Content.Buffs.Cripple))
		{
			armor -= 20f;
		}
		if (HasBuff(RoR2Content.Buffs.Pulverized))
		{
			armor -= 60f;
		}
		if (isSprinting && num23 > 0)
		{
			armor += num23 * 30;
		}
		if (tamperedHeartActive)
		{
			armor += tamperedHeartArmorBonus;
		}
		int buffCount4 = GetBuffCount(DLC1Content.Buffs.PermanentDebuff);
		armor -= (float)buffCount4 * 2f;
		float num87 = 0f;
		if (num36 > 0)
		{
			num87 += 2f + 1f * (float)(num36 - 1);
		}
		float num88 = 1f;
		if (HasBuff(JunkContent.Buffs.GoldEmpowered))
		{
			num88 *= 0.25f;
		}
		for (int i = 0; i < num16; i++)
		{
			num88 *= 0.75f;
		}
		for (int j = 0; j < num44; j++)
		{
			num88 *= 0.5f;
		}
		for (int k = 0; k < num47; k++)
		{
			num88 *= 0.5f;
		}
		if (teamComponent.teamIndex == TeamIndex.Monster && Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse7)
		{
			num88 *= 0.5f;
		}
		if (HasBuff(RoR2Content.Buffs.NoCooldowns))
		{
			num88 = 0f;
		}
		if ((bool)skillLocator.primary)
		{
			skillLocator.primary.cooldownScale = num88;
			skillLocator.primary.flatCooldownReduction = num87;
		}
		if ((bool)skillLocator.secondaryBonusStockSkill)
		{
			skillLocator.secondaryBonusStockSkill.cooldownScale = num88;
			skillLocator.secondaryBonusStockSkill.SetBonusStockFromBody(num21 + extraSecondaryFromSkill);
			skillLocator.secondaryBonusStockSkill.flatCooldownReduction = num87;
		}
		if ((bool)skillLocator.utilityBonusStockSkill)
		{
			float num89 = num88;
			if (num24 > 0)
			{
				num89 *= 2f / 3f;
			}
			skillLocator.utilityBonusStockSkill.cooldownScale = num89;
			skillLocator.utilityBonusStockSkill.flatCooldownReduction = num87;
			skillLocator.utilityBonusStockSkill.SetBonusStockFromBody(num24 * 2);
		}
		if ((bool)skillLocator.specialBonusStockSkill)
		{
			skillLocator.specialBonusStockSkill.cooldownScale = num88;
			if (num22 > 0)
			{
				skillLocator.specialBonusStockSkill.cooldownScale *= 0.67f;
			}
			skillLocator.specialBonusStockSkill.flatCooldownReduction = num87;
			skillLocator.specialBonusStockSkill.SetBonusStockFromBody(num22 + extraSpecialFromSkill);
		}
		critHeal = 0f;
		if (num19 > 0)
		{
			float num90 = crit;
			crit /= num19 + 1;
			critHeal = num90 - crit;
		}
		cursePenalty = 1f;
		if (num28 > 0)
		{
			cursePenalty = Mathf.Pow(2f, num28);
		}
		if (flag4)
		{
			cursePenalty *= 10f;
		}
		int buffCount5 = GetBuffCount(RoR2Content.Buffs.PermanentCurse);
		if (buffCount5 > 0)
		{
			cursePenalty += (float)buffCount5 * 0.01f;
		}
		if (HasBuff(RoR2Content.Buffs.Weak))
		{
			armor -= 30f;
			damage *= 0.6f;
			moveSpeed *= 0.6f;
		}
		if (HasBuff(DLC2Content.Buffs.lunarruin))
		{
			moveSpeed *= 0.8f;
		}
		if (flag)
		{
			maxHealth *= 1.5f;
			maxShield *= 1.5f;
			attackSpeed *= 1.7f;
			moveSpeed *= 1.3f;
			armor += 20f;
			damage *= 2f;
			regen *= 4f;
		}
		if (HasBuff(DLC2Content.Buffs.BoostAllStatsBuff))
		{
			float num91 = (float)num53 * boostAllStatsMultiplier;
			_ = maxHealth;
			_ = moveSpeed;
			_ = damage;
			_ = attackSpeed;
			_ = crit;
			_ = regen;
			maxHealth += maxHealth * num91;
			moveSpeed += moveSpeed * num91;
			damage += damage * num91;
			attackSpeed += attackSpeed * num91;
			crit += crit * num91;
			regen += regen * num91;
		}
		maxBonusHealth = maxHealth;
		if (num27 > 0 && !flag)
		{
			float num92 = Mathf.Pow(0.95f, num27);
			attackSpeed *= num92;
			moveSpeed *= num92;
			damage *= num92;
			regen *= num92;
			cursePenalty += 0.1f * (float)num27;
		}
		if (HasBuff(DLC2Content.Buffs.SoulCost))
		{
			cursePenalty += 0.1f * (float)GetBuffCount(DLC2Content.Buffs.SoulCost);
		}
		maxHealth /= cursePenalty;
		maxShield /= cursePenalty;
		oneShotProtectionFraction = Mathf.Max(0f, oneShotProtectionFraction - (1f - 1f / cursePenalty));
		maxBarrier = maxHealth + maxShield;
		barrierDecayRate = maxBarrier / 30f;
		if (NetworkServer.active)
		{
			float num93 = maxHealth - num59;
			float num94 = maxShield - num60;
			if (num93 > 0f)
			{
				healthComponent.Heal(num93, default(ProcChainMask), nonRegen: false);
			}
			else if (healthComponent.health > maxHealth)
			{
				healthComponent.Networkhealth = Mathf.Max(healthComponent.health + num93, maxHealth);
			}
			if (num94 > 0f)
			{
				healthComponent.RechargeShield(num94);
			}
			else if (healthComponent.shield > maxShield)
			{
				healthComponent.Networkshield = Mathf.Max(healthComponent.shield + num94, maxShield);
			}
		}
		bleedChance = 10f * (float)num41;
		visionDistance = baseVisionDistance;
		if (HasBuff(DLC1Content.Buffs.Blinded))
		{
			visionDistance = Mathf.Min(visionDistance, 15f);
		}
		if (level != num)
		{
			OnCalculatedLevelChanged(num, level);
		}
		int num95 = 0;
		if (num53 > 0 && !HasBuff(DLC2Content.Buffs.BoostAllStatsBuff))
		{
			BuffIndex[] nonHiddenBuffIndices = BuffCatalog.nonHiddenBuffIndices;
			foreach (BuffIndex buffIndex in nonHiddenBuffIndices)
			{
				if (HasBuff(buffIndex) && buffIndex != DLC2Content.Buffs.BoostAllStatsBuff.buffIndex && !BuffCatalog.ignoreGrowthNectarIndices.Contains(buffIndex))
				{
					num95++;
					if (num95 >= 5 && !HasBuff(DLC2Content.Buffs.BoostAllStatsBuff))
					{
						AddTimedBuff(DLC2Content.Buffs.BoostAllStatsBuff, 5f);
					}
				}
			}
		}
		UpdateAllTemporaryVisualEffects();
		statsDirty = false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void HandleDisableAllSkillsDebuff()
	{
		bool flag = HasBuff(DLC2Content.Buffs.DisableAllSkills);
		if (flag != allSkillsDisabled)
		{
			if (flag)
			{
				HandleSkillDisableState(_disable: true);
				allSkillsDisabled = true;
			}
			else
			{
				HandleSkillDisableState(_disable: false);
				allSkillsDisabled = false;
			}
		}
		void HandleSkillDisableState(bool _disable)
		{
			if (base.hasAuthority)
			{
				SkillDef skillDef = LegacyResourcesAPI.Load<SkillDef>("Skills/DisabledSkills");
				if (!skillDef)
				{
					Debug.LogWarning("Could not find disabledSkill for DisableAllSkills.");
				}
				else if (_disable)
				{
					skillLocator.primary.SetSkillOverride(this, skillDef, GenericSkill.SkillOverridePriority.Contextual);
					skillLocator.secondary.SetSkillOverride(this, skillDef, GenericSkill.SkillOverridePriority.Contextual);
					skillLocator.utility.SetSkillOverride(this, skillDef, GenericSkill.SkillOverridePriority.Contextual);
					skillLocator.special.SetSkillOverride(this, skillDef, GenericSkill.SkillOverridePriority.Contextual);
					inventory.SetEquipmentDisabled(_active: true);
					Util.PlaySound("Play_env_meridian_primeDevestator_debuff", base.gameObject);
					screenDamageSkillsDisabled = new ScreenDamageCalculatorDisabledSkills();
					healthComponent.screenDamageCalculator = screenDamageSkillsDisabled;
				}
				else
				{
					skillLocator.primary.UnsetSkillOverride(this, skillDef, GenericSkill.SkillOverridePriority.Contextual);
					skillLocator.secondary.UnsetSkillOverride(this, skillDef, GenericSkill.SkillOverridePriority.Contextual);
					skillLocator.utility.UnsetSkillOverride(this, skillDef, GenericSkill.SkillOverridePriority.Contextual);
					skillLocator.special.UnsetSkillOverride(this, skillDef, GenericSkill.SkillOverridePriority.Contextual);
					inventory.SetEquipmentDisabled(_active: false);
					if (screenDamageSkillsDisabled != null)
					{
						if (healthComponent.screenDamageCalculator == screenDamageSkillsDisabled)
						{
							healthComponent.screenDamageCalculator = null;
						}
						screenDamageSkillsDisabled.End();
					}
				}
			}
		}
	}

	public void OnTeamLevelChanged()
	{
		statsDirty = true;
	}

	private void OnCalculatedLevelChanged(float oldLevel, float newLevel)
	{
		if (newLevel > oldLevel)
		{
			int num = Mathf.FloorToInt(oldLevel);
			if (Mathf.FloorToInt(newLevel) > num && num != 0)
			{
				OnLevelUp();
			}
		}
	}

	private void OnLevelUp()
	{
		GlobalEventManager.OnCharacterLevelUp(this);
		if (inventory.GetItemCount(DLC2Content.Items.OnLevelUpFreeUnlock) > 0)
		{
			solitudeLevelCount++;
			AddFreeChestBuff();
			Util.PlaySound("Play_item_proc_onLevelUpFreeUnlock_activate", base.gameObject);
		}
	}

	public void SetAimTimer(float duration)
	{
		aimTimer = duration;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		byte num = reader.ReadByte();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			NetworkInstanceId networkInstanceId = reader.ReadNetworkId();
			if (networkInstanceId != masterObjectId)
			{
				masterObjectId = networkInstanceId;
				statsDirty = true;
			}
		}
		if ((num & 2u) != 0)
		{
			ReadBuffs(reader);
		}
		if ((num & 4u) != 0)
		{
			bool flag = reader.ReadBoolean();
			if (!hasEffectiveAuthority && flag != outOfCombat)
			{
				outOfCombat = flag;
				statsDirty = true;
			}
		}
		if ((num & 8u) != 0)
		{
			bool flag2 = reader.ReadBoolean();
			if (flag2 != outOfDanger)
			{
				outOfDanger = flag2;
				statsDirty = true;
			}
		}
		if ((num & 0x10u) != 0)
		{
			bool flag3 = reader.ReadBoolean();
			if (flag3 != isSprinting && !hasEffectiveAuthority)
			{
				statsDirty = true;
				isSprinting = flag3;
			}
		}
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = base.syncVarDirtyBits;
		if (initialState)
		{
			num = 31u;
		}
		bool num2 = (num & 1) != 0;
		bool flag = (num & 2) != 0;
		bool flag2 = (num & 4) != 0;
		bool flag3 = (num & 8) != 0;
		bool flag4 = (num & 0x10) != 0;
		writer.Write((byte)num);
		if (num2)
		{
			writer.Write(masterObjectId);
		}
		if (flag)
		{
			WriteBuffs(writer);
		}
		if (flag2)
		{
			writer.Write(outOfCombat);
		}
		if (flag3)
		{
			writer.Write(outOfDanger);
		}
		if (flag4)
		{
			writer.Write(isSprinting);
		}
		if (!initialState)
		{
			return num != 0;
		}
		return false;
	}

	public void TransmitItemBehavior(NetworkItemBehaviorData itemBehaviorData)
	{
		if (!NetworkServer.active)
		{
			InternalTransmitItemBehavior(itemBehaviorData);
			CallCmdTransmitItemBehavior(itemBehaviorData);
		}
		else
		{
			ServerTransmitItemBehavior(itemBehaviorData);
		}
	}

	[Server]
	private void ServerTransmitItemBehavior(NetworkItemBehaviorData itemBehaviorData)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::ServerTransmitItemBehavior(RoR2.CharacterBody/NetworkItemBehaviorData)' called on client");
			return;
		}
		CallRpcTransmitItemBehavior(itemBehaviorData);
		InternalTransmitItemBehavior(itemBehaviorData);
	}

	[Command]
	private void CmdTransmitItemBehavior(NetworkItemBehaviorData itemBehaviorData)
	{
		ServerTransmitItemBehavior(itemBehaviorData);
	}

	[ClientRpc(channel = 1)]
	private void RpcTransmitItemBehavior(NetworkItemBehaviorData itemBehaviorData)
	{
		if (!NetworkServer.active)
		{
			InternalTransmitItemBehavior(itemBehaviorData);
		}
	}

	private void InternalTransmitItemBehavior(NetworkItemBehaviorData itemBehaviorData)
	{
		OnNetworkItemBehaviorUpdate?.Invoke(itemBehaviorData);
	}

	public T AddItemBehavior<T>(int stack) where T : ItemBehavior
	{
		T val = GetComponent<T>();
		if (stack > 0)
		{
			if (!val)
			{
				val = base.gameObject.AddComponent<T>();
				val.body = this;
				val.enabled = true;
			}
			val.stack = stack;
			return val;
		}
		if ((bool)val)
		{
			UnityEngine.Object.Destroy(val);
		}
		return null;
	}

	public void HandleOnKillEffectsServer(DamageReport damageReport)
	{
		int num = killCountServer + 1;
		killCountServer = num;
		AddMultiKill(1);
	}

	public void OnKilledOtherServer(DamageReport damageReport)
	{
	}

	public void AddHelfireDuration(float duration)
	{
		helfireLifetime = duration;
	}

	[Server]
	private void UpdateHelfire(float deltaTime)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::UpdateHelfire(System.Single)' called on client");
			return;
		}
		helfireLifetime -= deltaTime;
		bool flag = false;
		if ((bool)inventory)
		{
			flag = itemAvailability.helfireCount > 0 || helfireLifetime > 0f;
		}
		if ((bool)helfireController != flag)
		{
			if (flag)
			{
				helfireController = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/HelfireController")).GetComponent<HelfireController>();
				helfireController.networkedBodyAttachment.AttachToGameObjectAndSpawn(base.gameObject);
			}
			else
			{
				UnityEngine.Object.Destroy(helfireController.gameObject);
				helfireController = null;
			}
		}
	}

	private void UpdateFireTrail()
	{
		bool hasFireTrail = itemAvailability.hasFireTrail;
		if (hasFireTrail != (bool)fireTrail)
		{
			if (hasFireTrail)
			{
				fireTrail = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/FireTrail"), transform).GetComponent<DamageTrail>();
				fireTrail.transform.position = footPosition;
				fireTrail.owner = base.gameObject;
				fireTrail.radius *= radius;
			}
			else
			{
				UnityEngine.Object.Destroy(fireTrail.gameObject);
				fireTrail = null;
			}
		}
		if ((bool)fireTrail)
		{
			fireTrail.damagePerSecond = damage * 1.5f;
		}
	}

	private void UpdateAffixPoison(float deltaTime)
	{
		if (!itemAvailability.hasAffixPoison)
		{
			return;
		}
		poisonballTimer += deltaTime;
		if (poisonballTimer >= 6f)
		{
			int num = 3 + (int)radius;
			poisonballTimer = 0f;
			Vector3 up = Vector3.up;
			float num2 = 360f / (float)num;
			Vector3 normalized = Vector3.ProjectOnPlane(transform.forward, up).normalized;
			Vector3 vector = Vector3.RotateTowards(up, normalized, 0.43633232f, float.PositiveInfinity);
			for (int i = 0; i < num; i++)
			{
				Vector3 forward = Quaternion.AngleAxis(num2 * (float)i, up) * vector;
				ProjectileManager.instance.FireProjectile(LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/PoisonOrbProjectile"), corePosition, Util.QuaternionSafeLookRotation(forward), base.gameObject, damage * 1f, 0f, Util.CheckRoll(crit, master));
			}
		}
	}

	private void UpdateAffixLunar(float deltaTime)
	{
		if (!outOfCombat && itemAvailability.hasAffixLunar)
		{
			lunarMissileRechargeTimer += deltaTime;
			lunarMissileTimerBetweenShots += deltaTime;
			int num = 4;
			if (!lunarMissilePrefab)
			{
				lunarMissilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LunarMissileProjectile");
			}
			if (lunarMissileRechargeTimer >= 10f)
			{
				lunarMissileRechargeTimer = 0f;
				remainingMissilesToFire += num;
			}
			if (remainingMissilesToFire > 0 && lunarMissileTimerBetweenShots > 0.1f)
			{
				lunarMissileTimerBetweenShots = 0f;
				Vector3 vector = (inputBank ? inputBank.aimDirection : transform.forward);
				float num2 = 180f / (float)num;
				float num3 = 3f + (float)(int)radius * 1f;
				float num4 = damage * 0.3f;
				Quaternion rotation = Util.QuaternionSafeLookRotation(vector);
				Vector3 vector2 = Quaternion.AngleAxis((float)(remainingMissilesToFire - 1) * num2 - num2 * (float)(num - 1) / 2f, vector) * Vector3.up * num3;
				Vector3 position = aimOrigin + vector2;
				FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
				fireProjectileInfo.projectilePrefab = lunarMissilePrefab;
				fireProjectileInfo.position = position;
				fireProjectileInfo.rotation = rotation;
				fireProjectileInfo.owner = base.gameObject;
				fireProjectileInfo.damage = num4;
				fireProjectileInfo.crit = Util.CheckRoll(crit, master);
				fireProjectileInfo.force = 200f;
				FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
				ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
				remainingMissilesToFire--;
			}
		}
	}

	private void UpdateAllTemporaryVisualEffects()
	{
		int buffCount = GetBuffCount(RoR2Content.Buffs.NullifyStack);
		UpdateSingleTemporaryVisualEffect(ref engiShieldTempEffectInstance, AssetReferences.engiShieldTempEffectPrefab, bestFitRadius, healthComponent.shield > 0f && HasBuff(RoR2Content.Buffs.EngiShield));
		ref TemporaryVisualEffect tempEffect = ref bucklerShieldTempEffectInstance;
		GameObject bucklerShieldTempEffectPrefab = AssetReferences.bucklerShieldTempEffectPrefab;
		float effectRadius = radius;
		int active;
		if (isSprinting)
		{
			Inventory obj = inventory;
			active = (((object)obj != null && obj.GetItemCount(RoR2Content.Items.SprintArmor) > 0) ? 1 : 0);
		}
		else
		{
			active = 0;
		}
		UpdateSingleTemporaryVisualEffect(ref tempEffect, bucklerShieldTempEffectPrefab, effectRadius, (byte)active != 0);
		UpdateSingleTemporaryVisualEffect(ref slowDownTimeTempEffectInstance, AssetReferences.slowDownTimeTempEffectPrefab, radius, HasBuff(RoR2Content.Buffs.Slow60));
		UpdateSingleTemporaryVisualEffect(ref crippleEffectInstance, AssetReferences.crippleEffectPrefab, radius, HasBuff(RoR2Content.Buffs.Cripple));
		UpdateSingleTemporaryVisualEffect(ref tonicBuffEffectInstance, AssetReferences.tonicBuffEffectPrefab, radius, HasBuff(RoR2Content.Buffs.TonicBuff));
		UpdateSingleTemporaryVisualEffect(ref weakTempEffectInstance, AssetReferences.weakTempEffectPrefab, radius, HasBuff(RoR2Content.Buffs.Weak));
		UpdateSingleTemporaryVisualEffect(ref energizedTempEffectInstance, AssetReferences.energizedTempEffectPrefab, radius, HasBuff(RoR2Content.Buffs.Energized));
		UpdateSingleTemporaryVisualEffect(ref barrierTempEffectInstance, AssetReferences.barrierTempEffectPrefab, bestFitRadius, healthComponent.barrier > 0f);
		UpdateSingleTemporaryVisualEffect(ref regenBoostEffectInstance, AssetReferences.regenBoostEffectPrefab, bestFitRadius, HasBuff(JunkContent.Buffs.MeatRegenBoost));
		UpdateSingleTemporaryVisualEffect(ref elephantDefenseEffectInstance, AssetReferences.elephantDefenseEffectPrefab, radius, HasBuff(RoR2Content.Buffs.ElephantArmorBoost));
		UpdateSingleTemporaryVisualEffect(ref healingDisabledEffectInstance, AssetReferences.healingDisabledEffectPrefab, radius, HasBuff(RoR2Content.Buffs.HealingDisabled));
		UpdateSingleTemporaryVisualEffect(ref noCooldownEffectInstance, AssetReferences.noCooldownEffectPrefab, radius, HasBuff(RoR2Content.Buffs.NoCooldowns), "Head");
		ref TemporaryVisualEffect tempEffect2 = ref doppelgangerEffectInstance;
		GameObject doppelgangerEffectPrefab = AssetReferences.doppelgangerEffectPrefab;
		float effectRadius2 = radius;
		Inventory obj2 = inventory;
		UpdateSingleTemporaryVisualEffect(ref tempEffect2, doppelgangerEffectPrefab, effectRadius2, (object)obj2 != null && obj2.GetItemCount(RoR2Content.Items.InvadingDoppelganger) > 0, "Head");
		UpdateSingleTemporaryVisualEffect(ref nullifyStack1EffectInstance, AssetReferences.nullifyStack1EffectPrefab, radius, buffCount == 1);
		UpdateSingleTemporaryVisualEffect(ref nullifyStack2EffectInstance, AssetReferences.nullifyStack2EffectPrefab, radius, buffCount == 2);
		UpdateSingleTemporaryVisualEffect(ref nullifyStack3EffectInstance, AssetReferences.nullifyStack3EffectPrefab, radius, HasBuff(RoR2Content.Buffs.Nullified));
		UpdateSingleTemporaryVisualEffect(ref deathmarkEffectInstance, AssetReferences.deathmarkEffectPrefab, radius, HasBuff(RoR2Content.Buffs.DeathMark));
		UpdateSingleTemporaryVisualEffect(ref crocoRegenEffectInstance, AssetReferences.crocoRegenEffectPrefab, bestFitRadius, HasBuff(RoR2Content.Buffs.CrocoRegen));
		UpdateSingleTemporaryVisualEffect(ref mercExposeEffectInstance, AssetReferences.mercExposeEffectPrefab, radius, HasBuff(RoR2Content.Buffs.MercExpose));
		UpdateSingleTemporaryVisualEffect(ref lifestealOnHitEffectInstance, AssetReferences.lifestealOnHitEffectPrefab, bestFitRadius, HasBuff(RoR2Content.Buffs.LifeSteal));
		UpdateSingleTemporaryVisualEffect(ref teamWarCryEffectInstance, AssetReferences.teamWarCryEffectPrefab, bestFitRadius, HasBuff(RoR2Content.Buffs.TeamWarCry), "HeadCenter");
		UpdateSingleTemporaryVisualEffect(ref lunarGolemShieldEffectInstance, AssetReferences.lunarGolemShieldEffectPrefab, bestFitRadius, HasBuff(RoR2Content.Buffs.LunarShell));
		UpdateSingleTemporaryVisualEffect(ref randomDamageEffectInstance, AssetReferences.randomDamageEffectPrefab, radius, HasBuff(RoR2Content.Buffs.PowerBuff));
		UpdateSingleTemporaryVisualEffect(ref warbannerEffectInstance, AssetReferences.warbannerEffectPrefab, radius, HasBuff(RoR2Content.Buffs.Warbanner));
		UpdateSingleTemporaryVisualEffect(ref teslaFieldEffectInstance, AssetReferences.teslaFieldEffectPrefab, bestFitRadius, HasBuff(RoR2Content.Buffs.TeslaField));
		UpdateSingleTemporaryVisualEffect(ref lunarSecondaryRootEffectInstance, AssetReferences.lunarSecondaryRootEffectPrefab, radius, HasBuff(RoR2Content.Buffs.LunarSecondaryRoot));
		UpdateSingleTemporaryVisualEffect(ref lunarDetonatorEffectInstance, AssetReferences.lunarDetonatorEffectPrefab, radius, HasBuff(RoR2Content.Buffs.LunarDetonationCharge));
		UpdateSingleTemporaryVisualEffect(ref fruitingEffectInstance, AssetReferences.fruitingEffectPrefab, radius, HasBuff(RoR2Content.Buffs.Fruiting));
		UpdateSingleTemporaryVisualEffect(ref mushroomVoidTempEffectInstance, AssetReferences.mushroomVoidTempEffectPrefab, radius, HasBuff(DLC1Content.Buffs.MushroomVoidActive));
		UpdateSingleTemporaryVisualEffect(ref bearVoidTempEffectInstance, AssetReferences.bearVoidTempEffectPrefab, radius, HasBuff(DLC1Content.Buffs.BearVoidReady));
		UpdateSingleTemporaryVisualEffect(ref outOfCombatArmorEffectInstance, AssetReferences.outOfCombatArmorEffectPrefab, radius, HasBuff(DLC1Content.Buffs.OutOfCombatArmorBuff));
		UpdateSingleTemporaryVisualEffect(ref voidFogMildEffectInstance, AssetReferences.voidFogMildEffectPrefab, radius, HasBuff(RoR2Content.Buffs.VoidFogMild));
		UpdateSingleTemporaryVisualEffect(ref voidFogStrongEffectInstance, AssetReferences.voidFogStrongEffectPrefab, radius, HasBuff(RoR2Content.Buffs.VoidFogStrong));
		UpdateSingleTemporaryVisualEffect(ref voidRaidcrabWardWipeFogEffectInstance, AssetReferences.voidRaidcrabWardWipeFogEffectPrefab, radius, HasBuff(DLC1Content.Buffs.VoidRaidCrabWardWipeFog));
		UpdateSingleTemporaryVisualEffect(ref voidJailerSlowEffectInstance, AssetReferences.voidJailerSlowEffectPrefab, radius, HasBuff(DLC1Content.Buffs.JailerSlow));
		UpdateSingleTemporaryVisualEffect(ref aurelioniteBlessingEffectInstance, AssetReferences.aurelioniteBlessingEffectInstance, bestFitActualRadius, HasBuff(DLC2Content.Buffs.AurelioniteBlessing), "Pelvis");
		if ((bool)mushroomVoidTempEffectInstance && !HasBuff(DLC1Content.Buffs.MushroomVoidActive))
		{
			UnityEngine.Object.Destroy(mushroomVoidTempEffectInstance.gameObject);
		}
	}

	private void UpdateSingleTemporaryVisualEffect(ref TemporaryVisualEffect tempEffect, string resourceString, float effectRadius, bool active, string childLocatorOverride = "")
	{
		bool flag = tempEffect != null;
		if (flag == active)
		{
			return;
		}
		if (active)
		{
			if (flag)
			{
				return;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>(resourceString), corePosition, Quaternion.identity);
			tempEffect = gameObject.GetComponent<TemporaryVisualEffect>();
			tempEffect.parentTransform = coreTransform;
			tempEffect.visualState = TemporaryVisualEffect.VisualState.Enter;
			tempEffect.healthComponent = healthComponent;
			tempEffect.radius = effectRadius;
			LocalCameraEffect component = gameObject.GetComponent<LocalCameraEffect>();
			if ((bool)component)
			{
				component.targetCharacter = base.gameObject;
			}
			if (string.IsNullOrEmpty(childLocatorOverride))
			{
				return;
			}
			ChildLocator childLocator = modelLocator?.modelTransform?.GetComponent<ChildLocator>();
			if ((bool)childLocator)
			{
				Transform transform = childLocator.FindChild(childLocatorOverride);
				if ((bool)transform)
				{
					tempEffect.parentTransform = transform;
				}
			}
		}
		else if ((bool)tempEffect)
		{
			tempEffect.visualState = TemporaryVisualEffect.VisualState.Exit;
		}
	}

	private void UpdateSingleTemporaryVisualEffect(ref TemporaryVisualEffect tempEffect, GameObject tempEffectPrefab, float effectRadius, bool active, string childLocatorOverride = "")
	{
		bool flag = tempEffect != null;
		if (flag == active)
		{
			return;
		}
		if (active)
		{
			if (flag)
			{
				return;
			}
			if ((bool)tempEffectPrefab)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(tempEffectPrefab, corePosition, Quaternion.identity);
				tempEffect = gameObject.GetComponent<TemporaryVisualEffect>();
				tempEffect.parentTransform = coreTransform;
				tempEffect.visualState = TemporaryVisualEffect.VisualState.Enter;
				tempEffect.healthComponent = healthComponent;
				tempEffect.radius = effectRadius;
				LocalCameraEffect component = gameObject.GetComponent<LocalCameraEffect>();
				if ((bool)component)
				{
					component.targetCharacter = base.gameObject;
				}
				if (string.IsNullOrEmpty(childLocatorOverride))
				{
					return;
				}
				ChildLocator childLocator = modelLocator?.modelTransform?.GetComponent<ChildLocator>();
				if ((bool)childLocator)
				{
					Transform transform = childLocator.FindChild(childLocatorOverride);
					if ((bool)transform)
					{
						tempEffect.parentTransform = transform;
					}
				}
			}
			else
			{
				Debug.LogError("Can't instantiate null temporary visual effect");
			}
		}
		else if ((bool)tempEffect)
		{
			tempEffect.visualState = TemporaryVisualEffect.VisualState.Exit;
		}
	}

	public VisibilityLevel GetVisibilityLevel(CharacterBody observer)
	{
		return GetVisibilityLevel(observer ? observer.teamComponent.teamIndex : TeamIndex.None);
	}

	public VisibilityLevel GetVisibilityLevel(TeamIndex observerTeam)
	{
		if (!hasCloakBuff)
		{
			return VisibilityLevel.Visible;
		}
		if (teamComponent.teamIndex != observerTeam)
		{
			return VisibilityLevel.Cloaked;
		}
		return VisibilityLevel.Revealed;
	}

	public void AddSpreadBloom(float value)
	{
		spreadBloomInternal = Mathf.Min(spreadBloomInternal + value, 1f);
	}

	public void SetSpreadBloom(float value, bool canOnlyIncreaseBloom = true)
	{
		if (canOnlyIncreaseBloom)
		{
			spreadBloomInternal = Mathf.Clamp(value, spreadBloomInternal, 1f);
		}
		else
		{
			spreadBloomInternal = Mathf.Min(value, 1f);
		}
	}

	private void UpdateSpreadBloom(float dt)
	{
		float num = 1f / spreadBloomDecayTime;
		spreadBloomInternal = Mathf.Max(spreadBloomInternal - num * dt, 0f);
	}

	[Client]
	public void SendConstructTurret(CharacterBody builder, Vector3 position, Quaternion rotation, MasterCatalog.MasterIndex masterIndex)
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.CharacterBody::SendConstructTurret(RoR2.CharacterBody,UnityEngine.Vector3,UnityEngine.Quaternion,RoR2.MasterCatalog/MasterIndex)' called on server");
			return;
		}
		ConstructTurretMessage msg = new ConstructTurretMessage
		{
			builder = builder.gameObject,
			position = position,
			rotation = rotation,
			turretMasterIndex = masterIndex
		};
		ClientScene.readyConnection.Send(62, msg);
	}

	[NetworkMessageHandler(msgType = 62, server = true)]
	private static void HandleConstructTurret(NetworkMessage netMsg)
	{
		ConstructTurretMessage constructTurretMessage = netMsg.ReadMessage<ConstructTurretMessage>();
		if (!constructTurretMessage.builder)
		{
			return;
		}
		CharacterBody component = constructTurretMessage.builder.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			CharacterMaster characterMaster = component.master;
			if ((bool)characterMaster)
			{
				CharacterMaster characterMaster2 = new MasterSummon
				{
					masterPrefab = MasterCatalog.GetMasterPrefab(constructTurretMessage.turretMasterIndex),
					position = constructTurretMessage.position,
					rotation = constructTurretMessage.rotation,
					summonerBodyObject = component.gameObject,
					ignoreTeamMemberLimit = true,
					inventoryToCopy = characterMaster.inventory
				}.Perform();
				Deployable deployable = characterMaster2.gameObject.AddComponent<Deployable>();
				deployable.onUndeploy = new UnityEvent();
				deployable.onUndeploy.AddListener(characterMaster2.TrueKill);
				characterMaster.AddDeployable(deployable, DeployableSlot.EngiTurret);
			}
		}
	}

	public void AddIncreasedDamageMultiKillTime()
	{
		increasedDamageKillTimer = 5f;
	}

	[Server]
	public void AddMultiKill(int kills)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::AddMultiKill(System.Int32)' called on client");
			return;
		}
		multiKillTimer = 1f;
		multiKillCount += kills;
		int num = (inventory ? inventory.GetItemCount(RoR2Content.Items.WarCryOnMultiKill) : 0);
		if (num > 0 && multiKillCount >= 4)
		{
			AddTimedBuff(RoR2Content.Buffs.WarCryBuff, 2f + 4f * (float)num);
		}
		increasedDamageKillTimer = 5f;
		increasedDamageKillCount += kills;
		increasedDamageKillCountBuff += kills;
		if ((((bool)inventory && inventory.GetItemCount(DLC2Content.Items.IncreaseDamageOnMultiKill) != 0) ? 1 : 0) > (false ? 1 : 0))
		{
			if (increasedDamageKillCountBuff >= 5)
			{
				AddBuff(DLC2Content.Buffs.IncreaseDamageBuff);
				EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/IncreaseDamageOnMultiKillVFX"), new EffectData
				{
					origin = base.gameObject.transform.position
				}, transmit: true);
				increasedDamageKillCountBuff = 0;
			}
			if (increasedDamageKillCount > oldKillcount)
			{
				oldKillcount = increasedDamageKillCount;
			}
		}
	}

	[Server]
	private void UpdateMultiKill(float deltaTime)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::UpdateMultiKill(System.Single)' called on client");
			return;
		}
		multiKillTimer -= deltaTime;
		if (multiKillTimer <= 0f)
		{
			multiKillTimer = 0f;
			multiKillCount = 0;
		}
		increasedDamageKillTimer -= deltaTime;
		if (increasedDamageKillTimer <= 0f)
		{
			increasedDamageKillTimer = 0f;
			increasedDamageKillCount = 0;
			increasedDamageKillCountBuff = 0;
			oldKillcount = 0;
			oldComboMeter = 0;
			if (HasBuff(DLC2Content.Buffs.IncreaseDamageBuff))
			{
				RemoveBuff(DLC2Content.Buffs.IncreaseDamageBuff);
			}
		}
	}

	public void AddIncreasePrimaryDamageStack()
	{
		int buffCount = GetBuffCount(DLC2Content.Buffs.IncreasePrimaryDamageBuff);
		int itemCount = inventory.GetItemCount(DLC2Content.Items.IncreasePrimaryDamage);
		int num = 4 + itemCount;
		if (buffCount < num)
		{
			AddBuff(DLC2Content.Buffs.IncreasePrimaryDamageBuff);
		}
		buffCount = GetBuffCount(DLC2Content.Buffs.IncreasePrimaryDamageBuff);
		TransmitItemBehavior(new NetworkItemBehaviorData(DLC2Content.Items.IncreasePrimaryDamage.itemIndex, buffCount));
		float num2 = baseDamage + levelDamage * (level - 1f);
		luminousShotDamage = num2 * (1.25f + 0.25f * (float)itemCount) * (float)buffCount;
	}

	public void IncreasePrimaryDamageReset()
	{
		if (GetBuffCount(DLC2Content.Buffs.IncreasePrimaryDamageBuff) > 0)
		{
			SetBuffCount(DLC2Content.Buffs.IncreasePrimaryDamageBuff.buffIndex, 0);
			TransmitItemBehavior(new NetworkItemBehaviorData(DLC2Content.Items.IncreasePrimaryDamage.itemIndex, 0f));
		}
		luminousShotReady = false;
	}

	[Server]
	private void UpdateDelayedDamage(float deltaTime)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::UpdateDelayedDamage(System.Single)' called on client");
			return;
		}
		int num = (inventory ? inventory.GetItemCount(DLC2Content.Items.DelayedDamage) : 0);
		if (num > 0)
		{
			int buffCount = GetBuffCount(DLC2Content.Buffs.DelayedDamageBuff);
			if (oldDelayedDamageCount < num)
			{
				int num2 = num - oldDelayedDamageCount;
				for (int i = 0; i < num2; i++)
				{
					AddBuff(DLC2Content.Buffs.DelayedDamageBuff);
				}
				oldDelayedDamageCount = num;
			}
			else if (oldDelayedDamageCount > num)
			{
				oldDelayedDamageCount = num;
			}
			if (buffCount < num)
			{
				delayedDamageRefreshTime -= deltaTime;
				if (delayedDamageRefreshTime <= 0f)
				{
					AddBuff(DLC2Content.Buffs.DelayedDamageBuff);
					delayedDamageRefreshTime = 10f;
				}
			}
		}
		else
		{
			delayedDamageRefreshTime = 10f;
			oldDelayedDamageCount = 0;
			RemoveBuff(DLC2Content.Buffs.DelayedDamageBuff);
			RemoveBuff(DLC2Content.Buffs.DelayedDamageDebuff);
			RemoveOldestTimedBuff(DLC2Content.Buffs.DelayedDamageDebuff);
		}
	}

	public void SpawnDelayedDamageEffect()
	{
		if (NetworkServer.active)
		{
			ServerSpawnDelayedDamageEffect();
		}
		else
		{
			CallCmdSpawnDelayedDamageEffect();
		}
	}

	[Server]
	private void ServerSpawnDelayedDamageEffect()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::ServerSpawnDelayedDamageEffect()' called on client");
		}
		else
		{
			UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/DelayedDamageIndicator"), mainHurtBox ? mainHurtBox.transform.position : transform.position, Quaternion.identity, mainHurtBox ? mainHurtBox.transform : transform);
		}
	}

	[Command]
	private void CmdSpawnDelayedDamageEffect()
	{
		ServerSpawnDelayedDamageEffect();
	}

	public void SecondHalfOfDelayedDamage(DamageInfo halfDamage)
	{
		RemoveBuff(DLC2Content.Buffs.DelayedDamageBuff);
		AddBuff(DLC2Content.Buffs.DelayedDamageDebuff);
		TransmitItemBehavior(new NetworkItemBehaviorData(DLC2Content.Items.DelayedDamage.itemIndex, 0f));
		DelayedDamageInfo delayedDamageInfo = new DelayedDamageInfo();
		delayedDamageInfo.halfDamage = halfDamage;
		incomingDamageList.Add(delayedDamageInfo);
		if (incomingDamageList.Count == 0)
		{
			halfDamageReady = false;
		}
		else
		{
			halfDamageReady = true;
		}
	}

	[Server]
	private void UpdateSecondHalfOfDamage(float deltaTime)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::UpdateSecondHalfOfDamage(System.Single)' called on client");
		}
		else
		{
			if (!halfDamageReady)
			{
				return;
			}
			for (int i = 0; i < incomingDamageList.Count; i++)
			{
				DelayedDamageInfo delayedDamageInfo = incomingDamageList[i];
				DamageInfo halfDamage = delayedDamageInfo.halfDamage;
				halfDamage.position = GetBody().corePosition;
				if (protectFromOneShot)
				{
					halfDamage.damageType = DamageType.NonLethal;
				}
				delayedDamageInfo.timeUntilDamage -= deltaTime;
				if (delayedDamageInfo.timeUntilDamage <= 0f)
				{
					Util.PlaySound("Play_item_proc_delayedDamage_2ndHit", base.gameObject);
					healthComponent.TakeDamage(halfDamage);
					RemoveBuff(DLC2Content.Buffs.DelayedDamageDebuff);
					incomingDamageList.RemoveAt(i);
					protectFromOneShot = false;
				}
			}
		}
	}

	[ClientRpc]
	public void RpcTeleportCharacterToSafety()
	{
		if (!hasEffectiveAuthority)
		{
			return;
		}
		Transform transform = modelLocator?.modelTransform;
		if (!(transform == null))
		{
			CharacterModel component = transform.GetComponent<CharacterModel>();
			Vector3 position = coreTransform.position;
			int itemCount = inventory.GetItemCount(DLC2Content.Items.TeleportOnLowHealth);
			BlastAttack blastAttack = new BlastAttack();
			blastAttack.attacker = base.gameObject;
			blastAttack.baseDamage = damage * ((float)itemCount * 3.5f);
			blastAttack.baseForce = 2000f;
			blastAttack.bonusForce = Vector3.zero;
			blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
			blastAttack.crit = RollCrit();
			blastAttack.damageColorIndex = DamageColorIndex.Item;
			blastAttack.damageType = DamageType.Generic;
			blastAttack.falloffModel = BlastAttack.FalloffModel.None;
			blastAttack.inflictor = base.gameObject;
			blastAttack.position = position;
			blastAttack.procChainMask = default(ProcChainMask);
			blastAttack.procCoefficient = 1f;
			blastAttack.radius = 25f;
			blastAttack.losType = BlastAttack.LoSType.None;
			blastAttack.teamIndex = TeamIndex.Player;
			blastAttack.Fire();
			EffectManager.SpawnEffect(CommonAssets.teleportOnLowHealthExplosion, new EffectData
			{
				origin = position,
				scale = 50f,
				rotation = Quaternion.identity
			}, transmit: true);
			Util.PlaySound("Play_item_proc_teleportOnLowHealth", base.gameObject);
			NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
			List<NodeGraph.NodeIndex> source = nodeGraph.FindNodesInRange(corePosition, 100f, 200f, HullMask.Human);
			nodeGraph.GetNodePosition(source.First(), out var position2);
			TeleportHelper.TeleportBody(this, position2);
			TeleportOutController.AddTPOutEffect(component, 1f, 0f, 1f);
			EffectManager.SpawnEffect(CommonAssets.teleportOnLowHealthVFX, new EffectData
			{
				origin = this.transform.position,
				rotation = Quaternion.identity
			}, transmit: true);
			Vector3 normalized = (position - position2).normalized;
			if ((bool)characterDirection)
			{
				characterDirection.forward = normalized;
			}
			if ((bool)master && (bool)master.playerCharacterMasterController)
			{
				master.playerCharacterMasterController.RedirectCamera(Quaternion.LookRotation(normalized).eulerAngles);
			}
		}
	}

	private void prayerBeadCalculateAppliedStats(float levelStat)
	{
		if (teamComponent.teamIndex == TeamIndex.Player)
		{
			float num = (float)GetBuffCount(DLC2Content.Buffs.ExtraStatsOnLevelUpBuff) * levelStat * (0.2f + 0.05f * (float)(inventory.GetItemCount(DLC2Content.Items.ExtraStatsOnLevelUp) - 1));
			if (levelStat == levelMaxHealth)
			{
				_ = inventory.beadAppliedHealth;
				inventory.beadAppliedHealth += num;
			}
			else if (levelStat == levelMaxShield)
			{
				_ = inventory.beadAppliedShield;
				inventory.beadAppliedShield += num;
			}
			else if (levelStat == levelRegen)
			{
				_ = inventory.beadAppliedRegen;
				inventory.beadAppliedRegen += num;
			}
			else if (levelStat == levelDamage)
			{
				_ = inventory.beadAppliedDamage;
				inventory.beadAppliedDamage += num;
			}
		}
	}

	[HideInInspector]
	public void runicLensUpdateVariables(float damage, Vector3 position, DamageInfo damageInfo, float blastForce, float blastRadius)
	{
		runicLensPosition = position;
		runicLensImpactTime = Run.instance.time + 2f;
		runicLensMeteorReady = true;
		runicLensDamage = damage;
		runicLensDamageInfo = damageInfo;
		runicLensBlastForce = blastForce;
		runicLensBlastRadius = blastRadius;
	}

	public void detonateRunicLensMeteor()
	{
		GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/RunicMeteorStrikeImpact");
		Util.PlaySound("Play_item_proc_meteorAttackOnHighDamage_impact", base.gameObject);
		EffectData effectData = new EffectData
		{
			origin = runicLensPosition
		};
		EffectManager.SpawnEffect(gameObject, effectData, transmit: true);
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.falloffModel = BlastAttack.FalloffModel.None;
		blastAttack.inflictor = gameObject;
		blastAttack.baseDamage = runicLensDamage;
		blastAttack.baseForce = runicLensBlastForce;
		blastAttack.attackerFiltering = AttackerFiltering.Default;
		blastAttack.crit = runicLensDamageInfo.crit;
		blastAttack.attacker = runicLensDamageInfo.attacker;
		blastAttack.bonusForce = Vector3.zero;
		blastAttack.damageColorIndex = DamageColorIndex.Item;
		blastAttack.position = runicLensPosition;
		blastAttack.procChainMask = default(ProcChainMask);
		blastAttack.procCoefficient = 0.5f;
		blastAttack.teamIndex = TeamIndex.Player;
		blastAttack.radius = runicLensBlastRadius;
		blastAttack.Fire();
	}

	public void TriggerEnemyDebuffs(DamageInfo damageInfo)
	{
		SphereSearch sphereSearch = new SphereSearch();
		List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
		sphereSearch.mask = LayerIndex.entityPrecise.mask;
		sphereSearch.origin = base.gameObject.transform.position;
		sphereSearch.radius = 30f;
		sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
		sphereSearch.RefreshCandidates();
		sphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(teamComponent.teamIndex));
		sphereSearch.OrderCandidatesByDistance();
		sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
		sphereSearch.GetHurtBoxes(list);
		sphereSearch.ClearCandidates();
		EffectManager.SpawnEffect(CommonAssets.thornExplosionEffect, new EffectData
		{
			origin = coreTransform.position,
			scale = 60f,
			rotation = Quaternion.identity
		}, transmit: true);
		Util.PlaySound("Play_item_proc_triggerEnemyDebuffs", base.gameObject);
		for (int i = 0; i < list.Count; i++)
		{
			HurtBox hurtBox = list[i];
			if (!hurtBox || !hurtBox.healthComponent || !hurtBox.healthComponent.alive)
			{
				continue;
			}
			CharacterBody body = hurtBox.healthComponent.body;
			int itemCount = inventory.GetItemCount(DLC2Content.Items.TriggerEnemyDebuffs);
			int num = 1;
			if (!body.hasStackableDebuff)
			{
				DotController.InflictDot(body.gameObject, damageInfo.attacker, DotController.DotIndex.Bleed, 3f * damageInfo.procCoefficient);
				continue;
			}
			int buffCount = body.GetBuffCount(RoR2Content.Buffs.OnFire);
			if (buffCount > 0)
			{
				float num2 = 0.5f;
				int num3 = num * itemCount;
				InflictDotInfo inflictDotInfo = default(InflictDotInfo);
				inflictDotInfo.attackerObject = damageInfo.attacker;
				inflictDotInfo.victimObject = body.gameObject;
				inflictDotInfo.totalDamage = damageInfo.damage * num2;
				inflictDotInfo.damageMultiplier = 1f;
				inflictDotInfo.dotIndex = DotController.DotIndex.Burn;
				InflictDotInfo inflictDotInfo2 = inflictDotInfo;
				for (int j = 0; j < num3; j++)
				{
					DotController.InflictDot(ref inflictDotInfo2);
				}
			}
			buffCount = body.GetBuffCount(RoR2Content.Buffs.BeetleJuice);
			if (buffCount > 0)
			{
				int newCount = buffCount + num * itemCount;
				body.SetBuffCount(RoR2Content.Buffs.BeetleJuice.buffIndex, newCount);
			}
			buffCount = body.GetBuffCount(RoR2Content.Buffs.NullifyStack);
			if (buffCount > 0)
			{
				int newCount2 = buffCount + num * itemCount;
				body.SetBuffCount(RoR2Content.Buffs.NullifyStack.buffIndex, newCount2);
			}
			buffCount = body.GetBuffCount(RoR2Content.Buffs.Bleeding);
			if (buffCount > 0)
			{
				int num4 = num * itemCount;
				for (int k = 0; k < num4; k++)
				{
					DotController.InflictDot(body.gameObject, damageInfo.attacker, DotController.DotIndex.Bleed, 3f * damageInfo.procCoefficient);
				}
			}
			buffCount = body.GetBuffCount(RoR2Content.Buffs.SuperBleed);
			if (buffCount > 0)
			{
				int num5 = num * itemCount;
				for (int l = 0; l < num5; l++)
				{
					DotController.InflictDot(body.gameObject, damageInfo.attacker, DotController.DotIndex.SuperBleed, 15f * damageInfo.procCoefficient);
				}
			}
			buffCount = body.GetBuffCount(RoR2Content.Buffs.Blight);
			if (buffCount > 0)
			{
				int num6 = num * itemCount;
				for (int m = 0; m < num6; m++)
				{
					DotController.InflictDot(body.gameObject, damageInfo.attacker, DotController.DotIndex.Blight, 5f * damageInfo.procCoefficient);
				}
			}
			buffCount = body.GetBuffCount(RoR2Content.Buffs.Overheat);
			if (buffCount > 0)
			{
				int newCount3 = buffCount + num * itemCount;
				body.SetBuffCount(RoR2Content.Buffs.Overheat.buffIndex, newCount3);
			}
			buffCount = body.GetBuffCount(RoR2Content.Buffs.PulverizeBuildup);
			if (buffCount > 0)
			{
				int newCount4 = buffCount + num * itemCount;
				body.SetBuffCount(RoR2Content.Buffs.PulverizeBuildup.buffIndex, newCount4);
			}
			buffCount = body.GetBuffCount(DLC1Content.Buffs.StrongerBurn);
			if (buffCount > 0)
			{
				int num7 = num * itemCount;
				for (int n = 0; n < num7; n++)
				{
					DotController.InflictDot(body.gameObject, damageInfo.attacker, DotController.DotIndex.StrongerBurn, 3f * damageInfo.procCoefficient);
				}
			}
			buffCount = body.GetBuffCount(DLC1Content.Buffs.Fracture);
			if (buffCount > 0)
			{
				int num8 = num * itemCount;
				DotController.DotDef dotDef = DotController.GetDotDef(DotController.DotIndex.Fracture);
				for (num = 0; num < num8; num++)
				{
					DotController.InflictDot(body.gameObject, damageInfo.attacker, DotController.DotIndex.Fracture, dotDef.interval);
				}
			}
			buffCount = body.GetBuffCount(DLC1Content.Buffs.PermanentDebuff);
			if (buffCount > 0)
			{
				int newCount5 = buffCount + num * itemCount;
				body.SetBuffCount(DLC1Content.Buffs.PermanentDebuff.buffIndex, newCount5);
			}
			buffCount = body.GetBuffCount(DLC2Content.Buffs.lunarruin);
			if (buffCount > 0)
			{
				int num2 = buffCount + num * itemCount;
				body.SetBuffCount(DLC2Content.Buffs.lunarruin.buffIndex, num2);
				for (num = 0; num < num2; num++)
				{
					DotController.InflictDot(body.gameObject, damageInfo.attacker, DotController.DotIndex.LunarRuin, 5f);
				}
			}
		}
	}

	private void IncreaseStunPierceDamage(int kills)
	{
	}

	public void ApplyTamperedHeartPassive(float heartRegen, float heartMSpeed, float heartDamage, float armor, float attackSpeed)
	{
		tamperedHeartActive = true;
		tamperedHeartRegenBonus = heartRegen;
		tamperedHeartMSpeedBonus = heartMSpeed;
		tamperedHeartDamageBonus = heartDamage;
		tamperedHeartAttackSpeedBonus = attackSpeed;
		tamperedHeartArmorBonus = armor;
	}

	public void AddFreeChestBuff()
	{
		int itemCount = inventory.GetItemCount(DLC2Content.Items.OnLevelUpFreeUnlock);
		for (int i = 0; i < itemCount; i++)
		{
			AddBuff(DLC2Content.Buffs.FreeUnlocks.buffIndex);
		}
	}

	[ClientRpc]
	public void RpcBark()
	{
		if ((bool)sfxLocator)
		{
			Util.PlaySound(sfxLocator.barkSound, base.gameObject);
		}
	}

	[Command]
	public void CmdRequestVehicleEjection()
	{
		if ((bool)currentVehicle)
		{
			currentVehicle.EjectPassenger(base.gameObject);
		}
	}

	public bool RollCrit()
	{
		if ((bool)master)
		{
			return Util.CheckRoll(crit, master);
		}
		return false;
	}

	public bool IsCullable()
	{
		if (!dontCull && Importance - TeamComponent.GetTeamAverageImportance(teamComponent) < 3 && teamComponent.teamIndex != TeamIndex.Player && !isBoss)
		{
			return TeamComponent.GetTeamMembers(TeamIndex.Monster).Count >= 10;
		}
		return false;
	}

	public bool IsActivelyCullable()
	{
		if (!isBoss)
		{
			if (!NearAnyPlayers(75f))
			{
				return !CharacterIsVisible;
			}
			return false;
		}
		return false;
	}

	public bool NearAnyPlayers(float checkRadius)
	{
		float num = checkRadius * checkRadius;
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			if ((bool)instance.master && (bool)instance.master.GetBody() && (transform.position - instance.master.GetBody().transform.position).sqrMagnitude < num)
			{
				return true;
			}
		}
		return false;
	}

	[ClientRpc]
	private void RpcUsePreferredInitialStateType()
	{
		if (hasEffectiveAuthority)
		{
			SetBodyStateToPreferredInitialState();
		}
	}

	public void SetBodyStateToPreferredInitialState()
	{
		if (!hasEffectiveAuthority)
		{
			if (NetworkServer.active)
			{
				CallRpcUsePreferredInitialStateType();
			}
			return;
		}
		Type stateType = preferredInitialStateType.stateType;
		if (!(stateType == null) && !(stateType == typeof(Uninitialized)))
		{
			EntityStateMachine.FindByCustomName(base.gameObject, "Body")?.SetState(EntityStateCatalog.InstantiateState(stateType));
		}
	}

	[Server]
	public void SetLoadoutServer(Loadout loadout)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterBody::SetLoadoutServer(RoR2.Loadout)' called on client");
		}
		else
		{
			skillLocator.ApplyLoadoutServer(loadout, bodyIndex);
		}
	}

	static CharacterBody()
	{
		instancesList = new List<CharacterBody>();
		readOnlyInstancesList = new ReadOnlyCollection<CharacterBody>(instancesList);
		kCmdCmdAddTimedBuff = -160178508;
		NetworkBehaviour.RegisterCommandDelegate(typeof(CharacterBody), kCmdCmdAddTimedBuff, InvokeCmdCmdAddTimedBuff);
		kCmdCmdSetInLava = 234733148;
		NetworkBehaviour.RegisterCommandDelegate(typeof(CharacterBody), kCmdCmdSetInLava, InvokeCmdCmdSetInLava);
		kCmdCmdUpdateSprint = -1006016914;
		NetworkBehaviour.RegisterCommandDelegate(typeof(CharacterBody), kCmdCmdUpdateSprint, InvokeCmdCmdUpdateSprint);
		kCmdCmdOnSkillActivated = 384138986;
		NetworkBehaviour.RegisterCommandDelegate(typeof(CharacterBody), kCmdCmdOnSkillActivated, InvokeCmdCmdOnSkillActivated);
		kCmdCmdTransmitItemBehavior = 394448000;
		NetworkBehaviour.RegisterCommandDelegate(typeof(CharacterBody), kCmdCmdTransmitItemBehavior, InvokeCmdCmdTransmitItemBehavior);
		kCmdCmdSpawnDelayedDamageEffect = 1860577938;
		NetworkBehaviour.RegisterCommandDelegate(typeof(CharacterBody), kCmdCmdSpawnDelayedDamageEffect, InvokeCmdCmdSpawnDelayedDamageEffect);
		kCmdCmdRequestVehicleEjection = 1803737791;
		NetworkBehaviour.RegisterCommandDelegate(typeof(CharacterBody), kCmdCmdRequestVehicleEjection, InvokeCmdCmdRequestVehicleEjection);
		kRpcRpcTransmitItemBehavior = -729896618;
		NetworkBehaviour.RegisterRpcDelegate(typeof(CharacterBody), kRpcRpcTransmitItemBehavior, InvokeRpcRpcTransmitItemBehavior);
		kRpcRpcTeleportCharacterToSafety = -245876694;
		NetworkBehaviour.RegisterRpcDelegate(typeof(CharacterBody), kRpcRpcTeleportCharacterToSafety, InvokeRpcRpcTeleportCharacterToSafety);
		kRpcRpcBark = -76716871;
		NetworkBehaviour.RegisterRpcDelegate(typeof(CharacterBody), kRpcRpcBark, InvokeRpcRpcBark);
		kRpcRpcUsePreferredInitialStateType = 638695010;
		NetworkBehaviour.RegisterRpcDelegate(typeof(CharacterBody), kRpcRpcUsePreferredInitialStateType, InvokeRpcRpcUsePreferredInitialStateType);
		NetworkCRC.RegisterBehaviour("CharacterBody", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdAddTimedBuff(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdAddTimedBuff called on client.");
		}
		else
		{
			((CharacterBody)obj).CmdAddTimedBuff((BuffIndex)reader.ReadInt32(), reader.ReadSingle());
		}
	}

	protected static void InvokeCmdCmdSetInLava(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetInLava called on client.");
		}
		else
		{
			((CharacterBody)obj).CmdSetInLava(reader.ReadBoolean());
		}
	}

	protected static void InvokeCmdCmdUpdateSprint(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUpdateSprint called on client.");
		}
		else
		{
			((CharacterBody)obj).CmdUpdateSprint(reader.ReadBoolean());
		}
	}

	protected static void InvokeCmdCmdOnSkillActivated(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdOnSkillActivated called on client.");
		}
		else
		{
			((CharacterBody)obj).CmdOnSkillActivated((sbyte)reader.ReadPackedUInt32());
		}
	}

	protected static void InvokeCmdCmdTransmitItemBehavior(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdTransmitItemBehavior called on client.");
		}
		else
		{
			((CharacterBody)obj).CmdTransmitItemBehavior(GeneratedNetworkCode._ReadNetworkItemBehaviorData_CharacterBody(reader));
		}
	}

	protected static void InvokeCmdCmdSpawnDelayedDamageEffect(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSpawnDelayedDamageEffect called on client.");
		}
		else
		{
			((CharacterBody)obj).CmdSpawnDelayedDamageEffect();
		}
	}

	protected static void InvokeCmdCmdRequestVehicleEjection(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestVehicleEjection called on client.");
		}
		else
		{
			((CharacterBody)obj).CmdRequestVehicleEjection();
		}
	}

	public void CallCmdAddTimedBuff(BuffIndex buffType, float duration)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdAddTimedBuff called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdAddTimedBuff(buffType, duration);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdAddTimedBuff);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write((int)buffType);
		networkWriter.Write(duration);
		SendCommandInternal(networkWriter, 0, "CmdAddTimedBuff");
	}

	public void CallCmdSetInLava(bool b)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSetInLava called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSetInLava(b);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSetInLava);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(b);
		SendCommandInternal(networkWriter, 0, "CmdSetInLava");
	}

	public void CallCmdUpdateSprint(bool newIsSprinting)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdUpdateSprint called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdUpdateSprint(newIsSprinting);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdUpdateSprint);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(newIsSprinting);
		SendCommandInternal(networkWriter, 0, "CmdUpdateSprint");
	}

	public void CallCmdOnSkillActivated(sbyte skillIndex)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdOnSkillActivated called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdOnSkillActivated(skillIndex);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdOnSkillActivated);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32((uint)skillIndex);
		SendCommandInternal(networkWriter, 0, "CmdOnSkillActivated");
	}

	public void CallCmdTransmitItemBehavior(NetworkItemBehaviorData itemBehaviorData)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdTransmitItemBehavior called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdTransmitItemBehavior(itemBehaviorData);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdTransmitItemBehavior);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteNetworkItemBehaviorData_CharacterBody(networkWriter, itemBehaviorData);
		SendCommandInternal(networkWriter, 0, "CmdTransmitItemBehavior");
	}

	public void CallCmdSpawnDelayedDamageEffect()
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSpawnDelayedDamageEffect called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSpawnDelayedDamageEffect();
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSpawnDelayedDamageEffect);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendCommandInternal(networkWriter, 0, "CmdSpawnDelayedDamageEffect");
	}

	public void CallCmdRequestVehicleEjection()
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdRequestVehicleEjection called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdRequestVehicleEjection();
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdRequestVehicleEjection);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendCommandInternal(networkWriter, 0, "CmdRequestVehicleEjection");
	}

	protected static void InvokeRpcRpcTransmitItemBehavior(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcTransmitItemBehavior called on server.");
		}
		else
		{
			((CharacterBody)obj).RpcTransmitItemBehavior(GeneratedNetworkCode._ReadNetworkItemBehaviorData_CharacterBody(reader));
		}
	}

	protected static void InvokeRpcRpcTeleportCharacterToSafety(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcTeleportCharacterToSafety called on server.");
		}
		else
		{
			((CharacterBody)obj).RpcTeleportCharacterToSafety();
		}
	}

	protected static void InvokeRpcRpcBark(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcBark called on server.");
		}
		else
		{
			((CharacterBody)obj).RpcBark();
		}
	}

	protected static void InvokeRpcRpcUsePreferredInitialStateType(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUsePreferredInitialStateType called on server.");
		}
		else
		{
			((CharacterBody)obj).RpcUsePreferredInitialStateType();
		}
	}

	public void CallRpcTransmitItemBehavior(NetworkItemBehaviorData itemBehaviorData)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcTransmitItemBehavior called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcTransmitItemBehavior);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteNetworkItemBehaviorData_CharacterBody(networkWriter, itemBehaviorData);
		SendRPCInternal(networkWriter, 1, "RpcTransmitItemBehavior");
	}

	public void CallRpcTeleportCharacterToSafety()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcTeleportCharacterToSafety called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcTeleportCharacterToSafety);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcTeleportCharacterToSafety");
	}

	public void CallRpcBark()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcBark called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcBark);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcBark");
	}

	public void CallRpcUsePreferredInitialStateType()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcUsePreferredInitialStateType called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcUsePreferredInitialStateType);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcUsePreferredInitialStateType");
	}

	public override void PreStartClient()
	{
	}
}
