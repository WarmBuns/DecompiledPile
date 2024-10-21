using UnityEngine;

public class SetRandomScale : MonoBehaviour
{
	public float minimumScale;

	public float maximumScale;

	private void Start()
	{
		float num = Random.Range(minimumScale, maximumScale);
		base.transform.localScale = Vector3.one * num;
	}
}
