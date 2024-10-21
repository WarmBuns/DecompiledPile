using System;
using RoR2;
using RoR2.UI;
using UnityEngine;

public class EOSTogglePopup : MonoBehaviour
{
	public string titleText = "EOS_TOGGLE_POPUP_TITLE";

	public string messageText = "EOS_TOGGLE_POPUP_MESSAGE";

	public string closeNowButtonText = "EOS_TOGGLE_CLOSE_NOW";

	public string closeLaterButtonText = "EOS_TOGGLE_CLOSE_LATER";

	public void emitMessage()
	{
		SimpleDialogBox dialogBox = SimpleDialogBox.Create();
		Action deactiveCrossplayAndRestartFunction = delegate
		{
			if ((bool)dialogBox)
			{
				RoR2.Console.instance.SubmitCmd(null, "quit");
			}
		};
		dialogBox.AddActionButton(delegate
		{
			deactiveCrossplayAndRestartFunction();
		}, closeNowButtonText, true);
		dialogBox.AddCancelButton(closeLaterButtonText);
		dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair
		{
			token = titleText,
			formatParams = Array.Empty<object>()
		};
		dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair
		{
			token = messageText,
			formatParams = Array.Empty<object>()
		};
	}
}
