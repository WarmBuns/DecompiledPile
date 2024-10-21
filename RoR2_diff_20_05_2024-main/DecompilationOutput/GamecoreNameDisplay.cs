using RoR2.UI;
using UnityEngine;

public class GamecoreNameDisplay : MonoBehaviour
{
	public HGTextMeshProUGUI Text;

	public GameObject Parent;

	private void Awake()
	{
		Parent.SetActive(value: false);
	}
}
