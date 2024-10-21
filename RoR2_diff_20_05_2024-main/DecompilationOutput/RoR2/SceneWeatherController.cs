using System;
using UnityEngine;

namespace RoR2;

[ExecuteInEditMode]
public class SceneWeatherController : MonoBehaviour
{
	[Serializable]
	public struct WeatherParams
	{
		[ColorUsage(true, true)]
		public Color sunColor;

		public float sunIntensity;

		public float fogStart;

		public float fogScale;

		public float fogIntensity;
	}

	private static SceneWeatherController _instance;

	public WeatherParams initialWeatherParams;

	public WeatherParams finalWeatherParams;

	public Light sun;

	public Material fogMaterial;

	public string rtpcWeather;

	public float rtpcMin;

	public float rtpcMax = 100f;

	public AnimationCurve weatherLerpOverChargeTime;

	[Range(0f, 1f)]
	public float weatherLerp;

	public static SceneWeatherController instance => _instance;

	private void OnEnable()
	{
		if (!_instance)
		{
			_instance = this;
		}
	}

	private void OnDisable()
	{
		if (_instance == this)
		{
			_instance = null;
		}
	}

	private WeatherParams GetWeatherParams(float t)
	{
		WeatherParams result = default(WeatherParams);
		result.sunColor = Color.Lerp(initialWeatherParams.sunColor, finalWeatherParams.sunColor, t);
		result.sunIntensity = Mathf.Lerp(initialWeatherParams.sunIntensity, finalWeatherParams.sunIntensity, t);
		result.fogStart = Mathf.Lerp(initialWeatherParams.fogStart, finalWeatherParams.fogStart, t);
		result.fogScale = Mathf.Lerp(initialWeatherParams.fogScale, finalWeatherParams.fogScale, t);
		result.fogIntensity = Mathf.Lerp(initialWeatherParams.fogIntensity, finalWeatherParams.fogIntensity, t);
		return result;
	}

	private void Update()
	{
		WeatherParams weatherParams = GetWeatherParams(weatherLerp);
		if ((bool)sun)
		{
			sun.color = weatherParams.sunColor;
			sun.intensity = weatherParams.sunIntensity;
		}
		if ((bool)fogMaterial)
		{
			fogMaterial.SetFloat("_FogPicker", weatherLerp);
			fogMaterial.SetFloat("_FogStart", weatherParams.fogStart);
			fogMaterial.SetFloat("_FogScale", weatherParams.fogScale);
			fogMaterial.SetFloat("_FogIntensity", weatherParams.fogIntensity);
		}
		if (true && rtpcWeather.Length != 0)
		{
			AkSoundEngine.SetRTPCValue(rtpcWeather, Mathf.Lerp(rtpcMin, rtpcMax, weatherLerp), base.gameObject);
		}
	}
}
