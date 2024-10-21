using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RoR2;

public static class HGPhysics
{
	private const int INITIAL_POOL_SIZE = 4;

	private const int MAX_COLLIDERS = 100;

	private const int MAX_RAYCASTHIT = 100;

	private static List<Collider[]> ColliderPool;

	private static List<RaycastHit[]> RaycastPool;

	private static int _colliderCallCount;

	private static int _raycastHitCallCount;

	private static bool _useSharedRaycast;

	private static bool _trackRaycasts;

	private static bool _useSharedColliders;

	private static bool _doCompare;

	private static int _maxHits;

	private static int _highRaycastCount;

	private static int _highColiderCount;

	public static bool useSharedRaycast
	{
		get
		{
			return _useSharedRaycast;
		}
		set
		{
			_useSharedRaycast = value;
			UnityEngine.Debug.LogFormat("Use Non-Alloc Raycasts: {0}", value.ToString());
		}
	}

	public static bool trackRaycasts
	{
		get
		{
			return _trackRaycasts;
		}
		set
		{
			_trackRaycasts = value;
		}
	}

	public static bool useSharedColliders
	{
		get
		{
			return _useSharedColliders;
		}
		set
		{
			_useSharedColliders = value;
			UnityEngine.Debug.LogFormat("Use Non-Alloc Casts: {0}", value.ToString());
		}
	}

	static HGPhysics()
	{
		_colliderCallCount = 0;
		_raycastHitCallCount = 0;
		_useSharedRaycast = true;
		_trackRaycasts = true;
		_useSharedColliders = true;
		_doCompare = false;
		_maxHits = 0;
		_highRaycastCount = 0;
		_highColiderCount = 0;
		ColliderPool = new List<Collider[]>(4);
		for (int i = 0; i < 4; i++)
		{
			Collider[] item = new Collider[100];
			ColliderPool.Add(item);
		}
		RaycastPool = new List<RaycastHit[]>(4);
		for (int j = 0; j < 4; j++)
		{
			RaycastHit[] item2 = new RaycastHit[100];
			RaycastPool.Add(item2);
		}
	}

	private static void AddColliderArray()
	{
		UnityEngine.Debug.LogWarning("HGPhysics allocating new collider array.  consider upping the initial value");
		ColliderPool.Add(new Collider[100]);
	}

	private static void AddRaycastArray()
	{
		UnityEngine.Debug.LogWarning("HGPhysics allocating new RaycastHit array.  consider upping the initial value");
		RaycastPool.Add(new RaycastHit[100]);
	}

	public static int OverlapSphere(out Collider[] colliders, Vector3 position, float radius, int layerMask = -1, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
	{
		int num = 0;
		if (useSharedColliders)
		{
			_colliderCallCount++;
			if (ColliderPool.Count < 1)
			{
				AddColliderArray();
			}
			Collider[] array = ColliderPool[ColliderPool.Count - 1];
			ColliderPool.RemoveAt(ColliderPool.Count - 1);
			num = Physics.OverlapSphereNonAlloc(position, radius, array, layerMask, queryTriggerInteraction);
			colliders = array;
		}
		else
		{
			colliders = Physics.OverlapSphere(position, radius, layerMask, queryTriggerInteraction);
			num = colliders.Length;
		}
		return num;
	}

	public static bool DoesOverlapSphere(Vector3 position, float radius, int layerMask = -1, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
	{
		Collider[] colliders;
		int num = OverlapSphere(out colliders, position, radius, layerMask, queryTriggerInteraction);
		ReturnResults(colliders);
		return num > 0;
	}

	public static int OverlapBox(out Collider[] colliders, Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
	{
		int num = 0;
		if (useSharedColliders)
		{
			_colliderCallCount++;
			if (ColliderPool.Count < 1)
			{
				AddColliderArray();
			}
			Collider[] array = ColliderPool[ColliderPool.Count - 1];
			ColliderPool.RemoveAt(ColliderPool.Count - 1);
			num = Physics.OverlapBoxNonAlloc(center, halfExtents, array, orientation, layerMask, queryTriggerInteraction);
			colliders = array;
		}
		else
		{
			colliders = Physics.OverlapBox(center, halfExtents, orientation, layerMask, queryTriggerInteraction);
			num = colliders.Length;
		}
		return num;
	}

	public static int SphereCastAll(out RaycastHit[] hits, Vector3 origin, float radius, Vector3 direction, float maxDistance = float.PositiveInfinity, int layerMask = -1, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
	{
		int num = 0;
		if (useSharedRaycast)
		{
			_raycastHitCallCount++;
			if (RaycastPool.Count < 1)
			{
				AddRaycastArray();
			}
			RaycastHit[] array = RaycastPool[RaycastPool.Count - 1];
			RaycastPool.RemoveAt(RaycastPool.Count - 1);
			num = Physics.SphereCastNonAlloc(origin, radius, direction, array, maxDistance, layerMask, queryTriggerInteraction);
			hits = array;
		}
		else
		{
			hits = Physics.SphereCastAll(origin, radius, direction, maxDistance, layerMask, queryTriggerInteraction);
			num = hits.Length;
		}
		return num;
	}

	public static int RaycastAll(out RaycastHit[] hits, Vector3 origin, Vector3 normal, float maxDistance = float.PositiveInfinity, int layerMask = -1, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
	{
		int num = 0;
		if (useSharedRaycast)
		{
			_raycastHitCallCount++;
			if (RaycastPool.Count < 1)
			{
				AddRaycastArray();
			}
			RaycastHit[] array = RaycastPool[RaycastPool.Count - 1];
			RaycastPool.RemoveAt(RaycastPool.Count - 1);
			num = Physics.RaycastNonAlloc(origin, normal, array, maxDistance, layerMask, queryTriggerInteraction);
			hits = array;
		}
		else
		{
			hits = Physics.RaycastAll(origin, normal, maxDistance, layerMask, queryTriggerInteraction);
			num = hits.Length;
		}
		return num;
	}

	public static void ReturnResults(Collider[] colliders)
	{
		if (useSharedColliders)
		{
			_colliderCallCount--;
			ColliderPool.Add(colliders);
		}
	}

	public static void ReturnResults(RaycastHit[] hits)
	{
		if (useSharedRaycast)
		{
			_raycastHitCallCount--;
			RaycastPool.Add(hits);
		}
	}

	public static float CalculateDistance(float initialVelocity, float acceleration, float time)
	{
		return initialVelocity * time + 0.5f * acceleration * time * time;
	}

	[Conditional("DEBUG")]
	public static void TrackHits(ref int hits, int newHits)
	{
		if (hits < newHits)
		{
			hits = newHits;
			if (newHits > _maxHits)
			{
				_maxHits = newHits;
			}
		}
	}
}
