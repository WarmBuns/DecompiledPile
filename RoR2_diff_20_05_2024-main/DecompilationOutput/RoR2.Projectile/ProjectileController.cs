using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RoR2.Audio;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(TeamFilter))]
public class ProjectileController : NetworkBehaviour
{
	[HideInInspector]
	[Tooltip("This is assigned to the prefab automatically by ProjectileCatalog at runtime. Do not set this value manually.")]
	public int catalogIndex = -1;

	[Tooltip("The prefab to instantiate as the visual representation of this projectile. The prefab must have a ProjectileGhostController attached.")]
	public GameObject ghostPrefab;

	[Tooltip("The transform for the ghost to follow. If null, the transform of this object will be used instead.")]
	public Transform ghostTransformAnchor;

	[Tooltip("The sound to play on Start(). Use this field to ensure the sound only plays once when prediction creates two instances.")]
	public string startSound;

	[Tooltip("Prevents this projectile from being deleted by gameplay events, like Captain's defense matrix.")]
	public bool cannotBeDeleted;

	[Tooltip("Check this if you want collision events to only run on the local authority (which may be a client instead of the host). 99% of projectiles should have this set to false.")]
	public bool authorityHandlesCollisionEvents;

	[SerializeField]
	[Tooltip("The sound loop to play while this object exists. Use this field to ensure the sound only plays once when prediction creates two instances.")]
	private LoopSoundDef flightSoundLoop;

	protected Rigidbody rigidbody;

	public bool canImpactOnTrigger;

	public bool allowPrediction = true;

	[NonSerialized]
	[SyncVar]
	public ushort predictionId;

	[HideInInspector]
	[SyncVar]
	public GameObject owner;

	[HideInInspector]
	public bool localCollisionHappened;

	public float procCoefficient = 1f;

	private Collider[] myColliders;

	protected EffectManagerHelper _efhGhost;

	public bool CheckChildrenForCollidersAndIncludeDisabled;

	private NetworkInstanceId ___ownerNetId;

	public TeamFilter teamFilter { get; private set; }

	public ProjectileGhostController ghost { get; set; }

	public bool isPrediction { get; set; }

	public bool shouldPlaySounds { get; set; }

	public ProcChainMask procChainMask { get; set; }

	public NetworkConnection clientAuthorityOwner { get; private set; }

	public ushort NetworkpredictionId
	{
		get
		{
			return predictionId;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref predictionId, 1u);
		}
	}

	public GameObject Networkowner
	{
		get
		{
			return owner;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref owner, 2u, ref ___ownerNetId);
		}
	}

	public event Action<ProjectileController> onInitialized;

	private void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
		teamFilter = GetComponent<TeamFilter>();
		if (CheckChildrenForCollidersAndIncludeDisabled)
		{
			myColliders = GetComponentsInChildren<Collider>(includeInactive: true);
		}
		else
		{
			myColliders = GetComponents<Collider>();
		}
		for (int i = 0; i < myColliders.Length; i++)
		{
			myColliders[i].enabled = false;
		}
	}

	private void Start()
	{
		for (int i = 0; i < myColliders.Length; i++)
		{
			myColliders[i].enabled = true;
		}
		IgnoreCollisionsWithOwner(shouldIgnore: true);
		if (!isPrediction && !NetworkServer.active)
		{
			ProjectileManager.instance.OnClientProjectileReceived(this);
		}
		GameObject gameObject = ProjectileGhostReplacementManager.FindProjectileGhostPrefab(this);
		shouldPlaySounds = false;
		if (isPrediction || !allowPrediction || !base.hasAuthority)
		{
			shouldPlaySounds = true;
			if ((bool)gameObject)
			{
				Transform transform = base.transform;
				if ((bool)ghostTransformAnchor)
				{
					transform = ghostTransformAnchor;
				}
				if (!EffectManager.ShouldUsePooledEffect(gameObject))
				{
					ghost = UnityEngine.Object.Instantiate(gameObject, transform.position, transform.rotation).GetComponent<ProjectileGhostController>();
				}
				else
				{
					_efhGhost = EffectManager.GetAndActivatePooledEffect(gameObject, transform.position, transform.rotation);
					ghost = _efhGhost.gameObject.GetComponent<ProjectileGhostController>();
				}
				if (isPrediction)
				{
					ghost.predictionTransform = transform;
				}
				else
				{
					ghost.authorityTransform = transform;
				}
				ghost.enabled = true;
			}
		}
		clientAuthorityOwner = GetComponent<NetworkIdentity>().clientAuthorityOwner;
		if (shouldPlaySounds)
		{
			PointSoundManager.EmitSoundLocal((AkEventIdArg)startSound, base.transform.position);
			if ((bool)flightSoundLoop)
			{
				Util.PlaySound(flightSoundLoop.startSoundName, base.gameObject);
			}
		}
	}

	private void OnDestroy()
	{
		if (NetworkServer.active && ProjectileManager.instance != null)
		{
			ProjectileManager.instance.OnServerProjectileDestroyed(this);
		}
		if (shouldPlaySounds && (bool)flightSoundLoop)
		{
			Util.PlaySound(flightSoundLoop.stopSoundName, base.gameObject);
		}
		DisconnectFromGhost();
	}

	public void DisconnectFromGhost()
	{
		if (!ghost || (isPrediction && (bool)ghost.authorityTransform))
		{
			return;
		}
		RunGhostParentProjectileDestroyedMethod();
		if (!EffectManager.UsePools)
		{
			UnityEngine.Object.Destroy(ghost.gameObject);
		}
		else
		{
			if (!isPrediction && _efhGhost == null)
			{
				_efhGhost = ghost.emh;
			}
			if (_efhGhost != null && _efhGhost.OwningPool != null)
			{
				if (!_efhGhost.HasDestroyOnTimer && !_efhGhost.HasAnimateShaderAlphas)
				{
					_efhGhost.OwningPool.ReturnObject(_efhGhost);
				}
			}
			else
			{
				if (_efhGhost != null)
				{
					Debug.LogFormat("efhGhost has no owning pool {0} {1}", _efhGhost.gameObject.name, _efhGhost.gameObject.GetInstanceID());
				}
				UnityEngine.Object.Destroy(ghost.gameObject);
			}
		}
		_efhGhost = null;
		ghost = null;
	}

	public void RunGhostParentProjectileDestroyedMethod()
	{
		if ((bool)ghost)
		{
			ghost.OnParentProjectileDestroyed();
		}
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
		IgnoreCollisionsWithOwner(shouldIgnore: true);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	public void IgnoreCollisionsWithOwner(bool shouldIgnore)
	{
		if (!owner)
		{
			return;
		}
		ModelLocator component = owner.GetComponent<ModelLocator>();
		if (!component)
		{
			return;
		}
		Transform modelTransform = component.modelTransform;
		if (!modelTransform)
		{
			return;
		}
		HurtBoxGroup component2 = modelTransform.GetComponent<HurtBoxGroup>();
		if (!component2)
		{
			return;
		}
		HurtBox[] hurtBoxes = component2.hurtBoxes;
		for (int i = 0; i < hurtBoxes.Length; i++)
		{
			List<Collider> gameObjectComponents = GetComponentsCache<Collider>.GetGameObjectComponents(hurtBoxes[i].gameObject);
			int j = 0;
			for (int count = gameObjectComponents.Count; j < count; j++)
			{
				Collider collider = gameObjectComponents[j];
				for (int k = 0; k < myColliders.Length; k++)
				{
					Collider collider2 = myColliders[k];
					Physics.IgnoreCollision(collider, collider2, shouldIgnore);
				}
			}
			GetComponentsCache<Collider>.ReturnBuffer(gameObjectComponents);
		}
	}

	private static Vector3 EstimateContactPoint(ContactPoint[] contacts, Collider collider)
	{
		if (contacts.Length == 0)
		{
			return collider.transform.position;
		}
		return contacts[0].point;
	}

	private static Vector3 EstimateContactNormal(ContactPoint[] contacts)
	{
		if (contacts.Length == 0)
		{
			return Vector3.zero;
		}
		return contacts[0].normal;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool CanProcessCollisionEvents()
	{
		if ((authorityHandlesCollisionEvents || !NetworkServer.active) && (!authorityHandlesCollisionEvents || !Util.HasEffectiveAuthority(base.gameObject)))
		{
			return isPrediction;
		}
		return true;
	}

	public void OnCollisionEnter(Collision collision)
	{
		localCollisionHappened = true;
		if (CanProcessCollisionEvents())
		{
			ContactPoint[] contacts = collision.contacts;
			ProjectileImpactInfo projectileImpactInfo = default(ProjectileImpactInfo);
			projectileImpactInfo.collider = collision.collider;
			projectileImpactInfo.estimatedPointOfImpact = EstimateContactPoint(contacts, collision.collider);
			projectileImpactInfo.estimatedImpactNormal = EstimateContactNormal(contacts);
			ProjectileImpactInfo impactInfo = projectileImpactInfo;
			IProjectileImpactBehavior[] components = GetComponents<IProjectileImpactBehavior>();
			for (int i = 0; i < components.Length; i++)
			{
				components[i].OnProjectileImpact(impactInfo);
			}
		}
	}

	public void DispatchOnInitialized()
	{
		this.onInitialized?.Invoke(this);
	}

	private void OnValidate()
	{
		if (!Application.IsPlaying(this))
		{
			bool flag = GetComponent<NetworkIdentity>().localPlayerAuthority;
			if (allowPrediction && !flag)
			{
				Debug.LogWarningFormat(base.gameObject, "ProjectileController: {0} allows predictions, so it should have localPlayerAuthority=true", base.gameObject);
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt32(predictionId);
			writer.Write(owner);
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
			writer.WritePackedUInt32(predictionId);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(owner);
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
			predictionId = (ushort)reader.ReadPackedUInt32();
			___ownerNetId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			predictionId = (ushort)reader.ReadPackedUInt32();
		}
		if (((uint)num & 2u) != 0)
		{
			owner = reader.ReadGameObject();
		}
	}

	public override void PreStartClient()
	{
		if (!___ownerNetId.IsEmpty())
		{
			Networkowner = ClientScene.FindLocalObject(___ownerNetId);
		}
	}
}
