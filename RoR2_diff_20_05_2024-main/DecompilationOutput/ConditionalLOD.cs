using UnityEngine;

public class ConditionalLOD : MonoBehaviour
{
	public int PS4lod = -1;

	public int XBoxlod = -1;

	public int Switchlod = 1;

	public int SwitchHandHeldlod = 2;

	public int Defaultlod = -1;

	public int maxlod;

	private LODGroup lg;

	private void Start()
	{
	}

	private void OnEnable()
	{
		GetRenderers();
		int lODState = GetLODState();
		SetState(lODState);
	}

	public int GetLODPercentage(LOD[] lods, int i)
	{
		if (i == 0)
		{
			return 100;
		}
		return (int)(lods[i - 1].screenRelativeTransitionHeight * 100f + 0.5f);
	}

	private void ResetLOD()
	{
		GetRenderers();
	}

	private void GetRenderers()
	{
		lg = GetComponent<LODGroup>();
		LOD[] lODs = lg.GetLODs();
		int num = -1;
		for (int i = 0; i < lODs.Length; i++)
		{
			if (lODs[i].renderers != null && lODs[i].renderers.Length != 0)
			{
				num = i;
			}
		}
		maxlod = num;
	}

	public int GetLODState()
	{
		return Defaultlod;
	}

	private void SetState(int lodState)
	{
		LOD[] lODs = lg.GetLODs();
		if (lodState == -1)
		{
			lg.enabled = true;
			for (int i = 0; i < lODs.Length; i++)
			{
				Renderer[] renderers = lODs[i].renderers;
				for (int j = 0; j < renderers.Length; j++)
				{
					renderers[j].gameObject.SetActive(value: true);
				}
			}
			return;
		}
		for (int k = 0; k < lODs.Length; k++)
		{
			Renderer[] renderers = lODs[k].renderers;
			for (int j = 0; j < renderers.Length; j++)
			{
				renderers[j].gameObject.SetActive(value: false);
			}
		}
		lodState = Mathf.Min(lodState, maxlod);
		int num = maxlod - lodState + 1;
		if (num == 1 || !Application.isPlaying)
		{
			lg.enabled = false;
			Renderer[] renderers = lODs[lodState].renderers;
			for (int j = 0; j < renderers.Length; j++)
			{
				renderers[j].gameObject.SetActive(value: true);
			}
			return;
		}
		lg.enabled = true;
		int num2 = 0;
		LOD[] array = new LOD[num];
		for (int l = lodState; l < lODs.Length; l++)
		{
			Renderer[] renderers = lODs[l].renderers;
			for (int j = 0; j < renderers.Length; j++)
			{
				renderers[j].gameObject.SetActive(value: true);
			}
			array[num2] = lODs[l];
			if (l + 1 < lODs.Length)
			{
				array[num2].screenRelativeTransitionHeight = lODs[num2].screenRelativeTransitionHeight;
			}
			num2++;
		}
		lg.SetLODs(array);
	}
}
