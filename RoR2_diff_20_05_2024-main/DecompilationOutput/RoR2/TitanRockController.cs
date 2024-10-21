using System.Runtime.InteropServices;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class TitanRockController : NetworkBehaviour
{
	[Tooltip("The child transform from which projectiles will be fired.")]
	public Transform fireTransform;

	[Tooltip("How long it takes to start firing.")]
	public float startDelay = 4f;

	[Tooltip("Firing interval.")]
	public float fireInterval = 1f;

	[Tooltip("The prefab to fire as a projectile.")]
	public GameObject projectilePrefab;

	[Tooltip("The damage coefficient to multiply by the owner's damage stat for the projectile's final damage value.")]
	public float damageCoefficient;

	[Tooltip("The force of the projectile's damage.")]
	public float damageForce;

	[SyncVar(hook = "SetOwner")]
	private GameObject owner;

	private Transform targetTransform;

	private Vector3 velocity;

	private static readonly Vector3 targetLocalPosition = new Vector3(0f, 12f, -3f);

	private float fireTimer;

	private InputBankTest ownerInputBank;

	private CharacterBody ownerCharacterBody;

	private bool isCrit;

	private bool foundOwner;

	private NetworkInstanceId ___ownerNetId;

	public GameObject Networkowner
	{
		get
		{
			return owner;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetOwner(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVarGameObject(value, ref owner, 1u, ref ___ownerNetId);
		}
	}

	private void Start()
	{
		if (!NetworkServer.active)
		{
			SetOwner(owner);
		}
		else
		{
			fireTimer = startDelay;
		}
	}

	public void SetOwner(GameObject newOwner)
	{
		ownerInputBank = null;
		ownerCharacterBody = null;
		isCrit = false;
		Networkowner = newOwner;
		if (!owner)
		{
			return;
		}
		ownerInputBank = owner.GetComponent<InputBankTest>();
		ownerCharacterBody = owner.GetComponent<CharacterBody>();
		ModelLocator component = owner.GetComponent<ModelLocator>();
		if ((bool)component)
		{
			Transform modelTransform = component.modelTransform;
			if ((bool)modelTransform)
			{
				ChildLocator component2 = modelTransform.GetComponent<ChildLocator>();
				if ((bool)component2)
				{
					targetTransform = component2.FindChild("Chest");
					if ((bool)targetTransform)
					{
						base.transform.rotation = targetTransform.rotation;
					}
				}
			}
		}
		base.transform.position = owner.transform.position + Vector3.down * 20f;
		if (NetworkServer.active && (bool)ownerCharacterBody)
		{
			CharacterMaster master = ownerCharacterBody.master;
			if ((bool)master)
			{
				isCrit = Util.CheckRoll(ownerCharacterBody.crit, master);
			}
		}
	}

	public void FixedUpdate()
	{
		if ((bool)targetTransform)
		{
			foundOwner = true;
			base.transform.position = Vector3.SmoothDamp(base.transform.position, targetTransform.TransformPoint(targetLocalPosition), ref velocity, 1f);
			base.transform.rotation = targetTransform.rotation;
		}
		else if (foundOwner)
		{
			foundOwner = false;
			ParticleSystem[] componentsInChildren = GetComponentsInChildren<ParticleSystem>();
			foreach (ParticleSystem obj in componentsInChildren)
			{
				ParticleSystem.MainModule main = obj.main;
				main.gravityModifier = 1f;
				ParticleSystem.EmissionModule emission = obj.emission;
				emission.enabled = false;
				ParticleSystem.NoiseModule noise = obj.noise;
				noise.enabled = false;
				ParticleSystem.LimitVelocityOverLifetimeModule limitVelocityOverLifetime = obj.limitVelocityOverLifetime;
				limitVelocityOverLifetime.enabled = false;
			}
			ParticleSystem.CollisionModule collision = base.transform.Find("Debris").GetComponent<ParticleSystem>().collision;
			collision.enabled = true;
			Light[] componentsInChildren2 = GetComponentsInChildren<Light>();
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				componentsInChildren2[i].enabled = false;
			}
			Util.PlaySound("Stop_titanboss_shift_loop", base.gameObject);
		}
		if (NetworkServer.active)
		{
			FixedUpdateServer();
		}
	}

	[Server]
	private void FixedUpdateServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.TitanRockController::FixedUpdateServer()' called on client");
			return;
		}
		fireTimer -= Time.fixedDeltaTime;
		if (fireTimer <= 0f)
		{
			Fire();
			fireTimer += fireInterval;
		}
	}

	[Server]
	private void Fire()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.TitanRockController::Fire()' called on client");
		}
		else if ((bool)ownerInputBank)
		{
			Vector3 position = fireTransform.position;
			Vector3 forward = ownerInputBank.aimDirection;
			if (Util.CharacterRaycast(owner, new Ray(ownerInputBank.aimOrigin, ownerInputBank.aimDirection), out var hitInfo, float.PositiveInfinity, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
			{
				forward = hitInfo.point - position;
			}
			float num = (ownerCharacterBody ? ownerCharacterBody.damage : 1f);
			ProjectileManager.instance.FireProjectile(projectilePrefab, position, Util.QuaternionSafeLookRotation(forward), owner, damageCoefficient * num, damageForce, isCrit);
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
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
			___ownerNetId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			SetOwner(reader.ReadGameObject());
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
