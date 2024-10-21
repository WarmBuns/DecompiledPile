using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class RandomizeSplatBias : MonoBehaviour
{
	public float minRedBias;

	public float maxRedBias;

	public float minGreenBias;

	public float maxGreenBias;

	public float minBlueBias;

	public float maxBlueBias;

	private MaterialPropertyBlock _propBlock;

	private CharacterModel characterModel;

	private List<Material> materialsList;

	private List<Renderer> rendererList;

	private Shader printShader;

	private void Start()
	{
		materialsList = new List<Material>();
		rendererList = new List<Renderer>();
		printShader = LegacyResourcesAPI.Load<Shader>("Shaders/Deferred/HGStandard");
		Setup();
	}

	private void Setup()
	{
		characterModel = GetComponent<CharacterModel>();
		if ((bool)characterModel)
		{
			for (int i = 0; i < characterModel.baseRendererInfos.Length; i++)
			{
				CharacterModel.RendererInfo rendererInfo = characterModel.baseRendererInfos[i];
				Material material = Object.Instantiate(rendererInfo.defaultMaterial);
				if (material.shader == printShader)
				{
					materialsList.Add(material);
					rendererList.Add(rendererInfo.renderer);
					rendererInfo.defaultMaterial = material;
					characterModel.baseRendererInfos[i] = rendererInfo;
				}
				Renderer renderer = rendererList[i];
				_propBlock = new MaterialPropertyBlock();
				renderer.GetPropertyBlock(_propBlock);
				_propBlock.SetFloat("_RedChannelBias", Random.Range(minRedBias, maxRedBias));
				_propBlock.SetFloat("_BlueChannelBias", Random.Range(minBlueBias, maxBlueBias));
				_propBlock.SetFloat("_GreenChannelBias", Random.Range(minGreenBias, maxGreenBias));
				renderer.SetPropertyBlock(_propBlock);
			}
		}
		else
		{
			Renderer componentInChildren = GetComponentInChildren<Renderer>();
			Material material2 = Object.Instantiate(componentInChildren.material);
			materialsList.Add(material2);
			componentInChildren.material = material2;
			_propBlock = new MaterialPropertyBlock();
			componentInChildren.GetPropertyBlock(_propBlock);
			_propBlock.SetFloat("_RedChannelBias", Random.Range(minRedBias, maxRedBias));
			_propBlock.SetFloat("_BlueChannelBias", Random.Range(minBlueBias, maxBlueBias));
			_propBlock.SetFloat("_GreenChannelBias", Random.Range(minGreenBias, maxGreenBias));
			componentInChildren.SetPropertyBlock(_propBlock);
		}
	}

	private void OnDestroy()
	{
		if (materialsList != null)
		{
			for (int i = 0; i < materialsList.Count; i++)
			{
				Object.Destroy(materialsList[i]);
			}
		}
	}
}
