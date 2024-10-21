using UnityEngine;
using UnityEngine.Rendering;

namespace RoR2;

public class ItemDisplay : MonoBehaviour
{
	public CharacterModel.RendererInfo[] rendererInfos;

	public GameObject[] hideOnDeathObjects;

	private VisibilityLevel visibilityLevel = VisibilityLevel.Visible;

	private bool isDead;

	public VisibilityLevel GetVisibilityLevel()
	{
		return visibilityLevel;
	}

	public void SetVisibilityLevel(VisibilityLevel newVisibilityLevel)
	{
		if (visibilityLevel == newVisibilityLevel)
		{
			return;
		}
		visibilityLevel = newVisibilityLevel;
		switch (visibilityLevel)
		{
		case VisibilityLevel.Invisible:
		{
			for (int l = 0; l < rendererInfos.Length; l++)
			{
				CharacterModel.RendererInfo rendererInfo4 = rendererInfos[l];
				rendererInfo4.renderer.enabled = false;
				rendererInfo4.renderer.shadowCastingMode = ShadowCastingMode.Off;
			}
			break;
		}
		case VisibilityLevel.Cloaked:
		{
			for (int j = 0; j < rendererInfos.Length; j++)
			{
				CharacterModel.RendererInfo rendererInfo2 = rendererInfos[j];
				rendererInfo2.renderer.enabled = !rendererInfo2.ignoreOverlays && (!isDead || !rendererInfo2.hideOnDeath);
				rendererInfo2.renderer.shadowCastingMode = ShadowCastingMode.Off;
				rendererInfo2.renderer.material = CharacterModel.cloakedMaterial;
			}
			break;
		}
		case VisibilityLevel.Revealed:
		{
			for (int k = 0; k < rendererInfos.Length; k++)
			{
				CharacterModel.RendererInfo rendererInfo3 = rendererInfos[k];
				rendererInfo3.renderer.enabled = !rendererInfo3.ignoreOverlays && (!isDead || !rendererInfo3.hideOnDeath);
				rendererInfo3.renderer.shadowCastingMode = ShadowCastingMode.Off;
				rendererInfo3.renderer.material = CharacterModel.revealedMaterial;
			}
			break;
		}
		case VisibilityLevel.Visible:
		{
			for (int i = 0; i < rendererInfos.Length; i++)
			{
				CharacterModel.RendererInfo rendererInfo = rendererInfos[i];
				bool flag = !isDead || !rendererInfo.hideOnDeath;
				rendererInfo.renderer.enabled = flag;
				rendererInfo.renderer.shadowCastingMode = ((!flag) ? ShadowCastingMode.On : ShadowCastingMode.Off);
				rendererInfo.renderer.material = rendererInfo.defaultMaterial;
			}
			break;
		}
		}
	}

	public void OnDeath()
	{
		isDead = true;
		for (int i = 0; i < rendererInfos.Length; i++)
		{
			CharacterModel.RendererInfo rendererInfo = rendererInfos[i];
			if (rendererInfo.hideOnDeath)
			{
				rendererInfo.renderer.enabled = false;
				rendererInfo.renderer.shadowCastingMode = ShadowCastingMode.Off;
			}
		}
		GameObject[] array = hideOnDeathObjects;
		for (int j = 0; j < array.Length; j++)
		{
			array[j].SetActive(value: false);
		}
	}
}
