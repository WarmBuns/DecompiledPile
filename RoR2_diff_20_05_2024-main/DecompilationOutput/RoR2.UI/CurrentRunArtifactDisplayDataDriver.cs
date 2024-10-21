using UnityEngine;

namespace RoR2.UI;

public class CurrentRunArtifactDisplayDataDriver : MonoBehaviour
{
	public ArtifactDisplayPanelController artifactDisplayPanelController;

	private bool dirty;

	private void OnEnable()
	{
		RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabledGlobal;
		RunArtifactManager.onArtifactEnabledGlobal += OnArtifactDisabledGlobal;
		MarkDirty();
	}

	private void OnDisable()
	{
		RunArtifactManager.onArtifactEnabledGlobal -= OnArtifactDisabledGlobal;
		RunArtifactManager.onArtifactEnabledGlobal -= OnArtifactEnabledGlobal;
	}

	private void OnArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		MarkDirty();
	}

	private void OnArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		MarkDirty();
	}

	private void MarkDirty()
	{
		if (!dirty)
		{
			dirty = true;
			RoR2Application.onLateUpdate += Refresh;
		}
	}

	private void Refresh()
	{
		RunArtifactManager.RunEnabledArtifacts enabledArtifacts = RunArtifactManager.enabledArtifactsEnumerable.GetEnumerator();
		artifactDisplayPanelController.SetDisplayData(ref enabledArtifacts);
		dirty = false;
	}
}
