using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EntityStates;
using HG;
using JetBrains.Annotations;
using RoR2.Audio;
using RoR2.Networking;
using RoR2.Orbs;
using RoR2.PostProcessing;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterBody))]
public class HealthComponent : NetworkBehaviour, IManagedMonoBehaviour
{
	private static class AssetReferences
	{
		public static GameObject bearEffectPrefab;

		public static GameObject bearVoidEffectPrefab;

		public static GameObject executeEffectPrefab;

		public static GameObject critGlassesVoidExecuteEffectPrefab;

		public static GameObject shieldBreakEffectPrefab;

		public static GameObject loseCoinsImpactEffectPrefab;

		public static GameObject gainCoinsImpactEffectPrefab;

		public static GameObject damageRejectedPrefab;

		public static GameObject bossDamageBonusImpactEffectPrefab;

		public static GameObject pulverizedEffectPrefab;

		public static GameObject diamondDamageBonusImpactEffectPrefab;

		public static GameObject crowbarImpactEffectPrefab;

		public static GameObject captainBodyArmorBlockEffectPrefab;

		public static GameObject mercExposeConsumeEffectPrefab;

		public static GameObject explodeOnDeathVoidExplosionPrefab;

		public static GameObject fragileDamageBonusBreakEffectPrefab;

		public static GameObject healthOrbPrefab;

		public static GameObject permanentDebuffEffectPrefab;

		public static void Resolve()
		{
			AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/BearProc");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				bearEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/BearVoidProc");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				bearVoidEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/OmniEffect/OmniImpactExecute");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				executeEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ShieldBreakEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				shieldBreakEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/CoinImpact");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				loseCoinsImpactEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/GainCoinsImpact");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				gainCoinsImpactEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/DamageRejected");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				damageRejectedPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/ImpactBossDamageBonus");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				bossDamageBonusImpactEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/PulverizedEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				pulverizedEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/DiamondDamageBonusEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				diamondDamageBonusImpactEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/ImpactCrowbar");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				crowbarImpactEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/CaptainBodyArmorBlockEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				captainBodyArmorBlockEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/PermanentDebuffEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				permanentDebuffEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/MercExposeConsumeEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				mercExposeConsumeEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/CritGlassesVoidExecuteEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				critGlassesVoidExecuteEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/NetworkedObjects/ExplodeOnDeathVoidExplosion");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				explodeOnDeathVoidExplosionPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/DelicateWatchProcEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				fragileDamageBonusBreakEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/NetworkedObjects/TreebotFruitPack");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				healthOrbPrefab = x.Result;
			};
		}
	}

	private class HealMessage : MessageBase
	{
		public GameObject target;

		public float amount;

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write(target);
			writer.Write(amount);
		}

		public override void Deserialize(NetworkReader reader)
		{
			target = reader.ReadGameObject();
			amount = reader.ReadSingle();
		}
	}

	private struct ItemCounts
	{
		public int bear;

		public int armorPlate;

		public int goldOnHit;

		public int goldOnHurt;

		public int phasing;

		public int thorns;

		public int invadingDoppelganger;

		public int medkit;

		public int parentEgg;

		public int fragileDamageBonus;

		public int minHealthPercentage;

		public int noxiousThorn;

		public int antlerShield;

		public int increaseHealing;

		public int barrierOnOverHeal;

		public int repeatHeal;

		public int novaOnHeal;

		public int adaptiveArmor;

		public int healingPotion;

		public int infusion;

		public int missileVoid;

		public int unstableTransmitter;

		public ItemCounts([NotNull] Inventory src)
		{
			bear = src.GetItemCount(RoR2Content.Items.Bear);
			armorPlate = src.GetItemCount(RoR2Content.Items.ArmorPlate);
			goldOnHit = src.GetItemCount(RoR2Content.Items.GoldOnHit);
			goldOnHurt = src.GetItemCount(DLC1Content.Items.GoldOnHurt);
			phasing = src.GetItemCount(RoR2Content.Items.Phasing);
			thorns = src.GetItemCount(RoR2Content.Items.Thorns);
			invadingDoppelganger = src.GetItemCount(RoR2Content.Items.InvadingDoppelganger);
			medkit = src.GetItemCount(RoR2Content.Items.Medkit);
			fragileDamageBonus = src.GetItemCount(DLC1Content.Items.FragileDamageBonus);
			minHealthPercentage = src.GetItemCount(RoR2Content.Items.MinHealthPercentage);
			increaseHealing = src.GetItemCount(RoR2Content.Items.IncreaseHealing);
			barrierOnOverHeal = src.GetItemCount(RoR2Content.Items.BarrierOnOverHeal);
			repeatHeal = src.GetItemCount(RoR2Content.Items.RepeatHeal);
			novaOnHeal = src.GetItemCount(RoR2Content.Items.NovaOnHeal);
			adaptiveArmor = src.GetItemCount(RoR2Content.Items.AdaptiveArmor);
			healingPotion = src.GetItemCount(DLC1Content.Items.HealingPotion);
			infusion = src.GetItemCount(RoR2Content.Items.Infusion);
			parentEgg = src.GetItemCount(RoR2Content.Items.ParentEgg);
			missileVoid = src.GetItemCount(DLC1Content.Items.MissileVoid);
			unstableTransmitter = src.GetItemCount(DLC2Content.Items.TeleportOnLowHealth);
			noxiousThorn = src.GetItemCount(DLC2Content.Items.TriggerEnemyDebuffs);
			antlerShield = src.GetItemCount(DLC2Content.Items.NegateAttack);
		}
	}

	public struct HealthBarValues
	{
		public bool hasInfusion;

		public bool hasVoidShields;

		public bool isVoid;

		public bool isElite;

		public bool isBoss;

		public float cullFraction;

		public float healthFraction;

		public float shieldFraction;

		public float barrierFraction;

		public float magneticFraction;

		public float curseFraction;

		public float ospFraction;

		public int healthDisplayValue;

		public int maxHealthDisplayValue;
	}

	private class RepeatHealComponent : MonoBehaviour
	{
		private float reserve;

		private float timer;

		private const float interval = 0.2f;

		public float healthFractionToRestorePerSecond = 0.1f;

		public HealthComponent healthComponent;

		private void FixedUpdate()
		{
			timer -= Time.fixedDeltaTime;
			if (timer <= 0f)
			{
				timer = 0.2f;
				if (reserve > 0f)
				{
					float num = Mathf.Min(healthComponent.fullHealth * healthFractionToRestorePerSecond * 0.2f, reserve);
					reserve -= num;
					ProcChainMask procChainMask = default(ProcChainMask);
					procChainMask.AddProc(ProcType.RepeatHeal);
					healthComponent.Heal(num, procChainMask);
				}
			}
		}

		public void AddReserve(float amount, float max)
		{
			reserve = Mathf.Min(reserve + amount, max);
		}
	}

	public static readonly float lowHealthFraction;

	[HideInInspector]
	[SyncVar]
	[Tooltip("How much health this object has.")]
	public float health = 100f;

	[HideInInspector]
	[SyncVar]
	[Tooltip("How much shield this object has.")]
	public float shield;

	[HideInInspector]
	[SyncVar(hook = "SetBarrier")]
	[Tooltip("How much barrier this object has.")]
	public float barrier;

	[HideInInspector]
	[SyncVar]
	public float magnetiCharge;

	public bool dontShowHealthbar;

	private const float recentlyTookDamageCoyoteTimer_Duration = 0.2f;

	[NonSerialized]
	public float recentlyTookDamageCoyoteTimer;

	public float globalDeathEventChanceCoefficient = 1f;

	[SyncVar]
	private uint _killingDamageType;

	public CharacterBody body;

	private ModelLocator modelLocator;

	private IPainAnimationHandler painAnimationHandler;

	private IOnIncomingDamageServerReceiver[] onIncomingDamageReceivers;

	private IOnTakeDamageServerReceiver[] onTakeDamageReceivers;

	public ScreenDamageCalculator screenDamageCalculator;

	public const float frozenExecuteThreshold = 0.3f;

	private const float adaptiveArmorPerOnePercentTaken = 30f;

	private const float adaptiveArmorDecayPerSecond = 40f;

	private const float adaptiveArmorCap = 400f;

	public const float medkitActivationDelay = 2f;

	private const float devilOrbMaxTimer = 0.1f;

	private float devilOrbHealPool;

	private float devilOrbTimer;

	private float regenAccumulator;

	private bool wasAlive = true;

	private float adaptiveArmorValue;

	private bool isShieldRegenForced;

	public bool forceCulled;

	private float ospTimer;

	private const float ospBufferDuration = 0.1f;

	private float serverDamageTakenThisUpdate;

	private RepeatHealComponent repeatHealComponent;

	private ItemCounts itemCounts;

	private EquipmentIndex currentEquipmentIndex;

	private static int kCmdCmdHealFull;

	private static int kCmdCmdRechargeShieldFull;

	private static int kCmdCmdAddBarrier;

	private static int kCmdCmdForceShieldRegen;

	public bool recentlyTookDamage
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return recentlyTookDamageCoyoteTimer > 0f;
		}
	}

	public DamageTypeCombo killingDamageType
	{
		get
		{
			return _killingDamageType;
		}
		private set
		{
			Network_killingDamageType = (uint)(ulong)value;
		}
	}

	public bool alive => health > 0f;

	public float fullHealth => body.maxHealth;

	public float fullShield => body.maxShield;

	public float fullBarrier => body.maxBarrier;

	public float combinedHealth => health + shield + barrier;

	public float fullCombinedHealth => fullHealth + fullShield;

	public float combinedHealthFraction => combinedHealth / fullCombinedHealth;

	public float missingCombinedHealth => fullCombinedHealth - (combinedHealth - barrier);

	public Run.FixedTimeStamp lastHitTime { get; private set; }

	public Run.FixedTimeStamp lastHealTime { get; private set; }

	public GameObject lastHitAttacker { get; private set; }

	public float timeSinceLastHit => lastHitTime.timeSince;

	public float timeSinceLastHeal => lastHealTime.timeSince;

	public bool godMode { get; set; }

	public float potionReserve { get; private set; }

	public bool isInFrozenState { get; set; }

	public bool isHealthLow => (health + shield) / fullCombinedHealth <= lowHealthFraction;

	public float Networkhealth
	{
		get
		{
			return health;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref health, 1u);
		}
	}

	public float Networkshield
	{
		get
		{
			return shield;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref shield, 2u);
		}
	}

	public float Networkbarrier
	{
		get
		{
			return barrier;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetBarrier(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref barrier, 4u);
		}
	}

	public float NetworkmagnetiCharge
	{
		get
		{
			return magnetiCharge;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref magnetiCharge, 8u);
		}
	}

	public uint Network_killingDamageType
	{
		get
		{
			return _killingDamageType;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _killingDamageType, 16u);
		}
	}

	public static event Action<HealthComponent, float, ProcChainMask> onCharacterHealServer;

	private void SetBarrier(float newBarrier)
	{
		float num = barrier;
		Networkbarrier = newBarrier;
		if (barrier <= 0f && num > 0f)
		{
			body.MarkAllStatsDirty();
		}
	}

	public void OnValidate()
	{
		if (base.gameObject.GetComponents<HealthComponent>().Length > 1)
		{
			Debug.LogErrorFormat(base.gameObject, "{0} has multiple health components!!", base.gameObject);
		}
	}

	public float Heal(float amount, ProcChainMask procChainMask, bool nonRegen = true)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Single RoR2.HealthComponent::Heal(System.Single, RoR2.ProcChainMask, System.Boolean)' called on client");
			return 0f;
		}
		if (!alive || amount <= 0f || body.HasBuff(RoR2Content.Buffs.HealingDisabled))
		{
			return 0f;
		}
		recentlyTookDamageCoyoteTimer = 0.2f;
		float num = health;
		bool flag = false;
		if (currentEquipmentIndex == RoR2Content.Equipment.LunarPotion.equipmentIndex && !procChainMask.HasProc(ProcType.LunarPotionActivation))
		{
			potionReserve += amount;
			return amount;
		}
		if (nonRegen && !procChainMask.HasProc(ProcType.CritHeal) && Util.CheckRoll(body.critHeal, body.master))
		{
			procChainMask.AddProc(ProcType.CritHeal);
			flag = true;
		}
		if (flag)
		{
			amount *= 2f;
		}
		if (itemCounts.increaseHealing > 0)
		{
			amount *= 1f + (float)itemCounts.increaseHealing;
		}
		if (body.teamComponent.teamIndex == TeamIndex.Player && Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse5)
		{
			amount /= 2f;
		}
		if (nonRegen && (bool)repeatHealComponent && !procChainMask.HasProc(ProcType.RepeatHeal))
		{
			repeatHealComponent.healthFractionToRestorePerSecond = 0.1f / (float)itemCounts.repeatHeal;
			repeatHealComponent.AddReserve(amount * (float)(1 + itemCounts.repeatHeal), fullHealth);
			return 0f;
		}
		if (body.HasBuff(DLC2Content.Buffs.lunarruin))
		{
			amount *= 0.8f;
		}
		if (body.HasBuff(DLC2Content.Buffs.SojournHealing))
		{
			int buffCount = body.GetBuffCount(DLC2Content.Buffs.SojournHealing);
			float num2 = 1f - (float)buffCount * 0.1f;
			if (num2 < 0f)
			{
				num2 = 0f;
			}
			amount *= num2;
		}
		float num3 = amount;
		if (health < fullHealth)
		{
			float num4 = Mathf.Max(Mathf.Min(amount, fullHealth - health), 0f);
			num3 = amount - num4;
			Networkhealth = health + num4;
		}
		if (num3 > 0f && nonRegen && itemCounts.barrierOnOverHeal > 0)
		{
			float value = num3 * ((float)itemCounts.barrierOnOverHeal * 0.5f);
			AddBarrier(value);
		}
		if (nonRegen)
		{
			lastHealTime = Run.FixedTimeStamp.now;
			SendHeal(base.gameObject, amount, flag);
			if (itemCounts.novaOnHeal > 0 && !procChainMask.HasProc(ProcType.HealNova))
			{
				devilOrbHealPool = Mathf.Min(devilOrbHealPool + amount * (float)itemCounts.novaOnHeal, fullCombinedHealth);
			}
		}
		if (flag)
		{
			GlobalEventManager.instance.OnCrit(body, null, body.master, amount / fullHealth * 10f, procChainMask);
		}
		if (nonRegen)
		{
			HealthComponent.onCharacterHealServer?.Invoke(this, amount, procChainMask);
		}
		if ((bool)body.inventory && body.inventory.GetItemCount(DLC2Content.Items.LowerHealthHigherDamage) > 0 && alive)
		{
			body.UpdateLowerHealthHigherDamage();
		}
		return health - num;
	}

	public void UsePotion()
	{
		ProcChainMask procChainMask = default(ProcChainMask);
		procChainMask.AddProc(ProcType.LunarPotionActivation);
		Heal(potionReserve, procChainMask);
	}

	public float HealFraction(float fraction, ProcChainMask procChainMask)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Single RoR2.HealthComponent::HealFraction(System.Single, RoR2.ProcChainMask)' called on client");
			return 0f;
		}
		return Heal(fraction * fullHealth, procChainMask);
	}

	public void SetCurrentHealthLevel()
	{
	}

	public float GetNormalizedHealth()
	{
		return health / fullHealth;
	}

	[Command]
	public void CmdHealFull()
	{
		HealFraction(1f, default(ProcChainMask));
	}

	[Server]
	public void RechargeShieldFull()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealthComponent::RechargeShieldFull()' called on client");
		}
		else if (shield < fullShield)
		{
			Networkshield = fullShield;
		}
	}

	[Command]
	public void CmdRechargeShieldFull()
	{
		RechargeShieldFull();
	}

	[Server]
	public void RechargeShield(float value)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealthComponent::RechargeShield(System.Single)' called on client");
		}
		else if (shield < fullShield)
		{
			Networkshield = shield + value;
			if (shield > fullShield)
			{
				Networkshield = fullShield;
			}
		}
	}

	[Server]
	public void AddBarrier(float value)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealthComponent::AddBarrier(System.Single)' called on client");
		}
		else if (alive && barrier < fullBarrier)
		{
			Networkbarrier = Mathf.Min(barrier + value, fullBarrier);
		}
	}

	[Server]
	public void AddCharge(float value)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealthComponent::AddCharge(System.Single)' called on client");
		}
		else if (alive && magnetiCharge < fullHealth)
		{
			NetworkmagnetiCharge = Mathf.Min(barrier + value, fullBarrier);
		}
	}

	[Command]
	private void CmdAddBarrier(float value)
	{
		AddBarrier(value);
	}

	public void AddBarrierAuthority(float value)
	{
		if (NetworkServer.active)
		{
			AddBarrier(value);
		}
		else
		{
			CallCmdAddBarrier(value);
		}
	}

	[Command]
	private void CmdForceShieldRegen()
	{
		ForceShieldRegen();
	}

	public void ForceShieldRegen()
	{
		if (NetworkServer.active)
		{
			isShieldRegenForced = true;
		}
		else
		{
			CallCmdForceShieldRegen();
		}
	}

	[Server]
	public void Die(bool noCorpse = false)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealthComponent::Die(System.Boolean)' called on client");
			return;
		}
		Networkhealth = 0f;
		modelLocator.forceCulled = noCorpse;
		if ((bool)body && body.cost > 0f && CombatDirector.instancesList.Count > 0)
		{
			CombatDirector combatDirector = CombatDirector.instancesList[0];
			if (body.isElite || CombatDirector.IsEliteOnlyArtifactActive())
			{
				combatDirector.monsterCredit += body.cost;
			}
			else
			{
				combatDirector.refundedMonsterCredit += body.cost;
			}
			Debug.LogError("Refunded CombatDirector " + body.cost + " credits");
		}
	}

	[Server]
	public void TakeDamageForce(DamageInfo damageInfo, bool alwaysApply = false, bool disableAirControlUntilCollision = false)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealthComponent::TakeDamageForce(RoR2.DamageInfo,System.Boolean,System.Boolean)' called on client");
		}
		else if (!body.HasBuff(RoR2Content.Buffs.EngiShield) || !(shield > 0f))
		{
			CharacterMotor component = GetComponent<CharacterMotor>();
			if ((bool)component)
			{
				component.ApplyForce(damageInfo.force, alwaysApply, disableAirControlUntilCollision);
			}
			Rigidbody component2 = GetComponent<Rigidbody>();
			if ((bool)component2)
			{
				component2.AddForce(damageInfo.force, ForceMode.Impulse);
			}
		}
	}

	[Server]
	public void TakeDamageForce(Vector3 force, bool alwaysApply = false, bool disableAirControlUntilCollision = false)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealthComponent::TakeDamageForce(UnityEngine.Vector3,System.Boolean,System.Boolean)' called on client");
		}
		else if (!body.HasBuff(RoR2Content.Buffs.EngiShield) || !(shield > 0f))
		{
			CharacterMotor component = GetComponent<CharacterMotor>();
			if ((bool)component)
			{
				component.ApplyForce(force, alwaysApply, disableAirControlUntilCollision);
			}
			Rigidbody component2 = GetComponent<Rigidbody>();
			if ((bool)component2)
			{
				component2.AddForce(force, ForceMode.Impulse);
			}
		}
	}

	[Server]
	public void TakeDamage(DamageInfo damageInfo)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealthComponent::TakeDamage(RoR2.DamageInfo)' called on client");
		}
		else
		{
			TakeDamageProcess(damageInfo);
		}
	}

	private void TakeDamageProcess(DamageInfo damageInfo)
	{
		if (body.HasBuff(DLC2Content.Buffs.HiddenRejectAllDamage))
		{
			return;
		}
		if (!damageInfo.canRejectForce)
		{
			TakeDamageForce(damageInfo);
		}
		if (!alive || godMode || ospTimer > 0f)
		{
			return;
		}
		CharacterMaster characterMaster = null;
		CharacterBody characterBody = null;
		TeamIndex teamIndex = TeamIndex.None;
		Vector3 vector = Vector3.zero;
		float num = combinedHealth;
		if ((bool)damageInfo.attacker)
		{
			characterBody = damageInfo.attacker.GetComponent<CharacterBody>();
			if ((bool)characterBody)
			{
				teamIndex = characterBody.teamComponent.teamIndex;
				vector = characterBody.corePosition - damageInfo.position;
			}
		}
		if ((DamageTypeExtended)(damageInfo.damageType & DamageTypeExtended.DamagePercentOfMaxHealth) == DamageTypeExtended.DamagePercentOfMaxHealth)
		{
			damageInfo.damage = fullHealth * 0.1f;
		}
		bool flag = (ulong)(damageInfo.damageType & DamageType.BypassArmor) != 0;
		bool flag2 = (ulong)(damageInfo.damageType & DamageType.BypassBlock) != 0;
		if (!flag2 && itemCounts.bear > 0 && Util.CheckRoll(Util.ConvertAmplificationPercentageIntoReductionPercentage(15f * (float)itemCounts.bear)))
		{
			EffectData effectData = new EffectData
			{
				origin = damageInfo.position,
				rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : UnityEngine.Random.onUnitSphere)
			};
			EffectManager.SpawnEffect(AssetReferences.bearEffectPrefab, effectData, transmit: true);
			damageInfo.rejected = true;
		}
		if (!flag2 && body.HasBuff(DLC1Content.Buffs.BearVoidReady) && damageInfo.damage > 0f)
		{
			EffectData effectData2 = new EffectData
			{
				origin = damageInfo.position,
				rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : UnityEngine.Random.onUnitSphere)
			};
			EffectManager.SpawnEffect(AssetReferences.bearVoidEffectPrefab, effectData2, transmit: true);
			damageInfo.rejected = true;
			body.RemoveBuff(DLC1Content.Buffs.BearVoidReady);
			int itemCount = body.inventory.GetItemCount(DLC1Content.Items.BearVoid);
			Mathf.CeilToInt(15f * Mathf.Pow(0.9f, itemCount));
			body.AddTimedBuff(DLC1Content.Buffs.BearVoidCooldown, 15f * Mathf.Pow(0.9f, itemCount));
		}
		if (body.HasBuff(RoR2Content.Buffs.HiddenInvincibility) && !flag)
		{
			damageInfo.rejected = true;
		}
		if (((ulong)(damageInfo.damageType & DamageTypeExtended.SojournVehicleDamage) == 0) & body.HasBuff(DLC2Content.Buffs.SojournVehicle))
		{
			damageInfo.rejected = true;
		}
		if (body.HasBuff(RoR2Content.Buffs.Immune) && (!characterBody || !characterBody.HasBuff(JunkContent.Buffs.GoldEmpowered)))
		{
			EffectManager.SpawnEffect(AssetReferences.damageRejectedPrefab, new EffectData
			{
				origin = damageInfo.position
			}, transmit: true);
			damageInfo.rejected = true;
		}
		if (!damageInfo.rejected && body.HasBuff(JunkContent.Buffs.BodyArmor))
		{
			body.RemoveBuff(JunkContent.Buffs.BodyArmor);
			EffectData effectData3 = new EffectData
			{
				origin = damageInfo.position,
				rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : UnityEngine.Random.onUnitSphere)
			};
			EffectManager.SpawnEffect(AssetReferences.captainBodyArmorBlockEffectPrefab, effectData3, transmit: true);
			damageInfo.rejected = true;
		}
		IOnIncomingDamageServerReceiver[] array = onIncomingDamageReceivers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnIncomingDamageServer(damageInfo);
		}
		if (damageInfo.rejected)
		{
			return;
		}
		if (body.HasBuff(DLC2Content.Buffs.lunarruin))
		{
			float num2 = (float)body.GetBuffCount(DLC2Content.Buffs.lunarruin) * 0.1f;
			float num3 = damageInfo.damage * num2;
			damageInfo.damage += num3;
		}
		float num4 = damageInfo.damage;
		if (teamIndex == body.teamComponent.teamIndex)
		{
			TeamDef teamDef = TeamCatalog.GetTeamDef(teamIndex);
			if (teamDef != null)
			{
				num4 *= teamDef.friendlyFireScaling;
			}
		}
		if (num4 > 0f)
		{
			if ((bool)characterBody)
			{
				if (characterBody.canPerformBackstab && (DamageType)(damageInfo.damageType & DamageType.DoT) != DamageType.DoT && (damageInfo.procChainMask.HasProc(ProcType.Backstab) || BackstabManager.IsBackstab(-vector, body)))
				{
					damageInfo.crit = true;
					damageInfo.procChainMask.AddProc(ProcType.Backstab);
					if ((bool)BackstabManager.backstabImpactEffectPrefab)
					{
						EffectManager.SimpleImpactEffect(BackstabManager.backstabImpactEffectPrefab, damageInfo.position, -damageInfo.force, transmit: true);
					}
				}
				characterMaster = characterBody.master;
				if ((bool)characterMaster && (bool)characterMaster.inventory)
				{
					if (num >= fullCombinedHealth * 0.9f)
					{
						int itemCount2 = characterMaster.inventory.GetItemCount(RoR2Content.Items.Crowbar);
						if (itemCount2 > 0)
						{
							num4 *= 1f + 0.75f * (float)itemCount2;
							EffectManager.SimpleImpactEffect(AssetReferences.crowbarImpactEffectPrefab, damageInfo.position, -damageInfo.force, transmit: true);
						}
					}
					if (num >= fullCombinedHealth && !damageInfo.rejected)
					{
						int itemCount3 = characterMaster.inventory.GetItemCount(DLC1Content.Items.ExplodeOnDeathVoid);
						if (itemCount3 > 0)
						{
							Vector3 corePosition = Util.GetCorePosition(body);
							float damageCoefficient = 2.6f * (1f + (float)(itemCount3 - 1) * 0.6f);
							float baseDamage = Util.OnKillProcDamage(characterBody.damage, damageCoefficient);
							GameObject obj = UnityEngine.Object.Instantiate(AssetReferences.explodeOnDeathVoidExplosionPrefab, corePosition, Quaternion.identity);
							DelayBlast component = obj.GetComponent<DelayBlast>();
							component.position = corePosition;
							component.baseDamage = baseDamage;
							component.baseForce = 1000f;
							component.radius = 12f + 2.4f * ((float)itemCount3 - 1f);
							component.attacker = damageInfo.attacker;
							component.inflictor = null;
							component.crit = Util.CheckRoll(characterBody.crit, characterMaster);
							component.maxTimer = 0.2f;
							component.damageColorIndex = DamageColorIndex.Void;
							component.falloffModel = BlastAttack.FalloffModel.SweetSpot;
							obj.GetComponent<TeamFilter>().teamIndex = teamIndex;
							NetworkServer.Spawn(obj);
						}
					}
					int itemCount4 = characterMaster.inventory.GetItemCount(RoR2Content.Items.NearbyDamageBonus);
					if (itemCount4 > 0 && vector.sqrMagnitude <= 169f)
					{
						damageInfo.damageColorIndex = DamageColorIndex.Nearby;
						num4 *= 1f + (float)itemCount4 * 0.2f;
						EffectManager.SimpleImpactEffect(AssetReferences.diamondDamageBonusImpactEffectPrefab, damageInfo.position, vector, transmit: true);
					}
					int itemCount5 = characterMaster.inventory.GetItemCount(DLC1Content.Items.FragileDamageBonus);
					if (itemCount5 > 0)
					{
						num4 *= 1f + (float)itemCount5 * 0.2f;
					}
					if (characterMaster.GetBody().HasBuff(DLC2Content.Buffs.LowerHealthHigherDamageBuff))
					{
						int itemCount6 = characterMaster.inventory.GetItemCount(DLC2Content.Items.LowerHealthHigherDamage);
						num4 *= 1f + (float)itemCount6 * 0.2f;
					}
					if (damageInfo.procCoefficient > 0f)
					{
						int itemCount7 = characterMaster.inventory.GetItemCount(RoR2Content.Items.ArmorReductionOnHit);
						if (itemCount7 > 0 && !body.HasBuff(RoR2Content.Buffs.Pulverized))
						{
							body.AddTimedBuff(RoR2Content.Buffs.PulverizeBuildup, 2f * damageInfo.procCoefficient);
							if (body.GetBuffCount(RoR2Content.Buffs.PulverizeBuildup) >= 5)
							{
								body.ClearTimedBuffs(RoR2Content.Buffs.PulverizeBuildup);
								body.AddTimedBuff(RoR2Content.Buffs.Pulverized, 8f * (float)itemCount7);
								EffectManager.SpawnEffect(AssetReferences.pulverizedEffectPrefab, new EffectData
								{
									origin = body.corePosition,
									scale = body.radius
								}, transmit: true);
							}
						}
						int itemCount8 = characterMaster.inventory.GetItemCount(DLC1Content.Items.PermanentDebuffOnHit);
						bool flag3 = false;
						for (int j = 0; j < itemCount8; j++)
						{
							if (Util.CheckRoll(100f * damageInfo.procCoefficient, characterMaster))
							{
								body.AddBuff(DLC1Content.Buffs.PermanentDebuff);
								flag3 = true;
							}
						}
						if (flag3)
						{
							EffectManager.SpawnEffect(AssetReferences.permanentDebuffEffectPrefab, new EffectData
							{
								origin = damageInfo.position,
								scale = itemCount8
							}, transmit: true);
						}
						if (body.HasBuff(RoR2Content.Buffs.MercExpose) && (bool)characterBody && characterBody.bodyIndex == BodyCatalog.FindBodyIndex("MercBody"))
						{
							body.RemoveBuff(RoR2Content.Buffs.MercExpose);
							float num5 = characterBody.damage * 3.5f;
							num4 += num5;
							damageInfo.damage += num5;
							SkillLocator skillLocator = characterBody.skillLocator;
							if ((bool)skillLocator)
							{
								skillLocator.DeductCooldownFromAllSkillsServer(1f);
							}
							EffectManager.SimpleImpactEffect(AssetReferences.mercExposeConsumeEffectPrefab, damageInfo.position, Vector3.up, transmit: true);
						}
					}
					if (body.isBoss)
					{
						int itemCount9 = characterMaster.inventory.GetItemCount(RoR2Content.Items.BossDamageBonus);
						if (itemCount9 > 0)
						{
							num4 *= 1f + 0.2f * (float)itemCount9;
							damageInfo.damageColorIndex = DamageColorIndex.WeakPoint;
							EffectManager.SimpleImpactEffect(AssetReferences.bossDamageBonusImpactEffectPrefab, damageInfo.position, -damageInfo.force, transmit: true);
						}
					}
				}
				if (damageInfo.crit)
				{
					num4 *= characterBody.critMultiplier;
				}
			}
			if ((ulong)(damageInfo.damageType & DamageType.WeakPointHit) != 0L)
			{
				num4 *= 1.5f;
				damageInfo.damageColorIndex = DamageColorIndex.WeakPoint;
			}
			if (body.HasBuff(RoR2Content.Buffs.DeathMark))
			{
				num4 *= 1.5f;
				damageInfo.damageColorIndex = DamageColorIndex.DeathMark;
			}
			if (!flag)
			{
				float armor = body.armor;
				armor += adaptiveArmorValue;
				bool flag4 = (ulong)(damageInfo.damageType & DamageType.AOE) != 0;
				if ((body.bodyFlags & CharacterBody.BodyFlags.ResistantToAOE) != 0 && flag4)
				{
					armor += 300f;
				}
				float num6 = ((armor >= 0f) ? (1f - armor / (armor + 100f)) : (2f - 100f / (100f - armor)));
				num4 = Mathf.Max(1f, num4 * num6);
				if (itemCounts.armorPlate > 0)
				{
					num4 = Mathf.Max(1f, num4 - 5f * (float)itemCounts.armorPlate);
					EntitySoundManager.EmitSoundServer(LegacyResourcesAPI.Load<NetworkSoundEventDef>("NetworkSoundEventDefs/nseArmorPlateBlock").index, base.gameObject);
				}
				if (itemCounts.parentEgg > 0)
				{
					Heal((float)itemCounts.parentEgg * 15f, default(ProcChainMask));
					EntitySoundManager.EmitSoundServer(LegacyResourcesAPI.Load<NetworkSoundEventDef>("NetworkSoundEventDefs/nseParentEggHeal").index, base.gameObject);
				}
			}
			if (body.hasOneShotProtection && (DamageType)(damageInfo.damageType & DamageType.BypassOneShotProtection) != DamageType.BypassOneShotProtection)
			{
				float num7 = (fullCombinedHealth + barrier) * (1f - body.oneShotProtectionFraction);
				float b = Mathf.Max(0f, num7 - serverDamageTakenThisUpdate);
				float num8 = num4;
				num4 = Mathf.Min(num4, b);
				if (num4 != num8)
				{
					TriggerOneShotProtection();
				}
			}
			if ((ulong)(damageInfo.damageType & DamageType.BonusToLowHealth) != 0)
			{
				float num9 = Mathf.Lerp(3f, 1f, combinedHealthFraction);
				num4 *= num9;
			}
			if (body.HasBuff(RoR2Content.Buffs.LunarShell) && num4 > fullHealth * 0.1f)
			{
				num4 = fullHealth * 0.1f;
			}
			if (itemCounts.minHealthPercentage > 0)
			{
				float num10 = fullCombinedHealth * ((float)itemCounts.minHealthPercentage / 100f);
				num4 = Mathf.Max(0f, Mathf.Min(num4, combinedHealth - num10));
			}
		}
		if ((ulong)(damageInfo.damageType & DamageType.SlowOnHit) != 0L)
		{
			body.AddTimedBuff(RoR2Content.Buffs.Slow50, 2f);
		}
		if ((ulong)(damageInfo.damageType & DamageType.ClayGoo) != 0L && (body.bodyFlags & CharacterBody.BodyFlags.ImmuneToGoo) == 0)
		{
			body.AddTimedBuff(RoR2Content.Buffs.ClayGoo, 2f);
		}
		if ((ulong)(damageInfo.damageType & DamageType.Nullify) != 0L)
		{
			body.AddTimedBuff(RoR2Content.Buffs.NullifyStack, 8f);
		}
		if ((ulong)(damageInfo.damageType & DamageType.CrippleOnHit) != 0L || ((bool)characterBody && characterBody.HasBuff(RoR2Content.Buffs.AffixLunar)))
		{
			body.AddTimedBuff(RoR2Content.Buffs.Cripple, 3f);
		}
		if ((ulong)(damageInfo.damageType & DamageType.ApplyMercExpose) != 0L)
		{
			Debug.LogFormat("Adding expose");
			body.AddBuff(RoR2Content.Buffs.MercExpose);
		}
		CharacterMaster master = body.master;
		if ((bool)master)
		{
			if (itemCounts.goldOnHit > 0)
			{
				uint num11 = (uint)(num4 / fullCombinedHealth * (float)master.money * (float)itemCounts.goldOnHit);
				uint money = master.money;
				master.money = (uint)Mathf.Max(0f, (float)master.money - (float)num11);
				if (money - master.money != 0)
				{
					GoldOrb goldOrb = new GoldOrb();
					goldOrb.origin = damageInfo.position;
					goldOrb.target = (characterBody ? characterBody.mainHurtBox : body.mainHurtBox);
					goldOrb.goldAmount = 0u;
					OrbManager.instance.AddOrb(goldOrb);
					EffectManager.SimpleImpactEffect(AssetReferences.loseCoinsImpactEffectPrefab, damageInfo.position, Vector3.up, transmit: true);
				}
			}
			if (itemCounts.goldOnHurt > 0 && characterBody != body && characterBody != null)
			{
				int num12 = 3;
				GoldOrb goldOrb2 = new GoldOrb();
				goldOrb2.origin = damageInfo.position;
				goldOrb2.target = body.mainHurtBox;
				goldOrb2.goldAmount = (uint)((float)(itemCounts.goldOnHurt * num12) * Run.instance.difficultyCoefficient);
				OrbManager.instance.AddOrb(goldOrb2);
				EffectManager.SimpleImpactEffect(AssetReferences.gainCoinsImpactEffectPrefab, damageInfo.position, Vector3.up, transmit: true);
			}
		}
		if (itemCounts.adaptiveArmor > 0)
		{
			float num13 = num4 / fullCombinedHealth * 100f * 30f * (float)itemCounts.adaptiveArmor;
			adaptiveArmorValue = Mathf.Min(adaptiveArmorValue + num13, 400f);
		}
		float num14 = num4;
		if (num14 > 0f)
		{
			isShieldRegenForced = false;
		}
		if (body.teamComponent.teamIndex == TeamIndex.Player && Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse8)
		{
			float num15 = num14 / fullCombinedHealth * 100f;
			float num16 = 0.4f;
			int num17 = Mathf.FloorToInt(num15 * num16);
			for (int k = 0; k < num17; k++)
			{
				body.AddBuff(RoR2Content.Buffs.PermanentCurse);
			}
		}
		if (num14 > 0f && barrier > 0f)
		{
			if (num14 <= barrier)
			{
				Networkbarrier = barrier - num14;
				num14 = 0f;
			}
			else
			{
				num14 -= barrier;
				Networkbarrier = 0f;
			}
		}
		if (num14 > 0f && shield > 0f)
		{
			if (num14 <= shield)
			{
				Networkshield = shield - num14;
				num14 = 0f;
			}
			else
			{
				num14 -= shield;
				Networkshield = 0f;
				float scale = 1f;
				if ((bool)body)
				{
					scale = body.radius;
				}
				EffectManager.SpawnEffect(AssetReferences.shieldBreakEffectPrefab, new EffectData
				{
					origin = base.transform.position,
					scale = scale
				}, transmit: true);
			}
		}
		if (!flag2 && body.HasBuff(DLC2Content.Buffs.DelayedDamageBuff) && damageInfo.damage > 0f && !damageInfo.delayedDamageSecondHalf && !damageInfo.rejected)
		{
			Transform transform = (body.mainHurtBox ? body.mainHurtBox.transform : base.transform);
			UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/DelayedDamageIndicator"), transform.position, Quaternion.identity, transform);
			Util.PlaySound("Play_item_proc_delayedDamage", body.gameObject);
			num14 *= 0.5f;
			num4 = num14;
			DamageInfo damageInfo2 = new DamageInfo
			{
				crit = damageInfo.crit,
				damage = num14,
				damageType = DamageType.BypassArmor,
				attacker = damageInfo.attacker,
				position = damageInfo.position,
				inflictor = damageInfo.inflictor,
				damageColorIndex = damageInfo.damageColorIndex,
				delayedDamageSecondHalf = true
			};
			damageInfo2.damageType |= damageInfo.damageType & DamageType.NonLethal;
			body.SecondHalfOfDelayedDamage(damageInfo2);
		}
		bool flag5 = (ulong)(damageInfo.damageType & DamageType.VoidDeath) != 0L && (body.bodyFlags & CharacterBody.BodyFlags.ImmuneToVoidDeath) == 0;
		float executionHealthLost = 0f;
		GameObject gameObject = null;
		if (num14 > 0f)
		{
			float num18 = health - num14;
			if (num18 < 1f && (ulong)(damageInfo.damageType & DamageType.NonLethal) != 0L && health >= 1f)
			{
				num18 = 1f;
			}
			Networkhealth = num18;
			recentlyTookDamageCoyoteTimer = 0.2f;
		}
		if (health > 0f && body.teamComponent.teamIndex == TeamIndex.Player && (bool)body.inventory && body.inventory.GetItemCount(DLC2Content.Items.LowerHealthHigherDamage) > 0)
		{
			body.UpdateLowerHealthHigherDamage();
		}
		float num19 = float.NegativeInfinity;
		bool flag6 = (body.bodyFlags & CharacterBody.BodyFlags.ImmuneToExecutes) != 0;
		if (!flag5 && !flag6)
		{
			if (isInFrozenState && num19 < 0.3f)
			{
				num19 = 0.3f;
				gameObject = FrozenState.executeEffectPrefab;
			}
			if ((bool)characterBody)
			{
				if (body.isElite)
				{
					float executeEliteHealthFraction = characterBody.executeEliteHealthFraction;
					if (num19 < executeEliteHealthFraction)
					{
						num19 = executeEliteHealthFraction;
						gameObject = AssetReferences.executeEffectPrefab;
					}
				}
				if (!body.isBoss && (bool)characterBody.inventory && Util.CheckRoll((float)characterBody.inventory.GetItemCount(DLC1Content.Items.CritGlassesVoid) * 0.5f * damageInfo.procCoefficient, characterBody.master))
				{
					flag5 = true;
					gameObject = AssetReferences.critGlassesVoidExecuteEffectPrefab;
					damageInfo.damageType |= (DamageTypeCombo)DamageType.VoidDeath;
				}
			}
		}
		if (flag5 || (num19 > 0f && combinedHealthFraction <= num19))
		{
			flag5 = true;
			executionHealthLost = Mathf.Max(combinedHealth, 0f);
			if (health > 0f)
			{
				Networkhealth = 0f;
			}
			if (shield > 0f)
			{
				Networkshield = 0f;
			}
			if (barrier > 0f)
			{
				Networkbarrier = 0f;
			}
		}
		if (damageInfo.canRejectForce)
		{
			TakeDamageForce(damageInfo);
		}
		DamageReport damageReport = new DamageReport(damageInfo, this, num4, num);
		IOnTakeDamageServerReceiver[] array2 = onTakeDamageReceivers;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].OnTakeDamageServer(damageReport);
		}
		if (num4 > 0f)
		{
			SendDamageDealt(damageReport);
		}
		UpdateLastHitTime(damageReport.damageDealt, damageInfo.position, (ulong)(damageInfo.damageType & DamageType.Silent) != 0, damageInfo.attacker);
		if ((bool)damageInfo.attacker)
		{
			List<IOnDamageDealtServerReceiver> gameObjectComponents = GetComponentsCache<IOnDamageDealtServerReceiver>.GetGameObjectComponents(damageInfo.attacker);
			foreach (IOnDamageDealtServerReceiver item in gameObjectComponents)
			{
				item.OnDamageDealtServer(damageReport);
			}
			GetComponentsCache<IOnDamageDealtServerReceiver>.ReturnBuffer(gameObjectComponents);
		}
		if ((bool)damageInfo.inflictor)
		{
			List<IOnDamageInflictedServerReceiver> gameObjectComponents2 = GetComponentsCache<IOnDamageInflictedServerReceiver>.GetGameObjectComponents(damageInfo.inflictor);
			foreach (IOnDamageInflictedServerReceiver item2 in gameObjectComponents2)
			{
				item2.OnDamageInflictedServer(damageReport);
			}
			GetComponentsCache<IOnDamageInflictedServerReceiver>.ReturnBuffer(gameObjectComponents2);
		}
		GlobalEventManager.ServerDamageDealt(damageReport);
		if (!alive)
		{
			killingDamageType = damageInfo.damageType;
			if (flag5)
			{
				GlobalEventManager.ServerCharacterExecuted(damageReport, executionHealthLost);
				if ((object)gameObject != null)
				{
					EffectManager.SpawnEffect(gameObject, new EffectData
					{
						origin = body.corePosition,
						scale = (body ? body.radius : 1f)
					}, transmit: true);
				}
			}
			IOnKilledServerReceiver[] components = GetComponents<IOnKilledServerReceiver>();
			for (int i = 0; i < components.Length; i++)
			{
				components[i].OnKilledServer(damageReport);
			}
			if ((bool)damageInfo.attacker)
			{
				IOnKilledOtherServerReceiver[] components2 = damageInfo.attacker.GetComponents<IOnKilledOtherServerReceiver>();
				for (int i = 0; i < components2.Length; i++)
				{
					components2[i].OnKilledOtherServer(damageReport);
				}
			}
			if (Util.CheckRoll(globalDeathEventChanceCoefficient * 100f))
			{
				GlobalEventManager.instance.OnCharacterDeath(damageReport);
			}
		}
		else
		{
			if (!(num4 > 0f))
			{
				return;
			}
			int a = 5 + 2 * (itemCounts.thorns - 1);
			if (itemCounts.thorns > 0 && !damageReport.damageInfo.procChainMask.HasProc(ProcType.Thorns))
			{
				bool flag7 = itemCounts.invadingDoppelganger > 0;
				float radius = 25f + 10f * (float)(itemCounts.thorns - 1);
				bool isCrit = body.RollCrit();
				float damageValue = 1.6f * body.damage;
				TeamIndex teamIndex2 = body.teamComponent.teamIndex;
				HurtBox[] hurtBoxes = new SphereSearch
				{
					origin = damageReport.damageInfo.position,
					radius = radius,
					mask = LayerIndex.entityPrecise.mask,
					queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
				}.RefreshCandidates().FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(teamIndex2)).OrderCandidatesByDistance()
					.FilterCandidatesByDistinctHurtBoxEntities()
					.GetHurtBoxes();
				for (int l = 0; l < Mathf.Min(a, hurtBoxes.Length); l++)
				{
					LightningOrb lightningOrb = new LightningOrb();
					lightningOrb.attacker = base.gameObject;
					lightningOrb.bouncedObjects = null;
					lightningOrb.bouncesRemaining = 0;
					lightningOrb.damageCoefficientPerBounce = 1f;
					lightningOrb.damageColorIndex = DamageColorIndex.Item;
					lightningOrb.damageValue = damageValue;
					lightningOrb.isCrit = isCrit;
					lightningOrb.lightningType = LightningOrb.LightningType.RazorWire;
					lightningOrb.origin = damageReport.damageInfo.position;
					lightningOrb.procChainMask = default(ProcChainMask);
					lightningOrb.procChainMask.AddProc(ProcType.Thorns);
					lightningOrb.procCoefficient = (flag7 ? 0f : 0.5f);
					lightningOrb.range = 0f;
					lightningOrb.teamIndex = teamIndex2;
					lightningOrb.target = hurtBoxes[l];
					OrbManager.instance.AddOrb(lightningOrb);
				}
			}
			if (!flag2 && itemCounts.antlerShield > 0 && characterBody != null && !damageInfo.delayedDamageSecondHalf && damageInfo.attacker.GetComponent<TeamComponent>().teamIndex != body.teamComponent.teamIndex && Util.CheckRoll(50f + 5f * (float)(itemCounts.antlerShield - 1)))
			{
				bool flag8 = itemCounts.invadingDoppelganger > 0;
				float damageValue2 = damageInfo.damage / 10f * (float)itemCounts.antlerShield;
				bool isCrit2 = body.RollCrit();
				TeamIndex teamIndex3 = body.teamComponent.teamIndex;
				LightningOrb lightningOrb2 = new LightningOrb();
				lightningOrb2.attacker = base.gameObject;
				lightningOrb2.bouncedObjects = null;
				lightningOrb2.bouncesRemaining = 0;
				lightningOrb2.damageCoefficientPerBounce = 1f;
				lightningOrb2.damageColorIndex = DamageColorIndex.Item;
				lightningOrb2.damageValue = damageValue2;
				lightningOrb2.isCrit = isCrit2;
				lightningOrb2.lightningType = LightningOrb.LightningType.AntlerShield;
				lightningOrb2.origin = damageReport.damageInfo.position;
				lightningOrb2.procChainMask = default(ProcChainMask);
				lightningOrb2.procChainMask.AddProc(ProcType.Thorns);
				lightningOrb2.procCoefficient = (flag8 ? 0f : 0.3f);
				lightningOrb2.range = 0f;
				lightningOrb2.teamIndex = teamIndex3;
				lightningOrb2.target = characterBody.mainHurtBox;
				OrbManager.instance.AddOrb(lightningOrb2);
				Util.PlaySound("Play_item_proc_negateAttack", body.gameObject);
			}
			if (!flag2 && itemCounts.noxiousThorn > 0 && Util.CheckRoll(25f))
			{
				body.TriggerEnemyDebuffs(damageInfo);
			}
			if (!body.HasBuff(DLC2Content.Buffs.AurelioniteBlessing))
			{
				return;
			}
			int baseCost = 1;
			int difficultyScaledCost = Run.instance.GetDifficultyScaledCost(baseCost);
			uint money2 = characterMaster.money;
			characterMaster.money = (uint)Mathf.Max(0f, (float)characterMaster.money - (float)difficultyScaledCost);
			if (money2 - characterMaster.money != 0 || !characterBody.isPlayerControlled)
			{
				GoldOrb goldOrb3 = new GoldOrb();
				goldOrb3.origin = characterBody.transform.position;
				goldOrb3.target = (body ? body.mainHurtBox : characterBody.mainHurtBox);
				goldOrb3.goldAmount = (uint)difficultyScaledCost;
				OrbManager.instance.AddOrb(goldOrb3);
				EffectManager.SimpleImpactEffect(AssetReferences.loseCoinsImpactEffectPrefab, damageInfo.position, Vector3.up, transmit: true);
				if (!characterBody.isPlayerControlled)
				{
					characterMaster.money = money2;
				}
			}
		}
	}

	[Server]
	private void TriggerOneShotProtection()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealthComponent::TriggerOneShotProtection()' called on client");
			return;
		}
		ospTimer = 0.1f;
		if (body.GetBuffCount(DLC2Content.Buffs.DelayedDamageDebuff) > 0)
		{
			body.protectFromOneShot = true;
		}
	}

	[Server]
	public void Suicide(GameObject killerOverride = null, GameObject inflictorOverride = null, DamageTypeCombo damageType = default(DamageTypeCombo))
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealthComponent::Suicide(UnityEngine.GameObject,UnityEngine.GameObject,RoR2.DamageTypeCombo)' called on client");
		}
		else if (alive && !godMode)
		{
			float combinedHealthBeforeDamage = combinedHealth;
			DamageInfo damageInfo = new DamageInfo();
			damageInfo.damage = combinedHealth;
			damageInfo.position = base.transform.position;
			damageInfo.damageType = damageType;
			damageInfo.procCoefficient = 1f;
			if ((bool)killerOverride)
			{
				damageInfo.attacker = killerOverride;
			}
			if ((bool)inflictorOverride)
			{
				damageInfo.inflictor = inflictorOverride;
			}
			Networkhealth = 0f;
			DamageReport damageReport = new DamageReport(damageInfo, this, damageInfo.damage, combinedHealthBeforeDamage);
			killingDamageType = damageInfo.damageType;
			IOnKilledServerReceiver[] components = GetComponents<IOnKilledServerReceiver>();
			for (int i = 0; i < components.Length; i++)
			{
				components[i].OnKilledServer(damageReport);
			}
			GlobalEventManager.instance.OnCharacterDeath(damageReport);
		}
	}

	public void UpdateLastHitTime(float damageValue, Vector3 damagePosition, bool damageIsSilent, GameObject attacker)
	{
		if (NetworkServer.active && (bool)body && damageValue > 0f)
		{
			if (itemCounts.medkit > 0)
			{
				body.AddTimedBuff(RoR2Content.Buffs.MedkitHeal, 2f);
			}
			if (itemCounts.healingPotion > 0 && isHealthLow)
			{
				body.inventory.RemoveItem(DLC1Content.Items.HealingPotion);
				body.inventory.GiveItem(DLC1Content.Items.HealingPotionConsumed);
				CharacterMasterNotificationQueue.SendTransformNotification(body.master, DLC1Content.Items.HealingPotion.itemIndex, DLC1Content.Items.HealingPotionConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
				HealFraction(0.75f, default(ProcChainMask));
				EffectData effectData = new EffectData
				{
					origin = base.transform.position
				};
				effectData.SetNetworkedObjectReference(base.gameObject);
				EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/HealingPotionEffect"), effectData, transmit: true);
			}
			if (body.GetBuffCount(DLC2Content.Buffs.RevitalizeBuff) > 0 && isHealthLow)
			{
				body.RemoveBuff(DLC2Content.Buffs.RevitalizeBuff);
				HealFraction(0.5f, default(ProcChainMask));
				EffectData effectData2 = new EffectData
				{
					origin = base.transform.position
				};
				effectData2.SetNetworkedObjectReference(base.gameObject);
				EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Seeker/RevitalizeBuffVFXConsumed"), effectData2, transmit: true);
			}
			float num = 120f;
			if (isHealthLow && body.HasBuff(DLC2Content.Buffs.TeleportOnLowHealth) && itemCounts.unstableTransmitter > 0)
			{
				body.RemoveBuff(DLC2Content.Buffs.TeleportOnLowHealth);
				float duration = Mathf.Max(num - num * ((float)(itemCounts.unstableTransmitter - 1) * 0.2f), 15f);
				body.AddTimedBuff(DLC2Content.Buffs.TeleportOnLowHealthCooldown, duration);
				body.hasTeleported = true;
				body.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 2f);
				body.CallRpcTeleportCharacterToSafety();
			}
			if (itemCounts.fragileDamageBonus > 0 && isHealthLow)
			{
				body.inventory.GiveItem(DLC1Content.Items.FragileDamageBonusConsumed, itemCounts.fragileDamageBonus);
				body.inventory.RemoveItem(DLC1Content.Items.FragileDamageBonus, itemCounts.fragileDamageBonus);
				CharacterMasterNotificationQueue.SendTransformNotification(body.master, DLC1Content.Items.FragileDamageBonus.itemIndex, DLC1Content.Items.FragileDamageBonusConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
				EffectData effectData3 = new EffectData
				{
					origin = base.transform.position
				};
				effectData3.SetNetworkedObjectReference(base.gameObject);
				EffectManager.SpawnEffect(AssetReferences.fragileDamageBonusBreakEffectPrefab, effectData3, transmit: true);
			}
		}
		if (damageIsSilent)
		{
			return;
		}
		lastHitTime = Run.FixedTimeStamp.now;
		lastHitAttacker = attacker;
		serverDamageTakenThisUpdate += damageValue;
		if ((bool)modelLocator)
		{
			Transform modelTransform = modelLocator.modelTransform;
			if ((bool)modelTransform)
			{
				Animator component = modelTransform.GetComponent<Animator>();
				if ((bool)component)
				{
					string layerName = "Flinch";
					int layerIndex = component.GetLayerIndex(layerName);
					if (layerIndex >= 0)
					{
						component.SetLayerWeight(layerIndex, 1f + Mathf.Clamp01(damageValue / fullCombinedHealth * 10f) * 3f);
						component.Play("FlinchStart", layerIndex);
					}
				}
			}
		}
		painAnimationHandler?.HandlePain(damageValue, damagePosition);
	}

	[InitDuringStartup]
	private static void Init()
	{
		AssetReferences.Resolve();
	}

	private void Awake()
	{
		body = GetComponent<CharacterBody>();
		modelLocator = GetComponent<ModelLocator>();
		painAnimationHandler = GetComponent<IPainAnimationHandler>();
		onIncomingDamageReceivers = GetComponents<IOnIncomingDamageServerReceiver>();
		onTakeDamageReceivers = GetComponents<IOnTakeDamageServerReceiver>();
		lastHitTime = Run.FixedTimeStamp.negativeInfinity;
		lastHealTime = Run.FixedTimeStamp.negativeInfinity;
		body.onInventoryChanged += OnInventoryChanged;
	}

	private void OnDestroy()
	{
		body.onInventoryChanged -= OnInventoryChanged;
	}

	public void FixedUpdate()
	{
		ManagedFixedUpdate(Time.fixedDeltaTime);
	}

	public void ManagedFixedUpdate(float deltaTime)
	{
		if (NetworkServer.active)
		{
			ServerFixedUpdate(deltaTime);
		}
		if (!alive && wasAlive)
		{
			wasAlive = false;
			if ((bool)modelLocator)
			{
				modelLocator.forceCulled = forceCulled;
			}
			GetComponent<CharacterDeathBehavior>()?.OnDeath();
		}
	}

	private void ServerFixedUpdate(float deltaTime)
	{
		if (recentlyTookDamageCoyoteTimer > 0f)
		{
			recentlyTookDamageCoyoteTimer -= deltaTime;
		}
		if (!alive)
		{
			return;
		}
		regenAccumulator += body.regen * deltaTime;
		if (barrier > 0f)
		{
			Networkbarrier = Mathf.Max(barrier - body.barrierDecayRate * deltaTime, 0f);
			if (barrier <= 0f)
			{
				body.MarkAllStatsDirty();
			}
		}
		else if (barrier <= 0f && body.HasBuff(DLC2Content.Buffs.GeodeBuff))
		{
			body.RemoveBuff(DLC2Content.Buffs.GeodeBuff);
		}
		if (regenAccumulator > 1f)
		{
			float num = Mathf.Floor(regenAccumulator);
			regenAccumulator -= num;
			Heal(num, default(ProcChainMask), nonRegen: false);
		}
		if (regenAccumulator < -1f)
		{
			float num2 = Mathf.Ceil(regenAccumulator);
			regenAccumulator -= num2;
			Networkhealth = health + num2;
			if (health <= 0f)
			{
				Suicide();
			}
		}
		float num3 = shield;
		bool flag = num3 >= body.maxShield;
		if ((body.outOfDanger || isShieldRegenForced) && !flag)
		{
			num3 += body.maxShield * 0.5f * deltaTime;
			if (num3 > body.maxShield)
			{
				num3 = body.maxShield;
			}
		}
		if (num3 >= body.maxShield && !flag)
		{
			Util.PlaySound("Play_item_proc_personal_shield_end", base.gameObject);
		}
		if (!num3.Equals(shield))
		{
			Networkshield = num3;
		}
		if (devilOrbHealPool > 0f)
		{
			devilOrbTimer -= deltaTime;
			if (devilOrbTimer <= 0f)
			{
				devilOrbTimer += 0.1f;
				float scale = 1f;
				float num4 = fullCombinedHealth / 10f;
				float num5 = 2.5f;
				devilOrbHealPool -= num4;
				DevilOrb devilOrb = new DevilOrb();
				devilOrb.origin = body.aimOriginTransform.position;
				devilOrb.damageValue = num4 * num5;
				devilOrb.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
				devilOrb.attacker = base.gameObject;
				devilOrb.damageColorIndex = DamageColorIndex.Poison;
				devilOrb.scale = scale;
				devilOrb.procChainMask.AddProc(ProcType.HealNova);
				devilOrb.effectType = DevilOrb.EffectType.Skull;
				HurtBox hurtBox = devilOrb.PickNextTarget(devilOrb.origin, 40f);
				if ((bool)hurtBox)
				{
					devilOrb.target = hurtBox;
					devilOrb.isCrit = Util.CheckRoll(body.crit, body.master);
					OrbManager.instance.AddOrb(devilOrb);
				}
			}
		}
		adaptiveArmorValue = Mathf.Max(0f, adaptiveArmorValue - 40f * deltaTime);
		serverDamageTakenThisUpdate = 0f;
		ospTimer -= deltaTime;
	}

	private static void SendDamageDealt(DamageReport damageReport)
	{
		DamageInfo damageInfo = damageReport.damageInfo;
		DamageDealtMessage damageDealtMessage = new DamageDealtMessage();
		damageDealtMessage.victim = damageReport.victim.gameObject;
		damageDealtMessage.damage = damageReport.damageDealt;
		damageDealtMessage.attacker = damageInfo.attacker;
		damageDealtMessage.position = damageInfo.position;
		damageDealtMessage.crit = damageInfo.crit;
		damageDealtMessage.damageType = damageInfo.damageType;
		damageDealtMessage.damageColorIndex = damageInfo.damageColorIndex;
		damageDealtMessage.hitLowHealth = damageReport.hitLowHealth;
		NetworkServer.SendToAll(60, damageDealtMessage);
	}

	[NetworkMessageHandler(msgType = 60, client = true)]
	private static void HandleDamageDealt(NetworkMessage netMsg)
	{
		DamageDealtMessage damageDealtMessage = netMsg.ReadMessage<DamageDealtMessage>();
		if ((bool)damageDealtMessage.victim)
		{
			HealthComponent component = damageDealtMessage.victim.GetComponent<HealthComponent>();
			if ((bool)component && !NetworkServer.active)
			{
				component.UpdateLastHitTime(damageDealtMessage.damage, damageDealtMessage.position, damageDealtMessage.isSilent, damageDealtMessage.attacker);
			}
		}
		if (SettingsConVars.enableDamageNumbers.value && (bool)DamageNumberManager.instance)
		{
			TeamComponent teamComponent = null;
			if ((bool)damageDealtMessage.attacker)
			{
				teamComponent = damageDealtMessage.attacker.GetComponent<TeamComponent>();
			}
			DamageNumberManager.instance.SpawnDamageNumber(damageDealtMessage.damage, damageDealtMessage.position, damageDealtMessage.crit, teamComponent ? teamComponent.teamIndex : TeamIndex.None, damageDealtMessage.damageColorIndex);
		}
		GlobalEventManager.ClientDamageNotified(damageDealtMessage);
	}

	private static void SendHeal(GameObject target, float amount, bool isCrit)
	{
		HealMessage healMessage = new HealMessage();
		healMessage.target = target;
		healMessage.amount = (isCrit ? (0f - amount) : amount);
		NetworkServer.SendToAll(61, healMessage);
	}

	[NetworkMessageHandler(msgType = 61, client = true)]
	private static void HandleHeal(NetworkMessage netMsg)
	{
		HealMessage healMessage = netMsg.ReadMessage<HealMessage>();
		if (SettingsConVars.enableDamageNumbers.value && (bool)healMessage.target)
		{
			if ((bool)DamageNumberManager.instance)
			{
				DamageNumberManager.instance.SpawnDamageNumber(healMessage.amount, Util.GetCorePosition(healMessage.target), healMessage.amount < 0f, TeamIndex.Player, DamageColorIndex.Heal);
			}
			HealthComponent component = healMessage.target.GetComponent<HealthComponent>();
			if ((bool)component && !NetworkServer.active)
			{
				component.lastHealTime = Run.FixedTimeStamp.now;
			}
		}
	}

	private void OnInventoryChanged()
	{
		itemCounts = default(ItemCounts);
		Inventory inventory = body.inventory;
		itemCounts = (inventory ? new ItemCounts(inventory) : default(ItemCounts));
		currentEquipmentIndex = (inventory ? inventory.currentEquipmentIndex : EquipmentIndex.None);
		if (!NetworkServer.active)
		{
			return;
		}
		bool flag = itemCounts.repeatHeal != 0;
		if (flag != (bool)repeatHealComponent)
		{
			if (flag)
			{
				repeatHealComponent = base.gameObject.AddComponent<RepeatHealComponent>();
				repeatHealComponent.healthComponent = this;
			}
			else
			{
				UnityEngine.Object.Destroy(repeatHealComponent);
				repeatHealComponent = null;
			}
		}
	}

	public HealthBarValues GetHealthBarValues()
	{
		float num = 1f - 1f / body.cursePenalty;
		float num2 = (1f - num) / fullCombinedHealth;
		float num3 = body.oneShotProtectionFraction * fullCombinedHealth - missingCombinedHealth;
		HealthBarValues result = default(HealthBarValues);
		result.hasInfusion = (float)itemCounts.infusion > 0f;
		result.hasVoidShields = (float)itemCounts.missileVoid > 0f;
		result.isElite = body.isElite;
		result.isBoss = body.isBoss;
		result.isVoid = (body.bodyFlags & CharacterBody.BodyFlags.Void) != 0;
		result.cullFraction = ((isInFrozenState && (body.bodyFlags & CharacterBody.BodyFlags.ImmuneToExecutes) == 0) ? Mathf.Clamp01(0.3f * fullCombinedHealth * num2) : 0f);
		result.healthFraction = Mathf.Clamp01(health * num2);
		result.shieldFraction = Mathf.Clamp01(shield * num2);
		result.barrierFraction = Mathf.Clamp01(barrier * num2);
		result.magneticFraction = Mathf.Clamp01(magnetiCharge * num2);
		result.curseFraction = num;
		result.ospFraction = num3 * num2;
		result.healthDisplayValue = (int)combinedHealth;
		result.maxHealthDisplayValue = (int)fullHealth;
		return result;
	}

	static HealthComponent()
	{
		lowHealthFraction = 0.25f;
		kCmdCmdHealFull = -290141736;
		NetworkBehaviour.RegisterCommandDelegate(typeof(HealthComponent), kCmdCmdHealFull, InvokeCmdCmdHealFull);
		kCmdCmdRechargeShieldFull = -833942624;
		NetworkBehaviour.RegisterCommandDelegate(typeof(HealthComponent), kCmdCmdRechargeShieldFull, InvokeCmdCmdRechargeShieldFull);
		kCmdCmdAddBarrier = -1976809257;
		NetworkBehaviour.RegisterCommandDelegate(typeof(HealthComponent), kCmdCmdAddBarrier, InvokeCmdCmdAddBarrier);
		kCmdCmdForceShieldRegen = -1029271894;
		NetworkBehaviour.RegisterCommandDelegate(typeof(HealthComponent), kCmdCmdForceShieldRegen, InvokeCmdCmdForceShieldRegen);
		NetworkCRC.RegisterBehaviour("HealthComponent", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdHealFull(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdHealFull called on client.");
		}
		else
		{
			((HealthComponent)obj).CmdHealFull();
		}
	}

	protected static void InvokeCmdCmdRechargeShieldFull(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRechargeShieldFull called on client.");
		}
		else
		{
			((HealthComponent)obj).CmdRechargeShieldFull();
		}
	}

	protected static void InvokeCmdCmdAddBarrier(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdAddBarrier called on client.");
		}
		else
		{
			((HealthComponent)obj).CmdAddBarrier(reader.ReadSingle());
		}
	}

	protected static void InvokeCmdCmdForceShieldRegen(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdForceShieldRegen called on client.");
		}
		else
		{
			((HealthComponent)obj).CmdForceShieldRegen();
		}
	}

	public void CallCmdHealFull()
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdHealFull called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdHealFull();
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdHealFull);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendCommandInternal(networkWriter, 0, "CmdHealFull");
	}

	public void CallCmdRechargeShieldFull()
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdRechargeShieldFull called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdRechargeShieldFull();
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdRechargeShieldFull);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendCommandInternal(networkWriter, 0, "CmdRechargeShieldFull");
	}

	public void CallCmdAddBarrier(float value)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdAddBarrier called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdAddBarrier(value);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdAddBarrier);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(value);
		SendCommandInternal(networkWriter, 0, "CmdAddBarrier");
	}

	public void CallCmdForceShieldRegen()
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdForceShieldRegen called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdForceShieldRegen();
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdForceShieldRegen);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendCommandInternal(networkWriter, 0, "CmdForceShieldRegen");
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(health);
			writer.Write(shield);
			writer.Write(barrier);
			writer.Write(magnetiCharge);
			writer.WritePackedUInt32(_killingDamageType);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(health);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(shield);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(barrier);
		}
		if ((base.syncVarDirtyBits & 8u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(magnetiCharge);
		}
		if ((base.syncVarDirtyBits & 0x10u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32(_killingDamageType);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			health = reader.ReadSingle();
			shield = reader.ReadSingle();
			barrier = reader.ReadSingle();
			magnetiCharge = reader.ReadSingle();
			_killingDamageType = reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			health = reader.ReadSingle();
		}
		if (((uint)num & 2u) != 0)
		{
			shield = reader.ReadSingle();
		}
		if (((uint)num & 4u) != 0)
		{
			SetBarrier(reader.ReadSingle());
		}
		if (((uint)num & 8u) != 0)
		{
			magnetiCharge = reader.ReadSingle();
		}
		if (((uint)num & 0x10u) != 0)
		{
			_killingDamageType = reader.ReadPackedUInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}
