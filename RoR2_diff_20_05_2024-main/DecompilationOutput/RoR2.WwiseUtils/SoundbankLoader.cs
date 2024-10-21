using System;
using UnityEngine;

namespace RoR2.WwiseUtils;

public class SoundbankLoader : MonoBehaviour
{
	public string[] soundbankStrings;

	public bool decodeBank;

	public bool saveDecodedBank;

	private static int pendingLoads;

	public static bool doneLoading => pendingLoads == 0;

	private void Start()
	{
		for (int i = 0; i < soundbankStrings.Length; i++)
		{
			Debug.LogFormat("Queueing Soundbank for load {0} ", soundbankStrings[i]);
			AkBankManager.LoadBankAsync(soundbankStrings[i], Callback);
			pendingLoads++;
		}
		Debug.LogFormat("Soundbanks queued, pendingLoads = {0}", pendingLoads);
	}

	private void Callback(uint in_bankID, IntPtr in_InMemoryBankPtr, AKRESULT in_eLoadResult, object in_Cookie)
	{
		pendingLoads--;
		if (in_eLoadResult != AKRESULT.AK_BankAlreadyLoaded)
		{
			Debug.LogFormat("Soundbank {0} loaded. {1} remaining", in_bankID, pendingLoads);
		}
		else
		{
			Debug.LogFormat("Duplicate Soundbank loaded. {0} remaining", pendingLoads);
		}
	}
}
