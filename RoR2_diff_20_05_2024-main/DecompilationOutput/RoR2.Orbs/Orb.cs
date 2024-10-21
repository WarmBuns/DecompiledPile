using UnityEngine;

namespace RoR2.Orbs;

public class Orb
{
	public Vector3 origin;

	public HurtBox target;

	public Orb nextOrb;

	public float arrivalTime;

	public int instanceID = -1;

	protected static int _InstanceCount;

	protected OrbPool _OwningPool;

	public float duration { get; protected set; }

	public float timeUntilArrival => arrivalTime - OrbManager.instance.time;

	public static int InstanceCount => _InstanceCount;

	protected float distanceToTarget
	{
		get
		{
			if ((bool)target)
			{
				return Vector3.Distance(target.transform.position, origin);
			}
			return 0f;
		}
	}

	public void SetOwningPool(OrbPool inOwningPool)
	{
		_OwningPool = inOwningPool;
	}

	public Orb()
	{
		_InstanceCount++;
	}

	public virtual void Reset()
	{
		origin = Vector3.zero;
		target = null;
		duration = 0f;
		arrivalTime = 0f;
		nextOrb = null;
	}

	public virtual void Begin()
	{
	}

	public virtual void OnArrival()
	{
		ReturnToPool();
	}

	public virtual void ReturnToPool()
	{
		if (OrbPool.UsePools && _OwningPool != null)
		{
			_OwningPool.ReturnObject(this);
		}
	}
}
