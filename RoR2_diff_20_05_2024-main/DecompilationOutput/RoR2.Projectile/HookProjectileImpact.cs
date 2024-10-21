using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class HookProjectileImpact : NetworkBehaviour, IProjectileImpactBehavior
{
	private enum HookState
	{
		Flying,
		HitDelay,
		Reel,
		ReelFail
	}

	private ProjectileController projectileController;

	public float reelDelayTime;

	public float reelSpeed = 40f;

	public string attachmentString;

	public float victimPullFactor = 1f;

	public float pullMinimumDistance = 10f;

	public GameObject impactSpark;

	public GameObject impactSuccess;

	[SyncVar]
	private HookState hookState;

	[SyncVar]
	private GameObject victim;

	private Transform ownerTransform;

	private ProjectileDamage projectileDamage;

	private Rigidbody rigidbody;

	public float liveTimer;

	private float delayTimer;

	private float flyTimer;

	private NetworkInstanceId ___victimNetId;

	public HookState NetworkhookState
	{
		get
		{
			return hookState;
		}
		[param: In]
		set
		{
			ulong newValueAsUlong = (ulong)value;
			ulong fieldValueAsUlong = (ulong)hookState;
			SetSyncVarEnum(value, newValueAsUlong, ref hookState, fieldValueAsUlong, 1u);
		}
	}

	public GameObject Networkvictim
	{
		get
		{
			return victim;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref victim, 2u, ref ___victimNetId);
		}
	}

	private void Start()
	{
		rigidbody = GetComponent<Rigidbody>();
		projectileController = GetComponent<ProjectileController>();
		projectileDamage = GetComponent<ProjectileDamage>();
		ownerTransform = projectileController.owner.transform;
		if (!ownerTransform)
		{
			return;
		}
		ModelLocator component = ownerTransform.GetComponent<ModelLocator>();
		if (!component)
		{
			return;
		}
		Transform modelTransform = component.modelTransform;
		if ((bool)modelTransform)
		{
			ChildLocator component2 = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component2)
			{
				ownerTransform = component2.FindChild(attachmentString);
			}
		}
	}

	public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
	{
		EffectManager.SimpleImpactEffect(impactSpark, impactInfo.estimatedPointOfImpact, -base.transform.forward, transmit: true);
		if (hookState != 0)
		{
			return;
		}
		HurtBox component = impactInfo.collider.GetComponent<HurtBox>();
		if ((bool)component)
		{
			HealthComponent healthComponent = component.healthComponent;
			if ((bool)healthComponent)
			{
				TeamIndex teamIndex = projectileController.teamFilter.teamIndex;
				if (!FriendlyFireManager.ShouldDirectHitProceed(healthComponent, teamIndex))
				{
					return;
				}
				Networkvictim = healthComponent.gameObject;
				DamageInfo damageInfo = new DamageInfo();
				if ((bool)projectileDamage)
				{
					damageInfo.damage = projectileDamage.damage;
					damageInfo.crit = projectileDamage.crit;
					damageInfo.attacker = (projectileController.owner ? projectileController.owner.gameObject : null);
					damageInfo.inflictor = base.gameObject;
					damageInfo.position = impactInfo.estimatedPointOfImpact;
					damageInfo.force = projectileDamage.force * base.transform.forward;
					damageInfo.procChainMask = projectileController.procChainMask;
					damageInfo.procCoefficient = projectileController.procCoefficient;
					damageInfo.damageColorIndex = projectileDamage.damageColorIndex;
				}
				healthComponent.TakeDamage(damageInfo);
				GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
				NetworkhookState = HookState.HitDelay;
				EffectManager.SimpleImpactEffect(impactSuccess, impactInfo.estimatedPointOfImpact, -base.transform.forward, transmit: true);
				base.gameObject.layer = LayerIndex.noCollision.intVal;
			}
		}
		if (!victim)
		{
			NetworkhookState = HookState.ReelFail;
		}
	}

	private bool Reel()
	{
		Vector3 vector = ownerTransform.position - victim.transform.position;
		Vector3 normalized = vector.normalized;
		float num = vector.magnitude;
		Collider component = projectileController.owner.GetComponent<Collider>();
		Collider component2 = victim.GetComponent<Collider>();
		if ((bool)component && (bool)component2)
		{
			num = Util.EstimateSurfaceDistance(component, component2);
		}
		bool flag = num <= pullMinimumDistance;
		CharacterMotor characterMotor = null;
		Rigidbody rigidbody = null;
		float num2 = -1f;
		characterMotor = projectileController.owner.GetComponent<CharacterMotor>();
		if ((bool)characterMotor)
		{
			num2 = characterMotor.mass;
		}
		else
		{
			rigidbody = projectileController.owner.GetComponent<Rigidbody>();
			if ((bool)rigidbody)
			{
				num2 = rigidbody.mass;
			}
		}
		CharacterMotor characterMotor2 = null;
		Rigidbody rigidbody2 = null;
		float num3 = -1f;
		characterMotor2 = victim.GetComponent<CharacterMotor>();
		if ((bool)characterMotor2)
		{
			num3 = characterMotor2.mass;
		}
		else
		{
			rigidbody2 = victim.GetComponent<Rigidbody>();
			if ((bool)rigidbody2)
			{
				num3 = rigidbody2.mass;
			}
		}
		float num4 = 0f;
		float num5 = 0f;
		if (num2 > 0f && num3 > 0f)
		{
			num4 = 1f - num2 / (num2 + num3);
			num5 = 1f - num4;
		}
		else if (num2 > 0f)
		{
			num4 = 1f;
		}
		else if (num3 > 0f)
		{
			num5 = 1f;
		}
		else
		{
			flag = true;
		}
		if (flag)
		{
			num4 = 0f;
			num5 = 0f;
		}
		Vector3 velocity = normalized * (num5 * victimPullFactor * reelSpeed);
		if ((bool)characterMotor2)
		{
			characterMotor2.velocity = velocity;
		}
		if ((bool)rigidbody2)
		{
			rigidbody2.velocity = velocity;
		}
		return flag;
	}

	public void FixedUpdate()
	{
		if (NetworkServer.active && !projectileController.owner)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		if ((bool)victim)
		{
			rigidbody.MovePosition(victim.transform.position);
		}
		switch (hookState)
		{
		case HookState.Flying:
			if (NetworkServer.active)
			{
				flyTimer += Time.fixedDeltaTime;
				if (flyTimer >= liveTimer)
				{
					NetworkhookState = HookState.ReelFail;
				}
			}
			break;
		case HookState.HitDelay:
			if (!NetworkServer.active)
			{
				break;
			}
			if ((bool)victim)
			{
				delayTimer += Time.fixedDeltaTime;
				if (delayTimer >= reelDelayTime)
				{
					NetworkhookState = HookState.Reel;
				}
			}
			else
			{
				NetworkhookState = HookState.Reel;
			}
			break;
		case HookState.Reel:
		{
			bool flag = true;
			if ((bool)victim)
			{
				flag = Reel();
			}
			if (NetworkServer.active)
			{
				if (!victim)
				{
					NetworkhookState = HookState.ReelFail;
				}
				if (flag)
				{
					Object.Destroy(base.gameObject);
				}
			}
			break;
		}
		case HookState.ReelFail:
			if (!NetworkServer.active)
			{
				break;
			}
			if ((bool)rigidbody)
			{
				rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
				rigidbody.isKinematic = true;
			}
			if ((bool)ownerTransform)
			{
				rigidbody.MovePosition(Vector3.MoveTowards(base.transform.position, ownerTransform.position, reelSpeed * Time.fixedDeltaTime));
				if (base.transform.position == ownerTransform.position)
				{
					Object.Destroy(base.gameObject);
				}
			}
			break;
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write((int)hookState);
			writer.Write(victim);
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
			writer.Write((int)hookState);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(victim);
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
			hookState = (HookState)reader.ReadInt32();
			___victimNetId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			hookState = (HookState)reader.ReadInt32();
		}
		if (((uint)num & 2u) != 0)
		{
			victim = reader.ReadGameObject();
		}
	}

	public override void PreStartClient()
	{
		if (!___victimNetId.IsEmpty())
		{
			Networkvictim = ClientScene.FindLocalObject(___victimNetId);
		}
	}
}
