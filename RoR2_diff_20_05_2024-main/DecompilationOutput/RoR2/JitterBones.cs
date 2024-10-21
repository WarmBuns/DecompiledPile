using System;
using UnityEngine;

namespace RoR2;

public class JitterBones : MonoBehaviour
{
	private struct BoneInfo
	{
		public Transform transform;

		public bool isHead;

		public bool isRoot;
	}

	[SerializeField]
	private SkinnedMeshRenderer _skinnedMeshRenderer;

	private BoneInfo[] bones = Array.Empty<BoneInfo>();

	public float perlinNoiseFrequency;

	public float perlinNoiseStrength;

	public float perlinNoiseMinimumCutoff;

	public float perlinNoiseMaximumCutoff = 1f;

	public float headBonusStrength;

	private float age;

	public SkinnedMeshRenderer skinnedMeshRenderer
	{
		get
		{
			return _skinnedMeshRenderer;
		}
		set
		{
			if ((object)_skinnedMeshRenderer != value)
			{
				_skinnedMeshRenderer = value;
				RebuildBones();
			}
		}
	}

	private void RebuildBones()
	{
		if (!_skinnedMeshRenderer)
		{
			bones = Array.Empty<BoneInfo>();
			return;
		}
		Transform[] array = _skinnedMeshRenderer.bones;
		Array.Resize(ref bones, array.Length);
		for (int i = 0; i < bones.Length; i++)
		{
			Transform transform = array[i];
			string text = transform.name.ToLower();
			bones[i] = new BoneInfo
			{
				transform = transform,
				isHead = text.Contains("head"),
				isRoot = text.Contains("root")
			};
		}
	}

	private void Start()
	{
		RebuildBones();
	}

	private void LateUpdate()
	{
		if (!skinnedMeshRenderer)
		{
			return;
		}
		age += Time.deltaTime;
		for (int i = 0; i < bones.Length; i++)
		{
			BoneInfo boneInfo = bones[i];
			if (!boneInfo.isRoot)
			{
				float num = age * perlinNoiseFrequency;
				float num2 = i;
				Vector3 value = new Vector3(Mathf.PerlinNoise(num, num2), Mathf.PerlinNoise(num + 4f, num2 + 3f), Mathf.PerlinNoise(num + 6f, num2 - 7f));
				value = HGMath.Remap(value, perlinNoiseMinimumCutoff, perlinNoiseMaximumCutoff, -1f, 1f);
				value = HGMath.Clamp(value, 0f, 1f);
				value *= perlinNoiseStrength;
				if (headBonusStrength >= 0f && boneInfo.isHead)
				{
					value *= headBonusStrength;
				}
				boneInfo.transform.rotation *= Quaternion.Euler(value);
			}
		}
	}
}
