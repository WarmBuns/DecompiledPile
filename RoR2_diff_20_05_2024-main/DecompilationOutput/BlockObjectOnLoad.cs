using System;
using RoR2;
using UnityEngine;

public class BlockObjectOnLoad : MonoBehaviour
{
	private void Start()
	{
		if (!RoR2Application.loadFinished)
		{
			base.gameObject.SetActive(value: false);
			RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, (Action)delegate
			{
				base.gameObject.SetActive(value: true);
			});
		}
	}
}
