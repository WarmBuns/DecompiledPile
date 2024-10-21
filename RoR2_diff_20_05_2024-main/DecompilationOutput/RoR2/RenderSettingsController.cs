using UnityEngine;

namespace RoR2;

public class RenderSettingsController : MonoBehaviour
{
	public RenderSettingsState renderSettingsState;

	[ContextMenu("Copy from current render settings")]
	public void FromCurrent()
	{
		renderSettingsState = RenderSettingsState.FromCurrent();
	}

	[ContextMenu("Apply as current render settings")]
	public void Apply()
	{
		renderSettingsState.Apply();
	}

	private void LateUpdate()
	{
		Apply();
	}
}
