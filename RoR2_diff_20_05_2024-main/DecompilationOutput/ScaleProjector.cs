using UnityEngine;

public class ScaleProjector : MonoBehaviour
{
	private Projector projector;

	private void Start()
	{
		projector = GetComponent<Projector>();
	}

	private void Update()
	{
		if ((bool)projector)
		{
			projector.orthographicSize = base.transform.lossyScale.x;
		}
	}
}
