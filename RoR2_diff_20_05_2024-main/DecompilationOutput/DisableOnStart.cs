using UnityEngine;

public class DisableOnStart : MonoBehaviour
{
	private void Start()
	{
		base.gameObject.SetActive(value: false);
	}
}
