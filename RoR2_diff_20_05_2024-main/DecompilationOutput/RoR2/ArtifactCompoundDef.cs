using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/ArtifactCompoundDef")]
public class ArtifactCompoundDef : ScriptableObject
{
	public int value;

	public Material decalMaterial;

	public GameObject modelPrefab;
}
