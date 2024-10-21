using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

public class PrintController : MonoBehaviour
{
	private struct RendererMaterialPair
	{
		public readonly Renderer renderer;

		public readonly Material material;

		public RendererMaterialPair(Renderer renderer, Material material)
		{
			this.renderer = renderer;
			this.material = material;
		}
	}

	[Header("Print Time and Behaviors")]
	public float printTime;

	public AnimationCurve printCurve;

	public float age;

	public bool disableWhenFinished = true;

	public bool paused;

	[Header("Print Start/End Values")]
	public float startingPrintHeight;

	public float maxPrintHeight;

	public float startingPrintBias;

	public float maxPrintBias;

	public bool animateFlowmapPower;

	public float startingFlowmapPower;

	public float maxFlowmapPower;

	private CharacterModel characterModel;

	private MaterialPropertyBlock _propBlock;

	private RendererMaterialPair[] rendererMaterialPairs = Array.Empty<RendererMaterialPair>();

	private bool hasSetupOnce;

	private static Shader printShader;

	private static int sliceHeightShaderPropertyId;

	private static int printBiasShaderPropertyId;

	private static int flowHeightPowerShaderPropertyId;

	private static int printOnPropertyId;

	[InitDuringStartup]
	private static void Init()
	{
		AsyncOperationHandle<Shader> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Shader>("Shaders/Deferred/HGStandard");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Shader> x)
		{
			printShader = x.Result;
		};
		sliceHeightShaderPropertyId = Shader.PropertyToID("_SliceHeight");
		printBiasShaderPropertyId = Shader.PropertyToID("_PrintBias");
		flowHeightPowerShaderPropertyId = Shader.PropertyToID("_FlowHeightPower");
		printOnPropertyId = Shader.PropertyToID("_PrintOn");
	}

	private void Awake()
	{
		characterModel = GetComponent<CharacterModel>();
		_propBlock = new MaterialPropertyBlock();
		SetupPrint();
	}

	private void OnDisable()
	{
		SetMaterialPrintCutoffEnabled(shouldEnable: false);
	}

	private void OnEnable()
	{
		SetMaterialPrintCutoffEnabled(shouldEnable: true);
		age = 0f;
	}

	private void Update()
	{
		UpdatePrint(Time.deltaTime);
	}

	private void OnDestroy()
	{
		for (int num = rendererMaterialPairs.Length - 1; num > 0; num--)
		{
			UnityEngine.Object.Destroy(rendererMaterialPairs[num].material);
			rendererMaterialPairs[num] = new RendererMaterialPair(null, null);
		}
		rendererMaterialPairs = Array.Empty<RendererMaterialPair>();
	}

	private void OnValidate()
	{
		if (printCurve == null)
		{
			printCurve = new AnimationCurve();
		}
		Keyframe[] keys = printCurve.keys;
		for (int i = 1; i < keys.Length; i++)
		{
			ref Keyframe reference = ref keys[i - 1];
			ref Keyframe reference2 = ref keys[i];
			if (reference.time >= reference2.time)
			{
				Debug.LogErrorFormat("Animation curve error on object {0}", base.gameObject.name);
				break;
			}
		}
		if (printTime == 0f)
		{
			Debug.LogErrorFormat("printTime==0f on object {0}", base.gameObject.name);
		}
	}

	public void SetPaused(bool newPaused)
	{
		paused = newPaused;
	}

	private void UpdatePrint(float deltaTime)
	{
		if (printCurve != null)
		{
			if (!paused)
			{
				age += deltaTime;
			}
			float printThreshold = printCurve.Evaluate(age / printTime);
			SetPrintThreshold(printThreshold);
			if (age >= printTime && disableWhenFinished)
			{
				base.enabled = false;
				age = 0f;
			}
		}
	}

	private void SetPrintThreshold(float sample)
	{
		float num = 1f - sample;
		float value = sample * maxPrintHeight + num * startingPrintHeight;
		float value2 = sample * maxPrintBias + num * startingPrintBias;
		float value3 = sample * maxFlowmapPower + num * startingFlowmapPower;
		for (int i = 0; i < rendererMaterialPairs.Length; i++)
		{
			ref RendererMaterialPair reference = ref rendererMaterialPairs[i];
			reference.renderer.GetPropertyBlock(_propBlock);
			_propBlock.SetFloat(sliceHeightShaderPropertyId, value);
			_propBlock.SetFloat(printBiasShaderPropertyId, value2);
			if (animateFlowmapPower)
			{
				_propBlock.SetFloat(flowHeightPowerShaderPropertyId, value3);
			}
			reference.renderer.SetPropertyBlock(_propBlock);
		}
	}

	private void SetupPrint()
	{
		if (hasSetupOnce)
		{
			return;
		}
		hasSetupOnce = true;
		if ((bool)characterModel)
		{
			CharacterModel.RendererInfo[] baseRendererInfos = characterModel.baseRendererInfos;
			int num = 0;
			for (int i = 0; i < baseRendererInfos.Length; i++)
			{
				if (!(baseRendererInfos[i].defaultMaterial?.shader != printShader))
				{
					num++;
				}
			}
			Array.Resize(ref rendererMaterialPairs, num);
			int j = 0;
			int num2 = 0;
			for (; j < baseRendererInfos.Length; j++)
			{
				ref CharacterModel.RendererInfo reference = ref baseRendererInfos[j];
				if (!(reference.defaultMaterial?.shader != printShader))
				{
					Material material = (reference.defaultMaterial = UnityEngine.Object.Instantiate(reference.defaultMaterial));
					rendererMaterialPairs[num2++] = new RendererMaterialPair(reference.renderer, material);
				}
			}
		}
		else
		{
			List<Renderer> gameObjectComponentsInChildren = GetComponentsCache<Renderer>.GetGameObjectComponentsInChildren(base.gameObject, includeInactive: true);
			Array.Resize(ref rendererMaterialPairs, gameObjectComponentsInChildren.Count);
			int k = 0;
			for (int count = gameObjectComponentsInChildren.Count; k < count; k++)
			{
				Renderer renderer = gameObjectComponentsInChildren[k];
				Material material2 = renderer.material;
				rendererMaterialPairs[k] = new RendererMaterialPair(renderer, material2);
			}
			GetComponentsCache<Renderer>.ReturnBuffer(gameObjectComponentsInChildren);
		}
		SetMaterialPrintCutoffEnabled(shouldEnable: false);
		age = 0f;
	}

	private void SetMaterialPrintCutoffEnabled(bool shouldEnable)
	{
		if (shouldEnable)
		{
			EnableMaterialPrintCutoff();
		}
		else
		{
			DisableMaterialPrintCutoff();
		}
	}

	private void EnableMaterialPrintCutoff()
	{
		for (int i = 0; i < rendererMaterialPairs.Length; i++)
		{
			Material material = rendererMaterialPairs[i].material;
			material.EnableKeyword("PRINT_CUTOFF");
			material.SetInt(printOnPropertyId, 1);
		}
	}

	private void DisableMaterialPrintCutoff()
	{
		for (int i = 0; i < rendererMaterialPairs.Length; i++)
		{
			Material material = rendererMaterialPairs[i].material;
			material.DisableKeyword("PRINT_CUTOFF");
			material.SetInt(printOnPropertyId, 0);
		}
		SetPrintThreshold(1f);
	}
}
