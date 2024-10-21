using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoR2;

public static class TemporaryOverlayManager
{
	private static int arraySize = 50;

	private static int arrayExpansionSize = 20;

	private static int debugMaxSizeUsed = 0;

	private static TemporaryOverlayInstance[] overlayArray;

	private static TemporaryOverlayInstance temp;

	private static float _delta;

	private static float sample;

	private static ChildLocator childLocator;

	private static Transform childTransform;

	private static int propertyID;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		overlayArray = new TemporaryOverlayInstance[arraySize];
		arraySize = 0;
		propertyID = Shader.PropertyToID("_ExternalAlpha");
		SceneManager.sceneUnloaded += OnSceneUnloaded;
		RoR2Application.onUpdate += OverlayUpdate;
	}

	private static void OnSceneUnloaded(Scene arg0)
	{
		ClearAllOverlays();
	}

	private static void ClearAllOverlays()
	{
		int num = overlayArray.Length;
		for (int i = 0; i < num; i++)
		{
			overlayArray[i] = null;
		}
		arraySize = 0;
	}

	public static void OverlayUpdate()
	{
		_delta = Time.deltaTime;
		for (int i = 0; i < arraySize; i++)
		{
			temp = overlayArray[i];
			if (temp == null || !temp.ValidateOverlay())
			{
				RemoveOverlay(i);
				i--;
			}
			else
			{
				if (!temp.animateShaderAlpha)
				{
					continue;
				}
				if (!temp.initialized)
				{
					temp.Start();
				}
				temp.stopwatch += _delta;
				sample = temp.alphaCurve.Evaluate(temp.stopwatch / temp.duration);
				temp.materialInstance.SetFloat(propertyID, sample);
				if (!(temp.stopwatch >= temp.duration) || (!temp.destroyComponentOnEnd && !temp.destroyObjectOnEnd))
				{
					continue;
				}
				if ((bool)temp.destroyEffectPrefab)
				{
					childLocator = temp.gameObject.GetComponent<ChildLocator>();
					if ((bool)childLocator)
					{
						childTransform = childLocator.FindChild(temp.destroyEffectChildString);
						if ((bool)childTransform)
						{
							EffectManager.SpawnEffect(temp.destroyEffectPrefab, new EffectData
							{
								origin = childTransform.position,
								rotation = childTransform.rotation
							}, temp.transmit);
						}
					}
				}
				RemoveOverlay(i);
				i--;
			}
		}
	}

	public static TemporaryOverlayInstance AddOverlay(GameObject _gameObject)
	{
		if (arraySize == overlayArray.Length)
		{
			ExpandArray();
		}
		overlayArray[arraySize] = new TemporaryOverlayInstance(_gameObject);
		temp = overlayArray[arraySize];
		temp.managerIndex = arraySize++;
		debugMaxSizeUsed = Mathf.Max(arraySize, debugMaxSizeUsed);
		return temp;
	}

	public static void RemoveOverlay(int index)
	{
		if (index >= 0 && arraySize > index)
		{
			temp = overlayArray[index];
			if (temp != null)
			{
				temp.CleanupEffect();
				temp.managerIndex = -1;
			}
			arraySize--;
			if (arraySize != 0)
			{
				temp = overlayArray[arraySize];
				temp.managerIndex = index;
				overlayArray[index] = temp;
			}
		}
	}

	private static void ExpandArray()
	{
		Array.Resize(ref overlayArray, arraySize + arrayExpansionSize);
	}
}
