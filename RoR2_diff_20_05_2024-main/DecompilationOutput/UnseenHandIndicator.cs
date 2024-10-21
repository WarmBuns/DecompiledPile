using System.Collections.Generic;
using RoR2;
using UnityEngine;

public class UnseenHandIndicator : MonoBehaviour
{
	public Color customColor = Color.magenta;

	private Dictionary<Collider, Highlight> pairs = new Dictionary<Collider, Highlight>();

	private void OnTriggerEnter(Collider other)
	{
		AddIndictator(other);
	}

	private void OnTriggerExit(Collider other)
	{
		DelIndictator(other);
	}

	private void AddIndictator(Collider target)
	{
		if (pairs.ContainsKey(target))
		{
			return;
		}
		CharacterModel characterModel = target.GetComponent<ModelLocator>()?.modelTransform?.GetComponent<CharacterModel>();
		if (characterModel == null || characterModel.body.teamComponent.teamIndex != TeamIndex.Monster)
		{
			return;
		}
		CharacterModel.RendererInfo[] baseRendererInfos = characterModel.baseRendererInfos;
		for (int i = 0; i < baseRendererInfos.Length; i++)
		{
			CharacterModel.RendererInfo rendererInfo = baseRendererInfos[i];
			if (!rendererInfo.ignoreOverlays)
			{
				Highlight highlight = base.gameObject.AddComponent<Highlight>();
				highlight.CustomColor = customColor;
				highlight.highlightColor = Highlight.HighlightColor.custom;
				highlight.targetRenderer = rendererInfo.renderer;
				highlight.strength = 1f;
				highlight.isOn = true;
				pairs.Add(target, highlight);
				break;
			}
		}
	}

	private void DelIndictator(Collider target)
	{
		if (pairs.ContainsKey(target))
		{
			Object.Destroy(pairs[target]);
			pairs.Remove(target);
		}
	}
}
