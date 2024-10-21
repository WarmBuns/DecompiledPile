using UnityEngine;

public class LightIntensityCurve : MonoBehaviour
{
	public AnimationCurve curve;

	public float timeMax = 5f;

	private float time;

	private Light light;

	private float maxIntensity;

	[Tooltip("Loops the animation curve.")]
	public bool loop;

	[Tooltip("Starts in a random point in the animation curve.")]
	public bool randomStart;

	[Tooltip("Causes the lights to be negative, casting shadows instead.")]
	public bool enableNegativeLights;

	private void Awake()
	{
		if ((bool)light || TryGetComponent<Light>(out light))
		{
			maxIntensity = light.intensity;
		}
	}

	private void Start()
	{
		light.intensity = 0f;
		if (randomStart)
		{
			time = Random.Range(0f, timeMax);
		}
		if (enableNegativeLights)
		{
			light.color = new Color(0f - light.color.r, 0f - light.color.g, 0f - light.color.b);
		}
	}

	private void OnEnable()
	{
		time = 0f;
		if (!light)
		{
			light = GetComponent<Light>();
		}
		light.intensity = 0f;
		if (randomStart)
		{
			time = Random.Range(0f, timeMax);
		}
		if (enableNegativeLights)
		{
			light.color = new Color(0f - light.color.r, 0f - light.color.g, 0f - light.color.b);
		}
	}

	private void OnDisable()
	{
		if ((bool)light)
		{
			light.intensity = maxIntensity;
		}
	}

	private void Update()
	{
		time += Time.deltaTime;
		light.intensity = curve.Evaluate(time / timeMax) * maxIntensity;
		if (time >= timeMax && loop)
		{
			time = 0f;
		}
	}
}
