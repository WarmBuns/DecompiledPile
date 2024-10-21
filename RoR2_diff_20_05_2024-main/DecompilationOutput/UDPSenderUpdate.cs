using System.Collections;
using UnityEngine;

public class UDPSenderUpdate : MonoBehaviour
{
	private void Awake()
	{
		Object.DontDestroyOnLoad(base.gameObject);
	}

	private void Start()
	{
	}

	private void Update()
	{
	}

	private IEnumerator EndlessSending()
	{
		yield return new WaitForEndOfFrame();
	}
}
