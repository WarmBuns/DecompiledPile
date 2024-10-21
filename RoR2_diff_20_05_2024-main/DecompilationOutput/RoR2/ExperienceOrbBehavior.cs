using UnityEngine;

namespace RoR2;

public class ExperienceOrbBehavior : MonoBehaviour
{
	public GameObject hitEffectPrefab;

	private EffectManagerHelper efh;

	private new Transform transform;

	private TrailRenderer trail;

	private Light light;

	private float localTime;

	private Vector3 startPos;

	private Vector3 previousPos;

	private Vector3 initialVelocity;

	private float scale;

	private bool consumed;

	private static readonly string expSoundString = "Play_UI_xp_gain";

	public Transform targetTransform { get; set; }

	public float travelTime { get; set; }

	public ulong exp { get; set; }

	private void Awake()
	{
		transform = base.transform;
		trail = GetComponent<TrailRenderer>();
		light = GetComponent<Light>();
		efh = GetComponent<EffectManagerHelper>();
	}

	private void Start()
	{
		SetupStartingValues();
	}

	public void SetupStartingValues()
	{
		localTime = 0f;
		consumed = false;
		startPos = transform.position;
		previousPos = startPos;
		scale = 2f * Mathf.Log((float)exp + 1f, 6f);
		initialVelocity = (Vector3.up * 4f + Random.insideUnitSphere * 1f) * scale;
		transform.localScale = new Vector3(scale, scale, scale);
		if ((bool)trail)
		{
			trail.startWidth = 0.05f * scale;
		}
		if ((bool)light)
		{
			light.range = 1f * scale;
		}
	}

	private void Update()
	{
		localTime += Time.deltaTime;
		if (!targetTransform)
		{
			HandleEnd();
			return;
		}
		float num = Mathf.Clamp01(localTime / travelTime);
		previousPos = transform.position;
		transform.position = CalculatePosition(startPos, initialVelocity, targetTransform.position, num);
		if (num >= 1f)
		{
			OnHitTarget();
		}
	}

	private static Vector3 CalculatePosition(Vector3 startPos, Vector3 initialVelocity, Vector3 targetPos, float t)
	{
		Vector3 a = startPos + initialVelocity * t;
		float t2 = t * t * t;
		return Vector3.LerpUnclamped(a, targetPos, t2);
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.transform == targetTransform)
		{
			OnHitTarget();
		}
	}

	private void OnHitTarget()
	{
		if (!consumed)
		{
			consumed = true;
			Util.PlaySound(expSoundString, targetTransform.gameObject);
			GameObject gameObject;
			if (!EffectManager.ShouldUsePooledEffect(hitEffectPrefab))
			{
				gameObject = Object.Instantiate(hitEffectPrefab, transform.position, Util.QuaternionSafeLookRotation(previousPos - startPos));
			}
			else
			{
				EffectManagerHelper pooledEffect = EffectManager.GetPooledEffect(hitEffectPrefab, transform.position, Util.QuaternionSafeLookRotation(previousPos - startPos));
				pooledEffect.Reset(inActivate: true);
				gameObject = pooledEffect.gameObject;
				gameObject.SetActive(value: true);
				pooledEffect.ResetPostActivate();
			}
			gameObject.transform.localScale *= scale;
			HandleEnd();
		}
	}

	private void HandleEnd()
	{
		if (efh != null)
		{
			efh.ReturnToPool();
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}
}
