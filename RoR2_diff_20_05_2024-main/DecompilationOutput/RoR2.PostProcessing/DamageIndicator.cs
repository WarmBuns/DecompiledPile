using UnityEngine;

namespace RoR2.PostProcessing;

public class DamageIndicator : MonoBehaviour
{
	private struct IndicatorTimestamp
	{
		public Run.FixedTimeStamp hitTimeStamp;

		public bool isActive;
	}

	private CameraRigController cameraRigController;

	public Material mat;

	public float boxCornerRadius = 0.5f;

	public float boxFeather = 0.5f;

	public float maxboxSize = 1f;

	public float minboxSize = 0.75f;

	[Tooltip("The color of the damage indicator.")]
	public Color tintColor;

	[Tooltip("X and Y are the normalized screen position, and Z is the opacity of the indicator.")]
	public Vector4[] indicators = new Vector4[8];

	private IndicatorTimestamp[] _indicatorTimestamps = new IndicatorTimestamp[8];

	[Tooltip("The radius of the mask for the indicator. It will be scaled to fit the aspect ratio of the screen.")]
	public float indicatorRadius;

	[Tooltip("The opacity of the indicator over time.")]
	public AnimationCurve indicatorOpacity;

	[Min(0.01f)]
	[Tooltip("The duration of the indicator. This will scale the opacity fade time.")]
	public float indicatorDuration = 1f;

	[Tooltip("The feather on the indicator mask.")]
	public float indicatorFeather = 0.5f;

	private float _indicatorValue;

	private bool isEnabled = true;

	private void Awake()
	{
		cameraRigController = GetComponentInParent<CameraRigController>();
		mat = Object.Instantiate(mat);
	}

	private void OnEnable()
	{
		GlobalEventManager.onClientDamageNotified += OnClientDamage;
	}

	public void OnDisable()
	{
		GlobalEventManager.onClientDamageNotified -= OnClientDamage;
	}

	private float GetBoxSize(float value)
	{
		return Util.Remap(value, 0f, 1f, maxboxSize, minboxSize);
	}

	public void OnClientDamage(DamageDealtMessage damageDealtMessage)
	{
		if (!(damageDealtMessage.attacker == null) && !(damageDealtMessage.victim == null) && (bool)cameraRigController && damageDealtMessage.victim == cameraRigController.localUserViewer.cachedBodyObject)
		{
			_indicatorValue = cameraRigController.localUserViewer.userProfile.directionalDamageIndicatorScale;
			Vector3 forward = cameraRigController.transform.forward;
			Vector3 position = damageDealtMessage.victim.gameObject.transform.position;
			Vector3 position2 = damageDealtMessage.attacker.gameObject.transform.position;
			Vector2 to = new Vector2(position.x, position.z) - new Vector2(position2.x, position2.z);
			int num = Mathf.RoundToInt((0f - Vector2.SignedAngle(new Vector2(forward.x, forward.z), to) + 180f) / 45f) % 8;
			_indicatorTimestamps[num].hitTimeStamp = Run.FixedTimeStamp.now;
			_indicatorTimestamps[num].isActive = true;
		}
	}

	private void ResetIndicators()
	{
		for (int i = 0; i < _indicatorTimestamps.Length; i++)
		{
			_indicatorTimestamps[i].isActive = false;
			indicators[i].z = 0f;
		}
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (Mathf.Approximately(_indicatorValue, 0f))
		{
			if (isEnabled)
			{
				ResetIndicators();
				mat.SetVectorArray("_Indicators", indicators);
				isEnabled = false;
			}
			Graphics.Blit(source, destination, mat);
			return;
		}
		if (!isEnabled)
		{
			isEnabled = true;
		}
		mat.SetFloat("_BoxCornerRadius", boxCornerRadius);
		mat.SetFloat("_BoxFeather", boxFeather);
		mat.SetFloat("_BoxSize", GetBoxSize(_indicatorValue));
		mat.SetColor("_TintColor", tintColor);
		mat.SetFloat("_IndicatorRadius", indicatorRadius);
		mat.SetFloat("_IndicatorFeather", indicatorFeather);
		for (int i = 0; i < _indicatorTimestamps.Length; i++)
		{
			if (_indicatorTimestamps[i].isActive)
			{
				float timeSince = _indicatorTimestamps[i].hitTimeStamp.timeSince;
				if (timeSince >= indicatorDuration)
				{
					_indicatorTimestamps[i].isActive = false;
				}
				else
				{
					indicators[i].z = Mathf.Clamp(indicatorOpacity.Evaluate(timeSince / indicatorDuration), 0f, 1f);
				}
			}
		}
		mat.SetVectorArray("_Indicators", indicators);
		Graphics.Blit(source, destination, mat);
	}
}
