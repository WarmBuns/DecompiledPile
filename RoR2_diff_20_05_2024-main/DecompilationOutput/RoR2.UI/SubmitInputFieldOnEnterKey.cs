using TMPro;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(TMP_InputField))]
public class SubmitInputFieldOnEnterKey : MonoBehaviour
{
	private TMP_InputField inputField;

	private void Awake()
	{
		inputField = GetComponent<TMP_InputField>();
	}

	private void Update()
	{
		if (inputField.isFocused && inputField.text != "")
		{
			Input.GetKeyDown(KeyCode.Return);
		}
	}
}
