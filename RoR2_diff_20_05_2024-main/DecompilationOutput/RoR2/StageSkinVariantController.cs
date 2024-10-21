using System;
using UnityEngine;

namespace RoR2;

public class StageSkinVariantController : MonoBehaviour
{
	[Serializable]
	public struct StageSkinVariant
	{
		public string stageNameToken;

		public CharacterModel.RendererInfo[] replacementRenderInfos;

		public GameObject[] childObjects;
	}

	public StageSkinVariant[] stageSkinVariants;

	public CharacterModel characterModel;

	private void Awake()
	{
		if (!SceneInfo.instance)
		{
			return;
		}
		for (int i = 0; i < stageSkinVariants.Length; i++)
		{
			StageSkinVariant stageSkinVariant = stageSkinVariants[i];
			for (int j = 0; j < stageSkinVariant.childObjects.Length; j++)
			{
				stageSkinVariant.childObjects[j].SetActive(value: false);
			}
		}
		for (int k = 0; k < stageSkinVariants.Length; k++)
		{
			StageSkinVariant stageSkinVariant2 = stageSkinVariants[k];
			if (SceneInfo.instance.sceneDef.nameToken == stageSkinVariant2.stageNameToken)
			{
				for (int l = 0; l < stageSkinVariant2.childObjects.Length; l++)
				{
					stageSkinVariant2.childObjects[l].SetActive(value: true);
				}
				if (stageSkinVariant2.replacementRenderInfos.Length != 0)
				{
					characterModel.baseRendererInfos = stageSkinVariant2.replacementRenderInfos;
				}
				break;
			}
		}
	}
}
