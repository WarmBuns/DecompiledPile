using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RoR2;

public class MorsecodeFlasher : MonoBehaviour
{
	private string[] alphabet = new string[36]
	{
		".-", "-...", "-.-.", "-..", ".", "..-.", "--.", "....", "..", ".---",
		"-.-", ".-..", "--", "-.", "---", ".--.", "--.-", ".-.", "...", "-",
		"..-", "...-", ".--", "-..-", "-.--", "--..", "-----", ".----", "..---", "...--",
		"....-", ".....", "-....", "--...", "---..", "----."
	};

	public string morsecodeMessage;

	public float spaceDelay;

	public float delayBetweenCharacters;

	public float dotDuration;

	public float dashDuration;

	public float messageRepeatDelay;

	public GameObject flashRootObject;

	private float age;

	private void FixedUpdate()
	{
		age -= Time.fixedDeltaTime;
		if (age <= 0f)
		{
			age = messageRepeatDelay;
			PlayMorseCodeMessage(morsecodeMessage);
		}
	}

	public void PlayMorseCodeMessage(string message)
	{
		StartCoroutine("_PlayMorseCodeMessage", message);
	}

	private IEnumerator _PlayMorseCodeMessage(string message)
	{
		Regex regex = new Regex("[^A-z0-9 ]");
		message = regex.Replace(message.ToUpper(), "");
		string text = message;
		foreach (char c in text)
		{
			if (c == ' ')
			{
				yield return new WaitForSeconds(spaceDelay);
				continue;
			}
			int num = c - 65;
			if (num < 0)
			{
				num = c - 48 + 26;
			}
			string text2 = alphabet[num];
			string text3 = text2;
			foreach (char num2 in text3)
			{
				float num3 = dotDuration;
				if (num2 == '-')
				{
					num3 = dashDuration;
				}
				StartCoroutine("_FlashMorseCodeObject", num3);
				yield return new WaitForSeconds(num3 + delayBetweenCharacters);
			}
		}
	}

	private IEnumerator _FlashMorseCodeObject(float duration)
	{
		flashRootObject.SetActive(value: true);
		yield return new WaitForSeconds(duration);
		flashRootObject.SetActive(value: false);
	}
}
