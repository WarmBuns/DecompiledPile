using System.Collections.Generic;
using RoR2.Networking;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

public class ProjectileManager : MonoBehaviour
{
	private struct ProjectileQueueItem
	{
		public FireProjectileInfo fireProjectileInfo;

		public NetworkConnection clientAuthorityOwner;

		public NetworkClient localNetworkClient;

		public ushort predictionId;

		public double fireSendTime;

		public int timeCachedFrameStamp;

		public Vector3 positionOffset;

		public bool ownerStartedAsNull;

		public ProjectileQueueItem(FireProjectileInfo fireProjectileInfo, int currentFrame, NetworkConnection clientAuthorityOwner = null, ushort predictionId = 0, double fireSendTime = 0.0)
		{
			this.fireProjectileInfo = fireProjectileInfo;
			ownerStartedAsNull = fireProjectileInfo.owner == null;
			timeCachedFrameStamp = currentFrame;
			this.clientAuthorityOwner = clientAuthorityOwner;
			this.predictionId = predictionId;
			this.fireSendTime = fireSendTime;
			localNetworkClient = null;
			if (!ownerStartedAsNull && fireProjectileInfo.owner.TryGetComponent<CharacterBody>(out var component))
			{
				positionOffset = component.corePosition - fireProjectileInfo.position;
			}
			else
			{
				positionOffset = Vector3.zero;
			}
		}

		public ProjectileQueueItem(FireProjectileInfo fireProjectileInfo, NetworkClient serverClient = null, double fireSendTime = 0.0)
		{
			this.fireProjectileInfo = fireProjectileInfo;
			ownerStartedAsNull = fireProjectileInfo.owner == null;
			timeCachedFrameStamp = 0;
			clientAuthorityOwner = null;
			predictionId = 0;
			this.fireSendTime = fireSendTime;
			localNetworkClient = serverClient;
			if (!ownerStartedAsNull && fireProjectileInfo.owner.TryGetComponent<CharacterBody>(out var component))
			{
				positionOffset = component.corePosition - fireProjectileInfo.position;
			}
			else
			{
				positionOffset = Vector3.zero;
			}
		}

		public void TryAdjustingFiringPosition()
		{
			if (!ownerStartedAsNull && !(fireProjectileInfo.owner == null) && fireProjectileInfo.owner.TryGetComponent<CharacterBody>(out var component))
			{
				fireProjectileInfo.position = component.corePosition + positionOffset;
			}
		}
	}

	private class PlayerFireProjectileMessage : MessageBase
	{
		public double sendTime;

		public uint prefabIndexPlusOne;

		public Vector3 position;

		public Quaternion rotation;

		public GameObject owner;

		public HurtBoxReference target;

		public float damage;

		public float force;

		public bool crit;

		public ushort predictionId;

		public DamageColorIndex damageColorIndex;

		public float speedOverride;

		public float fuseOverride;

		public bool useDamageTypeOverride;

		public DamageTypeCombo damageTypeOverride;

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write(sendTime);
			writer.WritePackedUInt32(prefabIndexPlusOne);
			writer.Write(position);
			writer.Write(rotation);
			writer.Write(owner);
			GeneratedNetworkCode._WriteHurtBoxReference_None(writer, target);
			writer.Write(damage);
			writer.Write(force);
			writer.Write(crit);
			writer.WritePackedUInt32(predictionId);
			writer.Write((int)damageColorIndex);
			writer.Write(speedOverride);
			writer.Write(fuseOverride);
			writer.Write(useDamageTypeOverride);
			GeneratedNetworkCode._WriteDamageTypeCombo_None(writer, damageTypeOverride);
		}

		public override void Deserialize(NetworkReader reader)
		{
			sendTime = reader.ReadDouble();
			prefabIndexPlusOne = reader.ReadPackedUInt32();
			position = reader.ReadVector3();
			rotation = reader.ReadQuaternion();
			owner = reader.ReadGameObject();
			target = GeneratedNetworkCode._ReadHurtBoxReference_None(reader);
			damage = reader.ReadSingle();
			force = reader.ReadSingle();
			crit = reader.ReadBoolean();
			predictionId = (ushort)reader.ReadPackedUInt32();
			damageColorIndex = (DamageColorIndex)reader.ReadInt32();
			speedOverride = reader.ReadSingle();
			fuseOverride = reader.ReadSingle();
			useDamageTypeOverride = reader.ReadBoolean();
			damageTypeOverride = GeneratedNetworkCode._ReadDamageTypeCombo_None(reader);
		}
	}

	private class ReleasePredictionIdMessage : MessageBase
	{
		public ushort predictionId;

		public override void Serialize(NetworkWriter writer)
		{
			writer.WritePackedUInt32(predictionId);
		}

		public override void Deserialize(NetworkReader reader)
		{
			predictionId = (ushort)reader.ReadPackedUInt32();
		}
	}

	private class PredictionManager
	{
		private Dictionary<ushort, ProjectileController> predictions = new Dictionary<ushort, ProjectileController>();

		public ProjectileController FindPredictedProjectileController(ushort predictionId)
		{
			return predictions[predictionId];
		}

		public void OnAuthorityProjectileReceived(ProjectileController authoritativeProjectile)
		{
			if (authoritativeProjectile.hasAuthority && authoritativeProjectile.predictionId != 0 && predictions.TryGetValue(authoritativeProjectile.predictionId, out var value))
			{
				authoritativeProjectile.ghost = value.ghost;
				if ((bool)authoritativeProjectile.ghost)
				{
					authoritativeProjectile.ghost.authorityTransform = authoritativeProjectile.transform;
				}
				authoritativeProjectile.DispatchOnInitialized();
			}
		}

		public void ReleasePredictionId(ushort predictionId)
		{
			ProjectileController projectileController = predictions[predictionId];
			predictions.Remove(predictionId);
			if ((bool)projectileController && (bool)projectileController.gameObject)
			{
				Object.Destroy(projectileController.gameObject);
			}
		}

		public void RegisterPrediction(ProjectileController predictedProjectile)
		{
			predictedProjectile.NetworkpredictionId = RequestPredictionId();
			predictions[predictedProjectile.predictionId] = predictedProjectile;
			predictedProjectile.isPrediction = true;
		}

		private ushort RequestPredictionId()
		{
			for (ushort num = 1; num < 32767; num++)
			{
				if (!predictions.ContainsKey(num))
				{
					return num;
				}
			}
			return 0;
		}
	}

	private PredictionManager predictionManager;

	private static Queue<ProjectileQueueItem> queuedProjectiles = new Queue<ProjectileQueueItem>(150);

	private static int _projectileCurrentFrame;

	private static int totalProjectilesFiredThisFrame;

	private static int maximumProjectilesFiredPerFrame = 20;

	private static int totalQueuedProjectilesAllowedPerFrame = 5;

	private static double projectileExpirationLifetime = 5.0;

	private PlayerFireProjectileMessage fireMsg = new PlayerFireProjectileMessage();

	private ReleasePredictionIdMessage releasePredictionIdMsg = new ReleasePredictionIdMessage();

	public static ProjectileManager instance { get; private set; }

	private static int projectileCurrentFrame
	{
		get
		{
			return _projectileCurrentFrame;
		}
		set
		{
			if (value > _projectileCurrentFrame)
			{
				_projectileCurrentFrame = value;
				totalProjectilesFiredThisFrame = 0;
			}
		}
	}

	private static void CacheProjectileForNextFrameServer(FireProjectileInfo fireProjectileInfo, NetworkConnection clientAuthorityOwner = null, ushort predictionId = 0, double fastForwardTime = 0.0)
	{
		ProjectileQueueItem item = new ProjectileQueueItem(fireProjectileInfo, projectileCurrentFrame, clientAuthorityOwner, predictionId, (double)Run.instance.time - fastForwardTime);
		queuedProjectiles.Enqueue(item);
	}

	private static void CacheProjectileForNextFrameClient(FireProjectileInfo fireProjectileInfo, NetworkClient client = null, double fireTime = 0.0)
	{
		ProjectileQueueItem item = new ProjectileQueueItem(fireProjectileInfo, client, fireTime);
		queuedProjectiles.Enqueue(item);
	}

	private static bool CanAffordToFireProjectile()
	{
		return totalProjectilesFiredThisFrame < maximumProjectilesFiredPerFrame;
	}

	private bool ShouldSkipFiringProjectile(double currentRunTime, ProjectileQueueItem nextItem)
	{
		if (!(currentRunTime - nextItem.fireSendTime > projectileExpirationLifetime))
		{
			if (!nextItem.ownerStartedAsNull)
			{
				return nextItem.fireProjectileInfo.owner == null;
			}
			return false;
		}
		return true;
	}

	private void ClientFireQueuedProjectiles()
	{
		double currentRunTime = Run.instance.time;
		int num = 0;
		int num2 = 0;
		num2 = 0;
		while (totalProjectilesFiredThisFrame < maximumProjectilesFiredPerFrame && queuedProjectiles.Count > 0 && num2 < totalQueuedProjectilesAllowedPerFrame)
		{
			ProjectileQueueItem nextItem = queuedProjectiles.Dequeue();
			if (ShouldSkipFiringProjectile(currentRunTime, nextItem))
			{
				num++;
				num2--;
			}
			else
			{
				nextItem.TryAdjustingFiringPosition();
				FireProjectileClient(nextItem.fireProjectileInfo, nextItem.localNetworkClient, nextItem.fireSendTime);
			}
			num2++;
		}
		_ = 0;
		_ = 0;
	}

	private void ServerFireQueuedProjectiles()
	{
		double num = Run.instance.time;
		int num2 = 0;
		int num3 = 0;
		num3 = 0;
		while (totalProjectilesFiredThisFrame < maximumProjectilesFiredPerFrame && queuedProjectiles.Count > 0 && num3 < totalQueuedProjectilesAllowedPerFrame)
		{
			ProjectileQueueItem nextItem = queuedProjectiles.Dequeue();
			if (ShouldSkipFiringProjectile(num, nextItem))
			{
				num2++;
				num3--;
			}
			else
			{
				nextItem.TryAdjustingFiringPosition();
				FireProjectileServer(nextItem.fireProjectileInfo, nextItem.clientAuthorityOwner, nextItem.predictionId, num - nextItem.fireSendTime);
			}
			num3++;
		}
		_ = 0;
		_ = 0;
	}

	private void Awake()
	{
		predictionManager = new PredictionManager();
	}

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
	}

	private void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
	}

	[NetworkMessageHandler(msgType = 49, server = true)]
	private static void HandlePlayerFireProjectile(NetworkMessage netMsg)
	{
		if ((bool)instance)
		{
			instance.HandlePlayerFireProjectileInternal(netMsg);
		}
	}

	[NetworkMessageHandler(msgType = 50, client = true)]
	private static void HandleReleaseProjectilePredictionId(NetworkMessage netMsg)
	{
		if ((bool)instance)
		{
			instance.HandleReleaseProjectilePredictionIdInternal(netMsg);
		}
	}

	public void FireProjectile(GameObject prefab, Vector3 position, Quaternion rotation, GameObject owner, float damage, float force, bool crit, DamageColorIndex damageColorIndex = DamageColorIndex.Default, GameObject target = null, float speedOverride = -1f)
	{
		FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
		fireProjectileInfo.projectilePrefab = prefab;
		fireProjectileInfo.position = position;
		fireProjectileInfo.rotation = rotation;
		fireProjectileInfo.owner = owner;
		fireProjectileInfo.damage = damage;
		fireProjectileInfo.force = force;
		fireProjectileInfo.crit = crit;
		fireProjectileInfo.damageColorIndex = damageColorIndex;
		fireProjectileInfo.target = target;
		fireProjectileInfo.speedOverride = speedOverride;
		fireProjectileInfo.fuseOverride = -1f;
		fireProjectileInfo.damageTypeOverride = null;
		FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
		FireProjectile(fireProjectileInfo2);
	}

	public void FireProjectile(FireProjectileInfo fireProjectileInfo)
	{
		if (NetworkServer.active)
		{
			FireProjectileServer(fireProjectileInfo, null, 0);
		}
		else
		{
			FireProjectileClient(fireProjectileInfo, NetworkManager.singleton.client);
		}
	}

	private void FireProjectileClient(FireProjectileInfo fireProjectileInfo, NetworkClient client, double fireTime = -1.0)
	{
		if (fireTime == -1.0)
		{
			fireTime = Run.instance.time;
		}
		int projectileIndex = ProjectileCatalog.GetProjectileIndex(fireProjectileInfo.projectilePrefab);
		if (projectileIndex == -1)
		{
			Debug.LogErrorFormat(fireProjectileInfo.projectilePrefab, "Prefab {0} is not a registered projectile prefab.", fireProjectileInfo.projectilePrefab);
			return;
		}
		bool allowPrediction = ProjectileCatalog.GetProjectilePrefabProjectileControllerComponent(projectileIndex).allowPrediction;
		ushort predictionId = 0;
		if (allowPrediction)
		{
			ProjectileController component = Object.Instantiate(fireProjectileInfo.projectilePrefab, fireProjectileInfo.position, fireProjectileInfo.rotation).GetComponent<ProjectileController>();
			InitializeProjectile(component, fireProjectileInfo);
			predictionManager.RegisterPrediction(component);
			predictionId = component.predictionId;
		}
		fireMsg.sendTime = fireTime;
		fireMsg.prefabIndexPlusOne = Util.IntToUintPlusOne(projectileIndex);
		fireMsg.position = fireProjectileInfo.position;
		fireMsg.rotation = fireProjectileInfo.rotation;
		fireMsg.owner = fireProjectileInfo.owner;
		fireMsg.predictionId = predictionId;
		fireMsg.damage = fireProjectileInfo.damage;
		fireMsg.force = fireProjectileInfo.force;
		fireMsg.crit = fireProjectileInfo.crit;
		fireMsg.damageColorIndex = fireProjectileInfo.damageColorIndex;
		fireMsg.speedOverride = fireProjectileInfo.speedOverride;
		fireMsg.fuseOverride = fireProjectileInfo.fuseOverride;
		fireMsg.useDamageTypeOverride = fireProjectileInfo.damageTypeOverride.HasValue;
		fireMsg.damageTypeOverride = fireProjectileInfo.damageTypeOverride ?? ((DamageTypeCombo)DamageType.Generic);
		if ((bool)fireProjectileInfo.target)
		{
			HurtBox component2 = fireProjectileInfo.target.GetComponent<HurtBox>();
			fireMsg.target = (component2 ? HurtBoxReference.FromHurtBox(component2) : HurtBoxReference.FromRootObject(fireProjectileInfo.target));
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.StartMessage(49);
		networkWriter.Write(fireMsg);
		networkWriter.FinishMessage();
		client.SendWriter(networkWriter, 0);
	}

	private static void InitializeProjectile(ProjectileController projectileController, FireProjectileInfo fireProjectileInfo)
	{
		GameObject gameObject = projectileController.gameObject;
		ProjectileDamage component = gameObject.GetComponent<ProjectileDamage>();
		TeamFilter component2 = gameObject.GetComponent<TeamFilter>();
		ProjectileNetworkTransform component3 = gameObject.GetComponent<ProjectileNetworkTransform>();
		ProjectileTargetComponent component4 = gameObject.GetComponent<ProjectileTargetComponent>();
		ProjectileSimple component5 = gameObject.GetComponent<ProjectileSimple>();
		projectileController.Networkowner = fireProjectileInfo.owner;
		projectileController.procChainMask = fireProjectileInfo.procChainMask;
		if ((bool)component2)
		{
			component2.teamIndex = TeamComponent.GetObjectTeam(fireProjectileInfo.owner);
		}
		if ((bool)component3)
		{
			component3.SetValuesFromTransform();
		}
		if ((bool)component4 && (bool)fireProjectileInfo.target)
		{
			CharacterBody component6 = fireProjectileInfo.target.GetComponent<CharacterBody>();
			component4.target = (component6 ? component6.coreTransform : fireProjectileInfo.target.transform);
		}
		if (fireProjectileInfo.useSpeedOverride && (bool)component5)
		{
			component5.desiredForwardSpeed = fireProjectileInfo.speedOverride;
		}
		if (fireProjectileInfo.useFuseOverride)
		{
			ProjectileImpactExplosion component7 = gameObject.GetComponent<ProjectileImpactExplosion>();
			if ((bool)component7)
			{
				component7.lifetime = fireProjectileInfo.fuseOverride;
			}
			ProjectileFuse component8 = gameObject.GetComponent<ProjectileFuse>();
			if ((bool)component8)
			{
				component8.fuse = fireProjectileInfo.fuseOverride;
			}
		}
		if ((bool)component)
		{
			component.damage = fireProjectileInfo.damage;
			component.force = fireProjectileInfo.force;
			component.crit = fireProjectileInfo.crit;
			component.damageColorIndex = fireProjectileInfo.damageColorIndex;
			if (fireProjectileInfo.damageTypeOverride.HasValue)
			{
				component.damageType = fireProjectileInfo.damageTypeOverride.Value;
			}
		}
		projectileController.DispatchOnInitialized();
	}

	private void FireProjectileServer(FireProjectileInfo fireProjectileInfo, NetworkConnection clientAuthorityOwner = null, ushort predictionId = 0, double fastForwardTime = 0.0)
	{
		GameObject gameObject = Object.Instantiate(fireProjectileInfo.projectilePrefab, fireProjectileInfo.position, fireProjectileInfo.rotation);
		ProjectileController component = gameObject.GetComponent<ProjectileController>();
		component.NetworkpredictionId = predictionId;
		InitializeProjectile(component, fireProjectileInfo);
		NetworkIdentity component2 = gameObject.GetComponent<NetworkIdentity>();
		if (clientAuthorityOwner != null && component2.localPlayerAuthority)
		{
			NetworkServer.SpawnWithClientAuthority(gameObject, clientAuthorityOwner);
		}
		else
		{
			NetworkServer.Spawn(gameObject);
		}
	}

	public void OnServerProjectileDestroyed(ProjectileController projectile)
	{
		if (projectile.predictionId != 0)
		{
			NetworkConnection clientAuthorityOwner = projectile.clientAuthorityOwner;
			if (clientAuthorityOwner != null)
			{
				ReleasePredictionId(clientAuthorityOwner, projectile.predictionId);
			}
		}
	}

	public void OnClientProjectileReceived(ProjectileController projectile)
	{
		if (projectile.predictionId != 0 && projectile.hasAuthority)
		{
			predictionManager.OnAuthorityProjectileReceived(projectile);
		}
	}

	private void ReleasePredictionId(NetworkConnection owner, ushort predictionId)
	{
		releasePredictionIdMsg.predictionId = predictionId;
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.StartMessage(50);
		networkWriter.Write(releasePredictionIdMsg);
		networkWriter.FinishMessage();
		owner.SendWriter(networkWriter, 0);
	}

	private void HandlePlayerFireProjectileInternal(NetworkMessage netMsg)
	{
		netMsg.ReadMessage(fireMsg);
		GameObject projectilePrefab = ProjectileCatalog.GetProjectilePrefab(Util.UintToIntMinusOne(fireMsg.prefabIndexPlusOne));
		if (projectilePrefab == null)
		{
			ReleasePredictionId(netMsg.conn, fireMsg.predictionId);
			return;
		}
		FireProjectileServer(new FireProjectileInfo
		{
			projectilePrefab = projectilePrefab,
			position = fireMsg.position,
			rotation = fireMsg.rotation,
			owner = fireMsg.owner,
			damage = fireMsg.damage,
			force = fireMsg.force,
			crit = fireMsg.crit,
			target = fireMsg.target.ResolveGameObject(),
			damageColorIndex = fireMsg.damageColorIndex,
			speedOverride = fireMsg.speedOverride,
			fuseOverride = fireMsg.fuseOverride,
			damageTypeOverride = (fireMsg.useDamageTypeOverride ? new DamageTypeCombo?(fireMsg.damageTypeOverride) : null)
		}, netMsg.conn, fireMsg.predictionId, (double)Run.instance.time - fireMsg.sendTime);
	}

	private void HandleReleaseProjectilePredictionIdInternal(NetworkMessage netMsg)
	{
		netMsg.ReadMessage(releasePredictionIdMsg);
		predictionManager.ReleasePredictionId(releasePredictionIdMsg.predictionId);
	}
}
