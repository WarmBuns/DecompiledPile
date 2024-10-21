using UnityEngine;

public class POISecretChest : MonoBehaviour
{
	public float influence = 5f;

	private void OnDrawGizmos()
	{
		Gizmos.matrix = base.transform.localToWorldMatrix;
		Gizmos.color = new Color(0f, 1f, 1f, 0.03f);
		Gizmos.DrawCube(Vector3.zero, base.transform.localScale / 2f);
		Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
		Gizmos.DrawWireCube(Vector3.zero, base.transform.localScale / 2f);
	}
}
