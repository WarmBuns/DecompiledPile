using UnityEngine;
using UnityEngine.Networking;

public class Spinner : MonoBehaviour
{
	private float randRotationSpeed;

	private int randNumX;

	private int randNumY;

	private int randNumZ;

	private float randZeroOrOneX;

	private float randZeroOrOneY;

	private float randZeroOrOneZ;

	private bool shouldUpdate = true;

	private void Start()
	{
		if (!NetworkServer.active)
		{
			base.enabled = false;
			return;
		}
		if (base.gameObject.isStatic)
		{
			base.enabled = false;
			return;
		}
		Random.Range(0f, 360f);
		Random.Range(0f, 360f);
		Random.Range(0f, 360f);
		Random.Range(0f, 360f);
		base.gameObject.transform.rotation = Random.rotationUniform;
		randRotationSpeed = Random.Range(0.2f, 1f) / Time.deltaTime;
		randNumX = Random.Range(0, 2);
		randNumY = Random.Range(0, 2);
		randNumZ = Random.Range(0, 2);
		randZeroOrOneX = randNumX;
		randZeroOrOneY = randNumY;
		randZeroOrOneZ = randNumZ;
		if (randZeroOrOneX == 0f && randZeroOrOneY == 0f && randZeroOrOneZ == 0f)
		{
			randZeroOrOneX = 1f;
			randZeroOrOneY = 1f;
			randZeroOrOneZ = 1f;
		}
	}

	private void OnBecameVisible()
	{
		shouldUpdate = true;
	}

	private void OnBecameInvisible()
	{
		shouldUpdate = false;
	}

	private void Update()
	{
		if (shouldUpdate)
		{
			base.gameObject.transform.Rotate(new Vector3(randZeroOrOneX, randZeroOrOneY, randZeroOrOneZ), randRotationSpeed * Time.deltaTime);
		}
	}
}
