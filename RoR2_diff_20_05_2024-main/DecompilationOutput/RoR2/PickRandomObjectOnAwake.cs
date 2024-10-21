using UnityEngine;

namespace RoR2;

public class PickRandomObjectOnAwake : MonoBehaviour
{
	public GameObject[] ObjectsToSelect;

	private void Awake()
	{
		int num = Random.Range(0, ObjectsToSelect.Length);
		for (int i = 0; i < ObjectsToSelect.Length; i++)
		{
			ObjectsToSelect[i].SetActive(i == num);
		}
	}
}
