using System;
using HG.BlendableTypes;
using UnityEngine;

namespace RoR2;

[Serializable]
public struct CharacterCameraParamsData
{
	public BlendableFloat minPitch;

	public BlendableFloat maxPitch;

	public BlendableFloat wallCushion;

	public BlendableFloat pivotVerticalOffset;

	public BlendableVector3 idealLocalCameraPos;

	public BlendableFloat fov;

	public BlendableBool isFirstPerson;

	public float overrideFirstPersonFadeDuration;

	public static readonly CharacterCameraParamsData basic = new CharacterCameraParamsData
	{
		minPitch = -70f,
		maxPitch = 70f,
		wallCushion = 0.1f,
		pivotVerticalOffset = 1.6f,
		idealLocalCameraPos = new Vector3(0f, 0f, -5f),
		fov = new BlendableFloat
		{
			value = 60f,
			alpha = 0f
		},
		overrideFirstPersonFadeDuration = 0f
	};

	public static void Blend(in CharacterCameraParamsData src, ref CharacterCameraParamsData dest, float alpha)
	{
		BlendableFloat.Blend(in src.minPitch, ref dest.minPitch, alpha);
		BlendableFloat.Blend(in src.maxPitch, ref dest.maxPitch, alpha);
		BlendableFloat.Blend(in src.wallCushion, ref dest.wallCushion, alpha);
		BlendableFloat.Blend(in src.pivotVerticalOffset, ref dest.pivotVerticalOffset, alpha);
		BlendableVector3.Blend(in src.idealLocalCameraPos, ref dest.idealLocalCameraPos, alpha);
		BlendableFloat.Blend(in src.fov, ref dest.fov, alpha);
		BlendableBool.Blend(in src.isFirstPerson, ref dest.isFirstPerson, alpha);
	}
}
