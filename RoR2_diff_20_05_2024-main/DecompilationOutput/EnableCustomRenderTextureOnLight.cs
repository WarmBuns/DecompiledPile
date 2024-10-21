using UnityEngine;

[RequireComponent(typeof(Light))]
public class EnableCustomRenderTextureOnLight : MonoBehaviour
{
	private Light targetLight;

	private bool targetLightHasCustomRenderTexture;

	private CustomRenderTexture targetLightCookie;

	private void Awake()
	{
		targetLight = GetComponent<Light>();
		InitRenderTexture();
	}

	private void Start()
	{
		SetUpdateMode(CustomRenderTextureUpdateMode.Realtime);
	}

	private void InitRenderTexture()
	{
		if (!(targetLight == null) && !(targetLight.cookie == null) && targetLight.cookie is CustomRenderTexture)
		{
			targetLightHasCustomRenderTexture = true;
			targetLightCookie = targetLight.cookie as CustomRenderTexture;
		}
	}

	private void SetUpdateMode(CustomRenderTextureUpdateMode mode)
	{
		if (targetLightHasCustomRenderTexture)
		{
			targetLightCookie.updateMode = mode;
		}
	}

	private void OnEnable()
	{
		SetUpdateMode(CustomRenderTextureUpdateMode.Realtime);
	}

	private void OnDisable()
	{
		SetUpdateMode(CustomRenderTextureUpdateMode.OnDemand);
	}

	private void OnDestroy()
	{
		SetUpdateMode(CustomRenderTextureUpdateMode.OnDemand);
	}
}
