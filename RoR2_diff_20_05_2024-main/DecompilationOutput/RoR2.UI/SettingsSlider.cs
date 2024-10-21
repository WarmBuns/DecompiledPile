using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class SettingsSlider : BaseSettingsControl
{
	public Slider slider;

	public HGTextMeshProUGUI valueText;

	public float minValue;

	public float maxValue;

	[Tooltip("When this setting is turned 'on' as part of a subsection, set the value to defaultEnabledValue. maxValue is used otherwise ")]
	public bool overrideDefaultEnabledValue;

	public float defaultEnabledValue = 1f;

	public string formatString = "{0:0.00}";

	private bool listenerAdded;

	private MPEventSystemLocator systemLocator;

	private MPInputModule inputModule;

	private float sliderSpeed = 1f;

	private float prevTimestamp;

	private float accumulatedTime;

	private float prevDirection;

	private const float timePerSpeedLevel = 1f;

	private const float boostPerSpeedLevel = 4f;

	private const float maxSpeedLevel = 10f;

	protected new void Awake()
	{
		base.Awake();
		if ((bool)slider)
		{
			slider.minValue = minValue;
			slider.maxValue = maxValue;
			if (!listenerAdded)
			{
				slider.onValueChanged.AddListener(OnSliderValueChanged);
			}
		}
		listenerAdded = true;
		systemLocator = GetComponent<MPEventSystemLocator>();
	}

	private new void OnEnable()
	{
		UpdateControls();
		if ((bool)slider)
		{
			slider.minValue = minValue;
			slider.maxValue = maxValue;
			if (!listenerAdded)
			{
				slider.onValueChanged.AddListener(OnSliderValueChanged);
			}
		}
		listenerAdded = true;
	}

	private void OnSliderValueChanged(float newValue)
	{
		if (!base.inUpdateControls)
		{
			SubmitSetting(TextSerialization.ToStringInvariant(newValue));
		}
	}

	private void OnInputFieldSubmit(string newString)
	{
		if (!base.inUpdateControls && TextSerialization.TryParseInvariant(newString, out float result))
		{
			SubmitSetting(TextSerialization.ToStringInvariant(result));
		}
	}

	public override void SetSettingState(bool turnedOn)
	{
		if (!turnedOn && !Mathf.Approximately(slider.value, minValue))
		{
			defaultEnabledValue = slider.value;
		}
		slider.value = ((!turnedOn) ? minValue : (overrideDefaultEnabledValue ? defaultEnabledValue : maxValue));
	}

	public override bool IsSettingEnabled()
	{
		return slider.normalizedValue > 0f;
	}

	protected override void OnUpdateControls()
	{
		base.OnUpdateControls();
		if (TextSerialization.TryParseInvariant(GetCurrentValue(), out float result))
		{
			float num = Mathf.Clamp(result, minValue, maxValue);
			if ((bool)slider)
			{
				slider.value = num;
			}
			if ((bool)valueText)
			{
				valueText.text = string.Format(CultureInfo.InvariantCulture, formatString, num);
			}
		}
	}

	private float CalculateSliderSpeed(float deltaTime, float direction)
	{
		if (inputModule == null)
		{
			inputModule = systemLocator.eventSystem.GetComponent<MPInputModule>();
		}
		float num = 1.5f / inputModule.inputActionsPerSecond;
		if (deltaTime <= num * 2f && direction == prevDirection)
		{
			accumulatedTime += deltaTime;
		}
		else
		{
			accumulatedTime = 0f;
		}
		prevDirection = direction;
		return Mathf.Min(10f, 1f + Mathf.Floor(accumulatedTime / 1f) * 4f);
	}

	public void MoveSlider(float delta)
	{
		float timeSinceLevelLoad = Time.timeSinceLevelLoad;
		float deltaTime = timeSinceLevelLoad - prevTimestamp;
		prevTimestamp = timeSinceLevelLoad;
		sliderSpeed = CalculateSliderSpeed(deltaTime, Mathf.Sign(delta));
		if ((bool)slider)
		{
			slider.normalizedValue += delta * sliderSpeed;
		}
	}
}
