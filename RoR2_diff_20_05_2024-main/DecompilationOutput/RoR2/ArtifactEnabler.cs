using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class ArtifactEnabler : MonoBehaviour
{
	[SerializeField]
	private ArtifactDef artifactDef;

	private bool artifactWasEnabled;

	private void OnEnable()
	{
		if (NetworkServer.active && (bool)artifactDef)
		{
			artifactWasEnabled = RunArtifactManager.instance.IsArtifactEnabled(artifactDef);
			RunArtifactManager.instance.SetArtifactEnabledServer(artifactDef, newEnabled: true);
		}
	}

	private void OnDisable()
	{
		if (NetworkServer.active && (bool)artifactDef && (bool)RunArtifactManager.instance)
		{
			RunArtifactManager.instance.SetArtifactEnabledServer(artifactDef, artifactWasEnabled);
		}
	}
}
