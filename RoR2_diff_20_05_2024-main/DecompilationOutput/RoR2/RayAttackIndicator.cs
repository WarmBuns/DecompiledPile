using System;
using System.Collections.Generic;
using HG;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace RoR2;

public class RayAttackIndicator : MonoBehaviour
{
	private struct HitIndicatorInfo
	{
		public GameObject gameObject;

		public Transform transform;
	}

	private struct RaycastInfo
	{
		public Ray ray;

		public float maxDistance;

		public int hitsStart;

		public int maxHits;
	}

	private struct HitPointIndicatorData
	{
		public Vector3 position;

		public Quaternion rotation;

		public float scale;
	}

	private struct Updater
	{
		private bool running;

		public void ScheduleUpdate()
		{
			if (running)
			{
				return;
			}
			List<RayAttackIndicator> instancesList = InstanceTracker.GetInstancesList<RayAttackIndicator>();
			if (instancesList.Count != 0)
			{
				requestsBuffer = new NativeArray<RaycastInfo>(instancesList.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
				raycastCommandBuffer = new NativeArray<RaycastCommand>(requestsBuffer.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
				raycastRequestersList = CollectionPool<RayAttackIndicator, List<RayAttackIndicator>>.RentCollection();
				int num = 1;
				resultsBuffer = new NativeArray<RaycastHit>(requestsBuffer.Length * num, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
				for (int i = 0; i < instancesList.Count; i++)
				{
					RayAttackIndicator rayAttackIndicator = instancesList[i];
					raycastRequestersList.Add(rayAttackIndicator);
					requestsBuffer[i] = new RaycastInfo
					{
						ray = rayAttackIndicator.attackRay,
						maxDistance = rayAttackIndicator.attackRange,
						hitsStart = i * num,
						maxHits = 1
					};
					raycastCommandBuffer[i] = new RaycastCommand(rayAttackIndicator.attackRay.origin, rayAttackIndicator.attackRay.direction, rayAttackIndicator.attackRange, rayAttackIndicator.layerMask);
				}
				raycastJobHandle = RaycastCommand.ScheduleBatch(raycastCommandBuffer, resultsBuffer, 4);
				running = true;
			}
		}

		public void CompleteUpdate()
		{
			if (!running)
			{
				return;
			}
			raycastJobHandle.Complete();
			for (int i = 0; i < requestsBuffer.Length; i++)
			{
				RaycastInfo raycastInfo = requestsBuffer[i];
				RayAttackIndicator rayAttackIndicator = raycastRequestersList[i];
				if (!rayAttackIndicator)
				{
					continue;
				}
				List<HitPointIndicatorData> list = CollectionPool<HitPointIndicatorData, List<HitPointIndicatorData>>.RentCollection();
				try
				{
					Vector3 up = rayAttackIndicator.transform.up;
					Ray ray = raycastInfo.ray;
					Vector3 origin = ray.origin;
					Vector3 direction = ray.direction;
					if ((bool)rayAttackIndicator.originRecipient)
					{
						rayAttackIndicator.originRecipient.SetPositionAndRotation(origin, Quaternion.LookRotation(direction, rayAttackIndicator.originRecipient.up));
					}
					float num = raycastInfo.maxDistance;
					float num2 = 0f;
					for (int j = 0; j < raycastInfo.maxHits; j++)
					{
						RaycastHit raycastHit = resultsBuffer[raycastInfo.hitsStart + j];
						bool num3 = (object)raycastHit.collider != null;
						float num4 = (num3 ? raycastHit.distance : raycastInfo.maxDistance);
						num = ((num4 < num) ? num4 : num);
						num2 = ((num4 > num2) ? num4 : num2);
						Vector3 position = origin + direction * num4;
						if (num3)
						{
							list.Add(new HitPointIndicatorData
							{
								position = position,
								rotation = Quaternion.LookRotation(-direction, up),
								scale = rayAttackIndicator.attackRadius
							});
						}
					}
					rayAttackIndicator.SetHits(list);
					if ((bool)rayAttackIndicator.nearestHitRecipient)
					{
						rayAttackIndicator.nearestHitRecipient.SetPositionAndRotation(origin + direction * num, Quaternion.LookRotation(-direction, rayAttackIndicator.nearestHitRecipient.up));
					}
					if ((bool)rayAttackIndicator.furthestHitRecipient)
					{
						rayAttackIndicator.furthestHitRecipient.SetPositionAndRotation(origin + direction * num2, Quaternion.LookRotation(-direction, rayAttackIndicator.furthestHitRecipient.up));
					}
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					rayAttackIndicator.enabled = false;
				}
				finally
				{
					list = CollectionPool<HitPointIndicatorData, List<HitPointIndicatorData>>.ReturnCollection(list);
				}
			}
			resultsBuffer.Dispose();
			requestsBuffer.Dispose();
			raycastCommandBuffer.Dispose();
			raycastRequestersList = CollectionPool<RayAttackIndicator, List<RayAttackIndicator>>.ReturnCollection(raycastRequestersList);
			running = false;
		}
	}

	[Tooltip("The prefab that will be instantiated and moved to wherever the ray hits.")]
	public GameObject hitIndicatorPrefab;

	[Tooltip("Transform which will be moved to the start of the ray.")]
	public Transform originRecipient;

	[Tooltip("Transform which will be moved to the nearest hit.")]
	public Transform nearestHitRecipient;

	[Tooltip("Transform which will be moved to the furthest hit.")]
	public Transform furthestHitRecipient;

	[Range(1f, 1f)]
	[Tooltip("How many hits this raycast is allowed to make. This is currently limited to 1.")]
	public int maxHits = 1;

	private static readonly Ray defaultRay = new Ray(Vector3.zero, Vector3.up);

	[NonSerialized]
	public Ray attackRay = defaultRay;

	[NonSerialized]
	public float attackRange = float.PositiveInfinity;

	[NonSerialized]
	public float attackRadius = 1f;

	[NonSerialized]
	public LayerMask layerMask;

	private List<HitIndicatorInfo> hitIndicators = new List<HitIndicatorInfo>();

	private static bool raysDirty = false;

	private static List<RayAttackIndicator> raycastRequestersList;

	private static NativeArray<RaycastCommand> raycastCommandBuffer;

	private static NativeArray<RaycastHit> resultsBuffer;

	private static NativeArray<RaycastInfo> requestsBuffer;

	private static JobHandle raycastJobHandle;

	private static Updater updater;

	private void Awake()
	{
		layerMask = LayerIndex.CommonMasks.bullet;
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		AllocateHitIndicators(0);
		InstanceTracker.Remove(this);
	}

	private void AllocateHitIndicators(int newHitIndicatorCount)
	{
		while (newHitIndicatorCount < hitIndicators.Count)
		{
			UnityEngine.Object.Destroy(ListUtils.TakeLast(hitIndicators).gameObject);
		}
		while (newHitIndicatorCount > hitIndicators.Count)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(hitIndicatorPrefab);
			gameObject.SetActive(value: true);
			hitIndicators.Add(new HitIndicatorInfo
			{
				gameObject = gameObject,
				transform = gameObject.transform
			});
		}
	}

	private void SetHits(List<HitPointIndicatorData> hits)
	{
		if (base.enabled)
		{
			AllocateHitIndicators(hits.Count);
			for (int i = 0; i < hits.Count; i++)
			{
				HitIndicatorInfo hitIndicatorInfo = hitIndicators[i];
				HitPointIndicatorData hitPointIndicatorData = hits[i];
				hitIndicatorInfo.transform.SetPositionAndRotation(hitPointIndicatorData.position, hitPointIndicatorData.rotation);
				hitIndicatorInfo.transform.localScale = new Vector3(hitPointIndicatorData.scale, hitPointIndicatorData.scale, hitPointIndicatorData.scale);
			}
		}
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		RoR2Application.onUpdate += StaticUpdate;
		RoR2Application.onLateUpdate += StaticLateUpdate;
		RoR2Application.onFixedUpdate += StaticFixedUpdate;
	}

	private static void StaticUpdate()
	{
		if (raysDirty)
		{
			raysDirty = false;
			updater.ScheduleUpdate();
		}
	}

	private static void StaticLateUpdate()
	{
		updater.CompleteUpdate();
	}

	private static void StaticFixedUpdate()
	{
		raysDirty = true;
	}
}
