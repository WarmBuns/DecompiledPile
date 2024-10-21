using System.Collections;
using UnityEngine;

public class SetAkGameObjToStaticAfterDelay : MonoBehaviour
{
	[SerializeField]
	[Tooltip("Delay until setting this AkGameObj script as 'static' (not updating")]
	private float timeUntilSetToStatic = -1f;

	public static float defaultValueForDelayUntilSetToStatic = 10f;

	private void Awake()
	{
		if (timeUntilSetToStatic == -1f)
		{
			timeUntilSetToStatic = defaultValueForDelayUntilSetToStatic;
		}
		else
		{
			timeUntilSetToStatic = Mathf.Abs(timeUntilSetToStatic);
		}
		StartCoroutine(SetAkGameObjToStatic());
	}

	private IEnumerator SetAkGameObjToStatic()
	{
		yield return new WaitForSeconds(timeUntilSetToStatic);
		AkGameObjUtil.SetAkGameObjectStatic(base.gameObject, this);
		Object.Destroy(this);
	}
}
