using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace RoR2.PostProcessing;

[Serializable]
public class ScreenDamageData
{
	public Color IntendedTintColor;

	public float distortionStrength;

	public float desaturationStrength;

	public float tintStrength;

	public bool combineAdditively;

	public Color tintColor
	{
		get
		{
			if (combineAdditively)
			{
				return IntendedTintColor;
			}
			return (Color)Vector4.one - IntendedTintColor;
		}
	}

	public ColorParameter colorParameter => new ColorParameter
	{
		value = tintColor
	};

	public ScreenDamageData(Color _tintColor, float _distortionStrength, float _desatStrength, float _tintStrength, bool _combineAdditively)
	{
		IntendedTintColor = _tintColor;
		distortionStrength = _distortionStrength;
		desaturationStrength = _desatStrength;
		tintStrength = _tintStrength;
		combineAdditively = _combineAdditively;
	}

	public ScreenDamageData()
	{
		IntendedTintColor = Color.red;
		distortionStrength = 0f;
		desaturationStrength = 0f;
		tintStrength = 0f;
		combineAdditively = false;
	}
}
