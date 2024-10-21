using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class MapZone : MonoBehaviour
{
	public enum TriggerType
	{
		TriggerExit,
		TriggerEnter
	}

	public enum ZoneType
	{
		OutOfBounds,
		KickOutPlayers
	}

	private struct CollisionInfo : IEquatable<CollisionInfo>
	{
		public readonly MapZone mapZone;

		public readonly Collider otherCollider;

		public CollisionInfo(MapZone mapZone, Collider otherCollider)
		{
			this.mapZone = mapZone;
			this.otherCollider = otherCollider;
		}

		public bool Equals(CollisionInfo other)
		{
			if ((object)mapZone == other.mapZone)
			{
				return (object)otherCollider == other.otherCollider;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is CollisionInfo)
			{
				return Equals((CollisionInfo)obj);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return otherCollider.GetHashCode();
		}
	}

	public TriggerType triggerType;

	public ZoneType zoneType;

	public Transform explicitDestination;

	public GameObject explicitSpawnEffectPrefab;

	public float destinationIdealRadius;

	private Collider collider;

	private readonly List<CollisionInfo> queuedCollisions = new List<CollisionInfo>();

	private static readonly Queue<Collider> collidersToCheckInFixedUpdate;

	public event Action<CharacterBody> onBodyTeleport;

	static MapZone()
	{
		collidersToCheckInFixedUpdate = new Queue<Collider>();
		VehicleSeat.onPassengerExitGlobal += CheckCharacterOnVehicleExit;
		RoR2Application.onFixedUpdate += StaticFixedUpdate;
	}

	private static bool TestColliders(Collider characterCollider, Collider triggerCollider)
	{
		Vector3 direction;
		float distance;
		return Physics.ComputePenetration(characterCollider, characterCollider.transform.position, characterCollider.transform.rotation, triggerCollider, triggerCollider.transform.position, triggerCollider.transform.rotation, out direction, out distance);
	}

	private void Awake()
	{
		collider = GetComponent<Collider>();
	}

	public void OnTriggerEnter(Collider other)
	{
		if (triggerType == TriggerType.TriggerEnter)
		{
			TryZoneStart(other);
		}
		else
		{
			TryZoneEnd(other);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (triggerType == TriggerType.TriggerExit)
		{
			TryZoneStart(other);
		}
		else
		{
			TryZoneEnd(other);
		}
	}

	public bool IsPointInsideMapZone(Vector3 point)
	{
		if (collider == null)
		{
			return true;
		}
		return collider.bounds.Contains(point);
	}

	private void TryZoneStart(Collider other)
	{
		CharacterBody component = other.GetComponent<CharacterBody>();
		if (!component)
		{
			return;
		}
		if ((bool)component.currentVehicle)
		{
			if (component.currentVehicle.GetComponent<SojournVehicle>() == null)
			{
				queuedCollisions.Add(new CollisionInfo(this, other));
				return;
			}
			component.currentVehicle.EjectPassenger();
		}
		TeamComponent teamComponent = component.teamComponent;
		switch (zoneType)
		{
		case ZoneType.OutOfBounds:
		{
			bool flag = false;
			if ((bool)component.inventory)
			{
				flag = component.inventory.GetItemCount(RoR2Content.Items.InvadingDoppelganger) > 0 || component.inventory.GetItemCount(RoR2Content.Items.TeleportWhenOob) > 0;
			}
			if (teamComponent.teamIndex != TeamIndex.Player && !flag)
			{
				if (!NetworkServer.active || Physics.GetIgnoreLayerCollision(base.gameObject.layer, other.gameObject.layer))
				{
					break;
				}
				Debug.LogFormat("Killing {0} for being out of bounds.", component.gameObject);
				HealthComponent healthComponent = component.healthComponent;
				if ((bool)healthComponent)
				{
					if ((bool)component.master)
					{
						component.master.TrueKill(healthComponent.lastHitAttacker, base.gameObject, DamageType.OutOfBounds);
					}
					else
					{
						healthComponent.Suicide(healthComponent.lastHitAttacker, base.gameObject, DamageType.OutOfBounds);
					}
				}
				else if ((bool)component.master)
				{
					component.master.TrueKill(null, null, DamageType.OutOfBounds);
				}
			}
			else
			{
				TeleportBody(component);
			}
			break;
		}
		case ZoneType.KickOutPlayers:
			if (teamComponent.teamIndex == TeamIndex.Player)
			{
				TeleportBody(component);
			}
			break;
		}
	}

	private void TryZoneEnd(Collider other)
	{
		if (queuedCollisions.Count != 0 && queuedCollisions.Contains(new CollisionInfo(this, other)))
		{
			queuedCollisions.Remove(new CollisionInfo(this, other));
		}
	}

	private void ProcessQueuedCollisionsForCollider(Collider collider)
	{
		for (int num = queuedCollisions.Count - 1; num >= 0; num--)
		{
			if ((object)queuedCollisions[num].otherCollider == collider)
			{
				queuedCollisions.RemoveAt(num);
				TryZoneStart(collider);
			}
		}
	}

	private static void StaticFixedUpdate()
	{
		int i = 0;
		for (int count = collidersToCheckInFixedUpdate.Count; i < count; i++)
		{
			Collider collider = collidersToCheckInFixedUpdate.Dequeue();
			if (!collider)
			{
				continue;
			}
			foreach (MapZone instances in InstanceTracker.GetInstancesList<MapZone>())
			{
				instances.ProcessQueuedCollisionsForCollider(collider);
			}
		}
	}

	private static void CheckCharacterOnVehicleExit(VehicleSeat vehicleSeat, GameObject passengerObject)
	{
		Collider component = passengerObject.GetComponent<Collider>();
		collidersToCheckInFixedUpdate.Enqueue(component);
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	private void TeleportBody(CharacterBody characterBody)
	{
		if (Util.HasEffectiveAuthority(characterBody.gameObject) && !Physics.GetIgnoreLayerCollision(base.gameObject.layer, characterBody.gameObject.layer))
		{
			Vector3 vector = Run.instance.FindSafeTeleportPosition(characterBody, explicitDestination, 0f, destinationIdealRadius);
			TeleportHelper.TeleportBody(characterBody, vector);
			GameObject teleportEffectPrefab = Run.instance.GetTeleportEffectPrefab(characterBody.gameObject);
			if ((bool)explicitSpawnEffectPrefab)
			{
				teleportEffectPrefab = explicitSpawnEffectPrefab;
			}
			if ((bool)teleportEffectPrefab)
			{
				EffectManager.SimpleEffect(teleportEffectPrefab, vector, Quaternion.identity, transmit: true);
			}
			this.onBodyTeleport?.Invoke(characterBody);
		}
	}
}
