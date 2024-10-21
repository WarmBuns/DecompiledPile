using System;
using System.Collections.Generic;
using Grumpy;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class BulletAttack
{
	public enum FalloffModel
	{
		None,
		DefaultBullet,
		Buckshot
	}

	public delegate bool HitCallback(BulletAttack bulletAttack, ref BulletHit hitInfo);

	public delegate void ModifyOutgoingDamageCallback(BulletAttack bulletAttack, ref BulletHit hitInfo, DamageInfo damageInfo);

	public delegate bool FilterCallback(BulletAttack bulletAttack, ref BulletHit hitInfo);

	public class BulletHit
	{
		public Vector3 direction;

		public Vector3 point;

		public Vector3 surfaceNormal;

		public float distance;

		public Collider collider;

		public HurtBox hitHurtBox;

		public GameObject entityObject;

		public HurtBox.DamageModifier damageModifier;

		public bool isSniperHit;

		public HurtBox hurtBox;

		public void Reset()
		{
			direction = Vector3.zero;
			point = Vector3.zero;
			surfaceNormal = Vector3.zero;
			distance = 0f;
			collider = null;
			entityObject = null;
			damageModifier = HurtBox.DamageModifier.Normal;
			isSniperHit = false;
			hurtBox = null;
		}
	}

	public class BulletHitPool : GenericPool<BulletHit>
	{
		protected override BulletHit CreateNewObject(bool inPoolEntry = true)
		{
			return new BulletHit();
		}

		public void ReturnAllInUse()
		{
			if (_InUse != null)
			{
				for (int num = _InUse.Count - 1; num >= 0; num--)
				{
					ReturnObject(_InUse[num]);
				}
			}
		}
	}

	private static GameObject sniperTargetHitEffect;

	public GameObject owner;

	public GameObject weapon;

	public float damage = 1f;

	public bool isCrit;

	public float force = 1f;

	public ProcChainMask procChainMask;

	public float procCoefficient = 1f;

	public DamageTypeCombo damageType = DamageType.Generic;

	public DamageColorIndex damageColorIndex;

	public bool sniper;

	public FalloffModel falloffModel = FalloffModel.DefaultBullet;

	public GameObject tracerEffectPrefab;

	public GameObject hitEffectPrefab;

	public string muzzleName = "";

	public bool HitEffectNormal = true;

	public Vector3 origin;

	private Vector3 _aimVector;

	private float _maxDistance = 200f;

	public float radius;

	public uint bulletCount = 1u;

	public float minSpread;

	public float maxSpread;

	public float spreadPitchScale = 1f;

	public float spreadYawScale = 1f;

	public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;

	private static readonly LayerMask defaultHitMask = (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask;

	public LayerMask hitMask = defaultHitMask;

	private static readonly LayerMask defaultStopperMask = defaultHitMask;

	public LayerMask stopperMask = defaultStopperMask;

	public bool smartCollision;

	public bool cheapMultiBullet;

	public float multiBulletOdds = 0.4f;

	public bool allowTrajectoryAimAssist = true;

	public float trajectoryAimAssistMultiplier = 1f;

	public HitCallback hitCallback;

	protected DamageInfo _dmgInfo;

	public static readonly HitCallback defaultHitCallback = DefaultHitCallbackImplementation;

	public ModifyOutgoingDamageCallback modifyOutgoingDamageCallback;

	private static NetworkWriter messageWriter = new NetworkWriter();

	public FilterCallback filterCallback;

	public static readonly FilterCallback defaultFilterCallback = DefaultFilterCallbackImplementation;

	protected static BulletHitPool __BulletHitPool = null;

	protected static List<BulletHit> _BulletHits = new List<BulletHit>();

	protected static List<GameObject> _IgnoreList = new List<GameObject>();

	protected static HashSet<GameObject> _EncounteredEntityObjects = new HashSet<GameObject>();

	protected static bool _UsePools = true;

	protected static EffectData _effectData = null;

	public Vector3 aimVector
	{
		get
		{
			return _aimVector;
		}
		set
		{
			_aimVector = value;
			_aimVector.Normalize();
		}
	}

	public float maxDistance
	{
		get
		{
			return _maxDistance;
		}
		set
		{
			if (!float.IsInfinity(value) && !float.IsNaN(value))
			{
				_maxDistance = value;
				return;
			}
			Debug.LogFormat("BulletAttack.maxDistance was assigned a value other than a finite number. value={0}", value);
		}
	}

	[Obsolete("Use .defaultHitCallback instead.", false)]
	public static HitCallback DefaultHitCallback => defaultHitCallback;

	[Obsolete("Use .defaultFilterCallback instead.", false)]
	public static FilterCallback DefaultFilterCallback => defaultFilterCallback;

	protected static BulletHitPool _BulletHitPool
	{
		get
		{
			if (__BulletHitPool == null)
			{
				__BulletHitPool = new BulletHitPool();
				__BulletHitPool.AlwaysGrowable = true;
				__BulletHitPool.Initialize(20, 10);
			}
			return __BulletHitPool;
		}
	}

	public static bool UsePools => _UsePools;

	public BulletAttack()
	{
		filterCallback = defaultFilterCallback;
		hitCallback = defaultHitCallback;
	}

	[InitDuringStartup]
	private static void Init()
	{
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/Effects/ImpactEffects/SniperTargetHitEffect", delegate(GameObject operationResult)
		{
			sniperTargetHitEffect = operationResult;
		});
	}

	public void Reset()
	{
		owner = null;
		weapon = null;
		damage = 1f;
		isCrit = false;
		force = 1f;
		procChainMask.mask = 0u;
		procCoefficient = 1f;
		damageType = DamageType.Generic;
		damageColorIndex = DamageColorIndex.Default;
		sniper = false;
		falloffModel = FalloffModel.DefaultBullet;
		tracerEffectPrefab = null;
		hitEffectPrefab = null;
		muzzleName = "";
		HitEffectNormal = true;
		origin = Vector3.zero;
		_aimVector = Vector3.zero;
		_maxDistance = 200f;
		radius = 0f;
		bulletCount = 1u;
		minSpread = 0f;
		maxSpread = 0f;
		spreadPitchScale = 1f;
		spreadYawScale = 1f;
		queryTriggerInteraction = QueryTriggerInteraction.Ignore;
		hitMask = defaultHitMask;
		stopperMask = defaultStopperMask;
		smartCollision = false;
		cheapMultiBullet = false;
		filterCallback = defaultFilterCallback;
		hitCallback = defaultHitCallback;
	}

	private static void PlayHitEffect(BulletAttack bulletAttack, ref BulletHit hitInfo)
	{
		if ((bool)bulletAttack.hitEffectPrefab)
		{
			EffectManager.SimpleImpactEffect(bulletAttack.hitEffectPrefab, hitInfo.point, bulletAttack.HitEffectNormal ? hitInfo.surfaceNormal : (-hitInfo.direction), transmit: true);
		}
		if (hitInfo.isSniperHit && (object)sniperTargetHitEffect != null)
		{
			EffectData effectData = new EffectData
			{
				origin = hitInfo.point,
				rotation = Quaternion.LookRotation(-hitInfo.direction)
			};
			effectData.SetHurtBoxReference(hitInfo.hitHurtBox);
			EffectManager.SpawnEffect(sniperTargetHitEffect, effectData, transmit: true);
		}
		if ((bool)hitInfo.collider)
		{
			SurfaceDef objectSurfaceDef = SurfaceDefProvider.GetObjectSurfaceDef(hitInfo.collider, hitInfo.point);
			if ((bool)objectSurfaceDef && (bool)objectSurfaceDef.impactEffectPrefab)
			{
				EffectData effectData2 = new EffectData
				{
					origin = hitInfo.point,
					rotation = Quaternion.LookRotation(hitInfo.surfaceNormal),
					color = objectSurfaceDef.approximateColor,
					surfaceDefIndex = objectSurfaceDef.surfaceDefIndex
				};
				EffectManager.SpawnEffect(objectSurfaceDef.impactEffectPrefab, effectData2, transmit: true);
			}
		}
	}

	private static bool DefaultHitCallbackImplementation(BulletAttack bulletAttack, ref BulletHit hitInfo)
	{
		bool result = false;
		if ((bool)hitInfo.collider)
		{
			result = ((1 << hitInfo.collider.gameObject.layer) & (int)bulletAttack.stopperMask) == 0;
		}
		PlayHitEffect(bulletAttack, ref hitInfo);
		GameObject entityObject = hitInfo.entityObject;
		if ((bool)entityObject)
		{
			float num = CalcFalloffFactor(bulletAttack.falloffModel, hitInfo.distance);
			DamageInfo damageInfo = new DamageInfo();
			damageInfo.damage = bulletAttack.damage * num;
			damageInfo.crit = bulletAttack.isCrit;
			damageInfo.attacker = bulletAttack.owner;
			damageInfo.inflictor = bulletAttack.weapon;
			damageInfo.position = hitInfo.point;
			damageInfo.force = hitInfo.direction * (bulletAttack.force * num);
			damageInfo.procChainMask = bulletAttack.procChainMask;
			damageInfo.procCoefficient = bulletAttack.procCoefficient;
			damageInfo.damageType = bulletAttack.damageType;
			damageInfo.damageColorIndex = bulletAttack.damageColorIndex;
			damageInfo.ModifyDamageInfo(hitInfo.damageModifier);
			if (hitInfo.isSniperHit)
			{
				damageInfo.crit = true;
				damageInfo.damageColorIndex = DamageColorIndex.Sniper;
			}
			bulletAttack.modifyOutgoingDamageCallback?.Invoke(bulletAttack, ref hitInfo, damageInfo);
			TeamIndex attackerTeamIndex = TeamIndex.None;
			if ((bool)bulletAttack.owner)
			{
				TeamComponent component = bulletAttack.owner.GetComponent<TeamComponent>();
				if ((bool)component)
				{
					attackerTeamIndex = component.teamIndex;
				}
			}
			HealthComponent healthComponent = null;
			if ((bool)hitInfo.hitHurtBox)
			{
				healthComponent = hitInfo.hitHurtBox.healthComponent;
			}
			bool flag = (bool)healthComponent && FriendlyFireManager.ShouldDirectHitProceed(healthComponent, attackerTeamIndex);
			if (NetworkServer.active)
			{
				if (flag)
				{
					healthComponent.TakeDamage(damageInfo);
					GlobalEventManager.instance.OnHitEnemy(damageInfo, hitInfo.entityObject);
				}
				GlobalEventManager.instance.OnHitAll(damageInfo, hitInfo.entityObject);
			}
			else if (ClientScene.ready)
			{
				messageWriter.StartMessage(53);
				int currentLogLevel = LogFilter.currentLogLevel;
				LogFilter.currentLogLevel = 4;
				messageWriter.Write(entityObject);
				LogFilter.currentLogLevel = currentLogLevel;
				messageWriter.Write(damageInfo);
				messageWriter.Write(flag);
				messageWriter.FinishMessage();
				ClientScene.readyConnection.SendWriter(messageWriter, QosChannelIndex.defaultReliable.intVal);
			}
		}
		return result;
	}

	private static float CalcFalloffFactor(FalloffModel falloffModel, float distance)
	{
		return falloffModel switch
		{
			FalloffModel.DefaultBullet => 0.5f + Mathf.Clamp01(Mathf.InverseLerp(60f, 25f, distance)) * 0.5f, 
			FalloffModel.Buckshot => 0.25f + Mathf.Clamp01(Mathf.InverseLerp(25f, 7f, distance)) * 0.75f, 
			_ => 1f, 
		};
	}

	private static bool DefaultFilterCallbackImplementation(BulletAttack bulletAttack, ref BulletHit hitInfo)
	{
		HurtBox component = hitInfo.collider.GetComponent<HurtBox>();
		if ((bool)component && (bool)component.healthComponent && component.healthComponent.gameObject == bulletAttack.weapon)
		{
			return false;
		}
		return hitInfo.entityObject != bulletAttack.weapon;
	}

	private void InitBulletHitFromOriginHit(ref BulletHit bulletHit, Vector3 direction, Collider hitCollider)
	{
		bulletHit.direction = direction;
		bulletHit.point = origin;
		bulletHit.surfaceNormal = -direction;
		bulletHit.distance = 0f;
		bulletHit.collider = hitCollider;
		HurtBox component = bulletHit.collider.GetComponent<HurtBox>();
		bulletHit.hitHurtBox = component;
		bulletHit.entityObject = (((bool)component && (bool)component.healthComponent) ? component.healthComponent.gameObject : bulletHit.collider.gameObject);
		bulletHit.damageModifier = (component ? component.damageModifier : HurtBox.DamageModifier.Normal);
	}

	private void InitBulletHitFromRaycastHit(ref BulletHit bulletHit, Vector3 origin, Vector3 direction, ref RaycastHit raycastHit)
	{
		bulletHit.direction = direction;
		bulletHit.surfaceNormal = raycastHit.normal;
		bulletHit.distance = raycastHit.distance;
		bulletHit.collider = raycastHit.collider;
		bulletHit.point = ((bulletHit.distance == 0f) ? origin : raycastHit.point);
		HurtBox component = bulletHit.collider.GetComponent<HurtBox>();
		bulletHit.hitHurtBox = component;
		bulletHit.entityObject = (((bool)component && (bool)component.healthComponent) ? component.healthComponent.gameObject : bulletHit.collider.gameObject);
		bulletHit.damageModifier = (component ? component.damageModifier : HurtBox.DamageModifier.Normal);
	}

	private bool ProcessHit(ref BulletHit hitInfo)
	{
		if (!filterCallback(this, ref hitInfo))
		{
			return true;
		}
		if (sniper)
		{
			hitInfo.isSniperHit = IsSniperTargetHit(in hitInfo);
		}
		return hitCallback(this, ref hitInfo);
	}

	private static bool IsSniperTargetHit(in BulletHit hitInfo)
	{
		if ((bool)hitInfo.hitHurtBox && (bool)hitInfo.hitHurtBox.hurtBoxGroup)
		{
			HurtBox[] hurtBoxes = hitInfo.hitHurtBox.hurtBoxGroup.hurtBoxes;
			foreach (HurtBox hurtBox in hurtBoxes)
			{
				if ((bool)hurtBox && hurtBox.isSniperTarget && Vector3.ProjectOnPlane(hitInfo.point - hurtBox.transform.position, hitInfo.direction).sqrMagnitude <= HurtBox.sniperTargetRadiusSqr)
				{
					return true;
				}
			}
		}
		return false;
	}

	private GameObject ProcessHitList(List<BulletHit> hits, ref Vector3 endPosition, List<GameObject> ignoreList)
	{
		int count = hits.Count;
		int[] array = new int[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = i;
		}
		for (int j = 0; j < count; j++)
		{
			float distance = maxDistance;
			int num = j;
			for (int k = j; k < count; k++)
			{
				int index = array[k];
				if (hits[index].distance < distance)
				{
					distance = hits[index].distance;
					num = k;
				}
			}
			GameObject entityObject = hits[array[num]].entityObject;
			if (!ignoreList.Contains(entityObject))
			{
				ignoreList.Add(entityObject);
				BulletHit hitInfo = hits[array[num]];
				if (!ProcessHit(ref hitInfo))
				{
					endPosition = hits[array[num]].point;
					return entityObject;
				}
			}
			array[num] = array[j];
		}
		return null;
	}

	private static GameObject LookUpColliderEntityObject(Collider collider)
	{
		HurtBox component = collider.GetComponent<HurtBox>();
		if (!component || !component.healthComponent)
		{
			return collider.gameObject;
		}
		return component.healthComponent.gameObject;
	}

	private static Collider[] PhysicsOverlapPoint(Vector3 point, int layerMask = -1, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
	{
		return Physics.OverlapBox(point, Vector3.zero, Quaternion.identity, layerMask, queryTriggerInteraction);
	}

	public void Fire()
	{
		if (allowTrajectoryAimAssist)
		{
			aimVector = TrajectoryAimAssist.ApplyTrajectoryAimAssist(aimVector, origin, maxDistance, owner, weapon, trajectoryAimAssistMultiplier);
		}
		Vector3[] array = new Vector3[bulletCount];
		Vector3 up = Vector3.up;
		Vector3 axis = Vector3.Cross(up, aimVector);
		for (int i = 0; i < bulletCount; i++)
		{
			float x = UnityEngine.Random.Range(minSpread, maxSpread);
			float z = UnityEngine.Random.Range(0f, 360f);
			Vector3 vector = Quaternion.Euler(0f, 0f, z) * (Quaternion.Euler(x, 0f, 0f) * Vector3.forward);
			float y = vector.y;
			vector.y = 0f;
			float angle = (Mathf.Atan2(vector.z, vector.x) * 57.29578f - 90f) * spreadYawScale;
			float angle2 = Mathf.Atan2(y, vector.magnitude) * 57.29578f * spreadPitchScale;
			array[i] = Quaternion.AngleAxis(angle, up) * (Quaternion.AngleAxis(angle2, axis) * aimVector);
		}
		int muzzleIndex = -1;
		if (!weapon)
		{
			weapon = owner;
		}
		if ((bool)weapon)
		{
			ModelLocator component = weapon.GetComponent<ModelLocator>();
			if ((bool)component && (bool)component.modelTransform)
			{
				ChildLocator component2 = component.modelTransform.GetComponent<ChildLocator>();
				if ((bool)component2)
				{
					muzzleIndex = component2.FindChildIndex(muzzleName);
				}
			}
		}
		Vector3 axis2 = Vector3.Cross(Vector3.up, aimVector);
		Vector3 forward = Vector3.forward;
		if (cheapMultiBullet)
		{
			float x2 = UnityEngine.Random.Range(minSpread, maxSpread);
			float z2 = UnityEngine.Random.Range(0f, 360f);
			Vector3 vector2 = Quaternion.Euler(0f, 0f, z2) * (Quaternion.Euler(x2, 0f, 0f) * Vector3.forward);
			float y2 = vector2.y;
			vector2.y = 0f;
			float angle3 = (Mathf.Atan2(vector2.z, vector2.x) * 57.29578f - 90f) * spreadYawScale;
			float angle4 = Mathf.Atan2(y2, vector2.magnitude) * 57.29578f * spreadPitchScale;
			forward = Quaternion.AngleAxis(angle3, Vector3.up) * (Quaternion.AngleAxis(angle4, axis2) * aimVector);
			FireMulti(forward, muzzleIndex);
			return;
		}
		for (int j = 0; j < bulletCount; j++)
		{
			float x3 = UnityEngine.Random.Range(minSpread, maxSpread);
			float z3 = UnityEngine.Random.Range(0f, 360f);
			Vector3 vector3 = Quaternion.Euler(0f, 0f, z3) * (Quaternion.Euler(x3, 0f, 0f) * Vector3.forward);
			float y3 = vector3.y;
			vector3.y = 0f;
			float angle5 = (Mathf.Atan2(vector3.z, vector3.x) * 57.29578f - 90f) * spreadYawScale;
			float angle6 = Mathf.Atan2(y3, vector3.magnitude) * 57.29578f * spreadPitchScale;
			forward = Quaternion.AngleAxis(angle5, Vector3.up) * (Quaternion.AngleAxis(angle6, axis2) * aimVector);
			FireSingle(forward, muzzleIndex);
		}
	}

	public Vector3 Fire_ReturnHit()
	{
		int muzzleIndex = -1;
		_ = origin;
		if (!weapon)
		{
			weapon = owner;
		}
		if ((bool)weapon)
		{
			ModelLocator component = weapon.GetComponent<ModelLocator>();
			if ((bool)component && (bool)component.modelTransform)
			{
				ChildLocator component2 = component.modelTransform.GetComponent<ChildLocator>();
				if ((bool)component2)
				{
					muzzleIndex = component2.FindChildIndex(muzzleName);
				}
			}
		}
		Vector3 axis = Vector3.Cross(Vector3.up, aimVector);
		Vector3 forward = Vector3.forward;
		Vector3 result = Vector3.zero;
		for (int i = 0; i < bulletCount; i++)
		{
			float x = UnityEngine.Random.Range(minSpread, maxSpread);
			float z = UnityEngine.Random.Range(0f, 360f);
			Vector3 vector = Quaternion.Euler(0f, 0f, z) * (Quaternion.Euler(x, 0f, 0f) * Vector3.forward);
			float y = vector.y;
			vector.y = 0f;
			float angle = (Mathf.Atan2(vector.z, vector.x) * 57.29578f - 90f) * spreadYawScale;
			float angle2 = Mathf.Atan2(y, vector.magnitude) * 57.29578f * spreadPitchScale;
			forward = Quaternion.AngleAxis(angle, Vector3.up) * (Quaternion.AngleAxis(angle2, axis) * aimVector);
			result = FireSingle_ReturnHit(forward, muzzleIndex);
		}
		return result;
	}

	public static void TogglePooling()
	{
		_UsePools = !_UsePools;
		Debug.LogFormat("BulletAttack pooling {0}", _UsePools ? "ENABLED" : "DISABLED");
	}

	public static void DumpPools()
	{
		Debug.LogFormat("BulletAttack Pool: Pooled {0}, InUse {1}", _BulletHitPool.PoolCount(), _BulletHitPool.InUseCount());
	}

	protected static BulletHit GetBulletHit()
	{
		BulletHit @object = _BulletHitPool.GetObject();
		@object.Reset();
		return @object;
	}

	private void FireSingle(Vector3 normal, int muzzleIndex)
	{
		if (_UsePools)
		{
			float distance = maxDistance;
			Vector3 endPosition = origin + normal * maxDistance;
			_BulletHits.Clear();
			bool num = radius == 0f || smartCollision;
			bool flag = radius != 0f;
			if (smartCollision)
			{
				_EncounteredEntityObjects.Clear();
			}
			if (num)
			{
				int num2 = 0;
				num2 = HGPhysics.RaycastAll(out var hits, origin, normal, distance, hitMask, queryTriggerInteraction);
				for (int i = 0; i < num2; i++)
				{
					BulletHit bulletHit = GetBulletHit();
					InitBulletHitFromRaycastHit(ref bulletHit, origin, normal, ref hits[i]);
					_BulletHits.Add(bulletHit);
					if (smartCollision)
					{
						_EncounteredEntityObjects.Add(bulletHit.entityObject);
					}
					if (bulletHit.distance < distance)
					{
						distance = bulletHit.distance;
					}
				}
				HGPhysics.ReturnResults(hits);
			}
			if (flag)
			{
				LayerMask layerMask = hitMask;
				if (smartCollision)
				{
					layerMask = (int)layerMask & ~(int)LayerIndex.world.mask;
				}
				int num3 = 0;
				num3 = HGPhysics.SphereCastAll(out var hits2, origin, radius, normal, distance, layerMask, queryTriggerInteraction);
				for (int j = 0; j < num3; j++)
				{
					BulletHit bulletHit2 = GetBulletHit();
					InitBulletHitFromRaycastHit(ref bulletHit2, origin, normal, ref hits2[j]);
					if (!smartCollision || !_EncounteredEntityObjects.Contains(bulletHit2.entityObject))
					{
						_BulletHits.Add(bulletHit2);
					}
				}
				HGPhysics.ReturnResults(hits2);
			}
			_IgnoreList.Clear();
			ProcessHitList(_BulletHits, ref endPosition, _IgnoreList);
			if ((bool)tracerEffectPrefab)
			{
				if (_effectData == null)
				{
					_effectData = new EffectData();
				}
				_effectData.Reset();
				_effectData.origin = endPosition;
				_effectData.start = origin;
				_effectData.SetChildLocatorTransformReference(weapon, muzzleIndex);
				EffectManager.SpawnEffect(tracerEffectPrefab, _effectData, transmit: true);
			}
			_BulletHitPool.ReturnAllInUse();
			_EncounteredEntityObjects.Clear();
			return;
		}
		float distance2 = maxDistance;
		Vector3 endPosition2 = origin + normal * maxDistance;
		List<BulletHit> list = new List<BulletHit>();
		bool num4 = radius == 0f || smartCollision;
		bool flag2 = radius != 0f;
		HashSet<GameObject> hashSet = null;
		if (smartCollision)
		{
			hashSet = new HashSet<GameObject>();
		}
		if (num4)
		{
			int num5 = 0;
			num5 = HGPhysics.RaycastAll(out var hits3, origin, normal, distance2, hitMask, queryTriggerInteraction);
			for (int k = 0; k < num5; k++)
			{
				BulletHit bulletHit3 = new BulletHit();
				InitBulletHitFromRaycastHit(ref bulletHit3, origin, normal, ref hits3[k]);
				list.Add(bulletHit3);
				if (smartCollision)
				{
					hashSet.Add(bulletHit3.entityObject);
				}
				if (bulletHit3.distance < distance2)
				{
					distance2 = bulletHit3.distance;
				}
			}
			HGPhysics.ReturnResults(hits3);
		}
		if (flag2)
		{
			LayerMask layerMask2 = hitMask;
			if (smartCollision)
			{
				layerMask2 = (int)layerMask2 & ~(int)LayerIndex.world.mask;
			}
			int num6 = 0;
			num6 = HGPhysics.SphereCastAll(out var hits4, origin, radius, normal, distance2, layerMask2, queryTriggerInteraction);
			for (int l = 0; l < num6; l++)
			{
				BulletHit bulletHit4 = new BulletHit();
				InitBulletHitFromRaycastHit(ref bulletHit4, origin, normal, ref hits4[l]);
				if (!smartCollision || !hashSet.Contains(bulletHit4.entityObject))
				{
					list.Add(bulletHit4);
				}
			}
			HGPhysics.ReturnResults(hits4);
		}
		ProcessHitList(list, ref endPosition2, new List<GameObject>());
		if ((bool)tracerEffectPrefab)
		{
			EffectData effectData = new EffectData
			{
				origin = endPosition2,
				start = origin
			};
			effectData.SetChildLocatorTransformReference(weapon, muzzleIndex);
			EffectManager.SpawnEffect(tracerEffectPrefab, effectData, transmit: true);
		}
	}

	private void FireMulti(Vector3 normal, int muzzleIndex)
	{
		if (_UsePools)
		{
			float distance = maxDistance;
			Vector3 endPosition = origin + normal * maxDistance;
			_BulletHits.Clear();
			bool num = radius == 0f || smartCollision;
			bool flag = radius != 0f;
			if (smartCollision)
			{
				_EncounteredEntityObjects.Clear();
			}
			if (num)
			{
				int num2 = 0;
				num2 = HGPhysics.RaycastAll(out var hits, origin, normal, distance, hitMask, queryTriggerInteraction);
				for (int i = 0; i < num2; i++)
				{
					BulletHit bulletHit = GetBulletHit();
					InitBulletHitFromRaycastHit(ref bulletHit, origin, normal, ref hits[i]);
					_BulletHits.Add(bulletHit);
					if (smartCollision)
					{
						_EncounteredEntityObjects.Add(bulletHit.entityObject);
					}
					if (bulletHit.distance < distance)
					{
						distance = bulletHit.distance;
					}
				}
				HGPhysics.ReturnResults(hits);
			}
			if (flag)
			{
				LayerMask layerMask = hitMask;
				if (smartCollision)
				{
					layerMask = (int)layerMask & ~(int)LayerIndex.world.mask;
				}
				int num3 = 0;
				num3 = HGPhysics.SphereCastAll(out var hits2, origin, radius, normal, distance, layerMask, queryTriggerInteraction);
				for (int j = 0; j < num3; j++)
				{
					BulletHit bulletHit2 = GetBulletHit();
					InitBulletHitFromRaycastHit(ref bulletHit2, origin, normal, ref hits2[j]);
					if (!smartCollision || !_EncounteredEntityObjects.Contains(bulletHit2.entityObject))
					{
						_BulletHits.Add(bulletHit2);
					}
				}
				HGPhysics.ReturnResults(hits2);
			}
			_IgnoreList.Clear();
			ProcessHitList(_BulletHits, ref endPosition, _IgnoreList);
			if ((bool)tracerEffectPrefab)
			{
				if (_effectData == null)
				{
					_effectData = new EffectData();
				}
				_effectData.Reset();
				_effectData.origin = endPosition;
				_effectData.start = origin;
				_effectData.SetChildLocatorTransformReference(weapon, muzzleIndex);
				EffectManager.SpawnEffect(tracerEffectPrefab, _effectData, transmit: true);
			}
			_BulletHitPool.ReturnAllInUse();
			_EncounteredEntityObjects.Clear();
			return;
		}
		float distance2 = maxDistance;
		Vector3 endPosition2 = origin + normal * maxDistance;
		List<BulletHit> list = new List<BulletHit>();
		bool num4 = radius == 0f || smartCollision;
		bool flag2 = radius != 0f;
		HashSet<GameObject> hashSet = null;
		if (smartCollision)
		{
			hashSet = new HashSet<GameObject>();
		}
		if (num4)
		{
			int num5 = 0;
			num5 = HGPhysics.RaycastAll(out var hits3, origin, normal, distance2, hitMask, queryTriggerInteraction);
			for (int k = 0; k < num5; k++)
			{
				BulletHit bulletHit3 = new BulletHit();
				InitBulletHitFromRaycastHit(ref bulletHit3, origin, normal, ref hits3[k]);
				list.Add(bulletHit3);
				if (smartCollision)
				{
					hashSet.Add(bulletHit3.entityObject);
				}
				if (bulletHit3.distance < distance2)
				{
					distance2 = bulletHit3.distance;
				}
			}
			HGPhysics.ReturnResults(hits3);
		}
		if (flag2)
		{
			LayerMask layerMask2 = hitMask;
			if (smartCollision)
			{
				layerMask2 = (int)layerMask2 & ~(int)LayerIndex.world.mask;
			}
			int num6 = 0;
			num6 = HGPhysics.SphereCastAll(out var hits4, origin, radius, normal, distance2, layerMask2, queryTriggerInteraction);
			for (int l = 0; l < num6; l++)
			{
				BulletHit bulletHit4 = new BulletHit();
				InitBulletHitFromRaycastHit(ref bulletHit4, origin, normal, ref hits4[l]);
				if (!smartCollision || !hashSet.Contains(bulletHit4.entityObject))
				{
					list.Add(bulletHit4);
				}
			}
			HGPhysics.ReturnResults(hits4);
		}
		ProcessHitList(list, ref endPosition2, new List<GameObject>());
		if ((bool)tracerEffectPrefab)
		{
			EffectData effectData = new EffectData
			{
				origin = endPosition2,
				start = origin
			};
			effectData.SetChildLocatorTransformReference(weapon, muzzleIndex);
			EffectManager.SpawnEffect(tracerEffectPrefab, effectData, transmit: true);
		}
	}

	private Vector3 FireSingle_ReturnHit(Vector3 normal, int muzzleIndex)
	{
		Vector3 endPosition;
		if (_UsePools)
		{
			float distance = maxDistance;
			endPosition = origin + normal * maxDistance;
			_BulletHits.Clear();
			bool num = radius == 0f || smartCollision;
			bool flag = radius != 0f;
			if (smartCollision)
			{
				_EncounteredEntityObjects.Clear();
			}
			if (num)
			{
				int num2 = 0;
				num2 = HGPhysics.RaycastAll(out var hits, origin, normal, distance, hitMask, queryTriggerInteraction);
				for (int i = 0; i < num2; i++)
				{
					BulletHit bulletHit = GetBulletHit();
					InitBulletHitFromRaycastHit(ref bulletHit, origin, normal, ref hits[i]);
					_BulletHits.Add(bulletHit);
					if (smartCollision)
					{
						_EncounteredEntityObjects.Add(bulletHit.entityObject);
					}
					if (bulletHit.distance < distance)
					{
						distance = bulletHit.distance;
					}
				}
				HGPhysics.ReturnResults(hits);
			}
			if (flag)
			{
				LayerMask layerMask = hitMask;
				if (smartCollision)
				{
					layerMask = (int)layerMask & ~(int)LayerIndex.world.mask;
				}
				int num3 = 0;
				num3 = HGPhysics.SphereCastAll(out var hits2, origin, radius, normal, distance, layerMask, queryTriggerInteraction);
				for (int j = 0; j < num3; j++)
				{
					BulletHit bulletHit2 = GetBulletHit();
					InitBulletHitFromRaycastHit(ref bulletHit2, origin, normal, ref hits2[j]);
					if (!smartCollision || !_EncounteredEntityObjects.Contains(bulletHit2.entityObject))
					{
						_BulletHits.Add(bulletHit2);
					}
				}
				HGPhysics.ReturnResults(hits2);
			}
			_IgnoreList.Clear();
			ProcessHitList(_BulletHits, ref endPosition, _IgnoreList);
			if ((bool)tracerEffectPrefab)
			{
				if (_effectData == null)
				{
					_effectData = new EffectData();
				}
				_effectData.Reset();
				_effectData.origin = endPosition;
				_effectData.start = origin;
				_effectData.SetChildLocatorTransformReference(weapon, muzzleIndex);
				EffectManager.SpawnEffect(tracerEffectPrefab, _effectData, transmit: true);
			}
			_BulletHitPool.ReturnAllInUse();
			_EncounteredEntityObjects.Clear();
		}
		else
		{
			float distance2 = maxDistance;
			endPosition = origin + normal * maxDistance;
			List<BulletHit> list = new List<BulletHit>();
			bool num4 = radius == 0f || smartCollision;
			bool flag2 = radius != 0f;
			HashSet<GameObject> hashSet = null;
			if (smartCollision)
			{
				hashSet = new HashSet<GameObject>();
			}
			if (num4)
			{
				int num5 = 0;
				num5 = HGPhysics.RaycastAll(out var hits3, origin, normal, distance2, hitMask, queryTriggerInteraction);
				for (int k = 0; k < num5; k++)
				{
					BulletHit bulletHit3 = new BulletHit();
					InitBulletHitFromRaycastHit(ref bulletHit3, origin, normal, ref hits3[k]);
					list.Add(bulletHit3);
					if (smartCollision)
					{
						hashSet.Add(bulletHit3.entityObject);
					}
					if (bulletHit3.distance < distance2)
					{
						distance2 = bulletHit3.distance;
					}
				}
				HGPhysics.ReturnResults(hits3);
			}
			if (flag2)
			{
				LayerMask layerMask2 = hitMask;
				if (smartCollision)
				{
					layerMask2 = (int)layerMask2 & ~(int)LayerIndex.world.mask;
				}
				int num6 = 0;
				num6 = HGPhysics.SphereCastAll(out var hits4, origin, radius, normal, distance2, layerMask2, queryTriggerInteraction);
				for (int l = 0; l < num6; l++)
				{
					BulletHit bulletHit4 = new BulletHit();
					InitBulletHitFromRaycastHit(ref bulletHit4, origin, normal, ref hits4[l]);
					if (!smartCollision || !hashSet.Contains(bulletHit4.entityObject))
					{
						list.Add(bulletHit4);
					}
				}
				HGPhysics.ReturnResults(hits4);
			}
			ProcessHitList(list, ref endPosition, new List<GameObject>());
			if ((bool)tracerEffectPrefab)
			{
				EffectData effectData = new EffectData
				{
					origin = endPosition,
					start = origin
				};
				effectData.SetChildLocatorTransformReference(weapon, muzzleIndex);
				EffectManager.SpawnEffect(tracerEffectPrefab, effectData, transmit: true);
			}
		}
		return endPosition;
	}

	[NetworkMessageHandler(msgType = 53, server = true)]
	private static void HandleBulletDamage(NetworkMessage netMsg)
	{
		NetworkReader reader = netMsg.reader;
		GameObject gameObject = reader.ReadGameObject();
		DamageInfo damageInfo = reader.ReadDamageInfo();
		if (reader.ReadBoolean() && (bool)gameObject)
		{
			HealthComponent component = gameObject.GetComponent<HealthComponent>();
			if ((bool)component)
			{
				component.TakeDamage(damageInfo);
			}
			GlobalEventManager.instance.OnHitEnemy(damageInfo, gameObject);
		}
		GlobalEventManager.instance.OnHitAll(damageInfo, gameObject);
	}
}
