using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RoR2;

public class DamageTrail : MonoBehaviour
{
	private struct TrailPoint
	{
		public Vector3 position;

		public float localStartTime;

		public float localEndTime;

		public Transform segmentTransform;
	}

	[FormerlySerializedAs("updateInterval")]
	[Tooltip("How often to drop a new point onto the trail.")]
	public float pointUpdateInterval = 0.2f;

	[Tooltip("How often the damage trail should deal damage.")]
	public float damageUpdateInterval = 0.2f;

	[Tooltip("How large the radius, or width, of the damage detection should be.")]
	public float radius = 0.5f;

	[Tooltip("How large the height of the damage detection should be.")]
	public float height = 0.5f;

	[Tooltip("How long a point on the trail should last.")]
	public float pointLifetime = 3f;

	[Tooltip("The line renderer to use for display.")]
	public LineRenderer lineRenderer;

	public bool active = true;

	[Tooltip("Prefab to use per segment.")]
	public GameObject segmentPrefab;

	public bool destroyTrailSegments;

	public float damagePerSecond;

	public GameObject owner;

	private HashSet<GameObject> ignoredObjects = new HashSet<GameObject>();

	private TeamIndex teamIndex;

	private new Transform transform;

	private List<TrailPoint> pointsList;

	private float localTime;

	private float nextTrailPointUpdate;

	private float nextTrailDamageUpdate;

	private static float optimizedDamageUpdateinterval;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Init()
	{
		RoR2Application.onUpdate += UpdateOptimizedDamageUpdateInterval;
	}

	private void Awake()
	{
		pointsList = new List<TrailPoint>();
		transform = base.transform;
	}

	private void Start()
	{
		localTime = 0f;
		AddPoint();
		AddPoint();
	}

	private static void UpdateOptimizedDamageUpdateInterval()
	{
		float b = 0.4f;
		float a = 0.2f;
		float num = 60f;
		float num2 = 1f / Time.deltaTime;
		if (num2 > num)
		{
			optimizedDamageUpdateinterval = a;
			return;
		}
		float a2 = (num - num2) / 30f;
		a2 = Mathf.Min(a2, 1f);
		optimizedDamageUpdateinterval = Mathf.Lerp(a, b, a2);
	}

	private void OnDisable()
	{
		if (!EffectManager.UsePools)
		{
			return;
		}
		for (int num = pointsList.Count - 1; num >= 0; num--)
		{
			if ((bool)pointsList[num].segmentTransform)
			{
				GameObject gameObject = pointsList[num].segmentTransform.gameObject;
				EffectManagerHelper component = gameObject.GetComponent<EffectManagerHelper>();
				if (component != null && component.OwningPool != null)
				{
					component.OwningPool.ReturnObject(component);
				}
				else
				{
					Debug.LogFormat("DamageTrail.OnDestroy: No EFH on {0} ({1})", gameObject.name, gameObject.GetInstanceID());
				}
			}
			pointsList.RemoveAt(num);
		}
	}

	private void FixedUpdate()
	{
		MyFixedUpdate(Time.fixedDeltaTime);
	}

	private void MyFixedUpdate(float deltaTime)
	{
		localTime += deltaTime;
		if (localTime >= nextTrailPointUpdate)
		{
			nextTrailPointUpdate += pointUpdateInterval;
			UpdateTrail(active);
		}
		if (localTime >= nextTrailDamageUpdate)
		{
			nextTrailDamageUpdate += optimizedDamageUpdateinterval;
			DoDamage(optimizedDamageUpdateinterval);
		}
		if (pointsList.Count > 0)
		{
			TrailPoint value = pointsList[pointsList.Count - 1];
			value.position = transform.position;
			value.localEndTime = localTime + pointLifetime;
			pointsList[pointsList.Count - 1] = value;
			if ((bool)value.segmentTransform)
			{
				value.segmentTransform.position = transform.position;
			}
			if ((bool)lineRenderer)
			{
				lineRenderer.SetPosition(pointsList.Count - 1, value.position);
			}
		}
		if (!segmentPrefab)
		{
			return;
		}
		Vector3 position = transform.position;
		for (int num = pointsList.Count - 1; num >= 0; num--)
		{
			Transform segmentTransform = pointsList[num].segmentTransform;
			if ((bool)segmentTransform)
			{
				segmentTransform.LookAt(position, Vector3.up);
				Vector3 vector = pointsList[num].position - position;
				segmentTransform.position = position + vector * 0.5f;
				float num2 = Mathf.Clamp01(Mathf.InverseLerp(pointsList[num].localStartTime, pointsList[num].localEndTime, localTime));
				Vector3 localScale = new Vector3(radius * (1f - num2), radius * (1f - num2), vector.magnitude);
				segmentTransform.localScale = localScale;
				position = pointsList[num].position;
			}
		}
	}

	private void UpdateTrail(bool addPoint)
	{
		while (pointsList.Count > 0 && pointsList[0].localEndTime <= localTime)
		{
			RemovePoint(0);
		}
		if (addPoint)
		{
			AddPoint();
		}
		if ((bool)lineRenderer)
		{
			UpdateLineRenderer(lineRenderer);
		}
	}

	private void DoDamage(float damageInterval)
	{
		if (!NetworkServer.active || pointsList.Count == 0)
		{
			return;
		}
		Vector3 vector = pointsList[pointsList.Count - 1].position;
		ignoredObjects.Clear();
		TeamIndex attackerTeamIndex = TeamIndex.Neutral;
		float damage = damagePerSecond * damageInterval;
		if ((bool)owner)
		{
			ignoredObjects.Add(owner);
			attackerTeamIndex = TeamComponent.GetObjectTeam(owner);
		}
		DamageInfo damageInfo = new DamageInfo();
		damageInfo.attacker = owner;
		damageInfo.inflictor = base.gameObject;
		damageInfo.crit = false;
		damageInfo.damage = damage;
		damageInfo.damageColorIndex = DamageColorIndex.Item;
		damageInfo.damageType = DamageType.Generic;
		damageInfo.force = Vector3.zero;
		damageInfo.procCoefficient = 0f;
		for (int num = pointsList.Count - 2; num >= 0; num--)
		{
			Vector3 position = pointsList[num].position;
			Vector3 forward = position - vector;
			Vector3 halfExtents = new Vector3(radius, height, forward.magnitude);
			Vector3 center = Vector3.Lerp(position, vector, 0.5f);
			Quaternion orientation = Util.QuaternionSafeLookRotation(forward);
			Collider[] colliders;
			int num2 = HGPhysics.OverlapBox(out colliders, center, halfExtents, orientation, LayerIndex.entityPrecise.mask);
			for (int i = 0; i < num2; i++)
			{
				HurtBox component = colliders[i].GetComponent<HurtBox>();
				if (!component)
				{
					continue;
				}
				HealthComponent healthComponent = component.healthComponent;
				if ((bool)healthComponent)
				{
					GameObject item = healthComponent.gameObject;
					if (!ignoredObjects.Contains(item) && FriendlyFireManager.ShouldSplashHitProceed(healthComponent, attackerTeamIndex))
					{
						ignoredObjects.Add(item);
						damageInfo.position = colliders[i].transform.position;
						healthComponent.TakeDamage(damageInfo);
					}
				}
			}
			HGPhysics.ReturnResults(colliders);
			vector = position;
		}
	}

	private void UpdateLineRenderer(LineRenderer lineRenderer)
	{
		lineRenderer.positionCount = pointsList.Count;
		for (int i = 0; i < pointsList.Count; i++)
		{
			lineRenderer.SetPosition(i, pointsList[i].position);
		}
	}

	private void AddPoint()
	{
		TrailPoint trailPoint = default(TrailPoint);
		trailPoint.position = transform.position;
		trailPoint.localStartTime = localTime;
		trailPoint.localEndTime = localTime + pointLifetime;
		TrailPoint item = trailPoint;
		if ((bool)segmentPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(segmentPrefab))
			{
				item.segmentTransform = Object.Instantiate(segmentPrefab, transform).transform;
			}
			else
			{
				EffectManagerHelper andActivatePooledEffect = EffectManager.GetAndActivatePooledEffect(segmentPrefab, transform, inResetLocal: true);
				item.segmentTransform = andActivatePooledEffect.gameObject.transform;
			}
		}
		pointsList.Add(item);
	}

	private void RemovePoint(int pointIndex)
	{
		if (destroyTrailSegments)
		{
			if ((bool)pointsList[pointIndex].segmentTransform)
			{
				if (!EffectManager.UsePools)
				{
					Object.Destroy(pointsList[pointIndex].segmentTransform.gameObject);
				}
				else
				{
					GameObject gameObject = pointsList[pointIndex].segmentTransform.gameObject;
					EffectManagerHelper component = gameObject.GetComponent<EffectManagerHelper>();
					if (component != null && component.OwningPool != null)
					{
						component.OwningPool.ReturnObject(component);
					}
					else
					{
						Debug.LogFormat("DamageTrail.RemovePoint: No EFH on {0} ({1})", gameObject.name, gameObject.GetInstanceID());
						Object.Destroy(gameObject);
					}
				}
			}
		}
		else if (EffectManager.UsePools && (bool)pointsList[pointIndex].segmentTransform)
		{
			pointsList[pointIndex].segmentTransform.gameObject.transform.SetParent(null);
		}
		pointsList.RemoveAt(pointIndex);
	}

	private void OnDrawGizmos()
	{
		Vector3 vector = pointsList[pointsList.Count - 1].position;
		for (int num = pointsList.Count - 2; num >= 0; num--)
		{
			Vector3 position = pointsList[num].position;
			Vector3 forward = position - vector;
			Vector3 s = new Vector3(radius, 0.5f, forward.magnitude);
			Vector3 pos = Vector3.Lerp(position, vector, 0.5f);
			Quaternion q = Util.QuaternionSafeLookRotation(forward);
			Gizmos.matrix = Matrix4x4.TRS(pos, q, s);
			Gizmos.color = Color.blue;
			Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
			Gizmos.matrix = Matrix4x4.identity;
			vector = position;
		}
	}
}
