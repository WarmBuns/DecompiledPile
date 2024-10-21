using RoR2.UI;
using UnityEngine;
using UnityEngine.Events;

public class SessionButtonController : MonoBehaviour
{
	public HGButton Button;

	public HGTextMeshProUGUI Text;

	public void AddListener(UnityAction call)
	{
		Button.onClick.AddListener(call);
	}

	public void SetText(int currentParticipationNumber, int maxParticipationNumber, string hostName)
	{
		Text.text = currentParticipationNumber + "/" + maxParticipationNumber + " " + hostName;
	}
}
