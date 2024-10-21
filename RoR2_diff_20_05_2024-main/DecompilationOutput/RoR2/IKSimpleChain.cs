using UnityEngine;

namespace RoR2;

public class IKSimpleChain : MonoBehaviour
{
	public enum InnerAxis
	{
		Left,
		Right,
		Forward,
		Backward
	}

	public float scale = 1f;

	public int maxIterations = 100;

	public float positionAccuracy = 0.001f;

	private float posAccuracy = 0.001f;

	public float bendingLow;

	public float bendingHigh;

	public int chainResolution;

	private int startBone;

	private bool minIsFound;

	private bool bendMore;

	private Vector3 targetPosition;

	public float legLength;

	public float poleAngle;

	public InnerAxis innerAxis = InnerAxis.Right;

	private Transform tmpBone;

	public Transform ikPole;

	public Transform[] boneList;

	private bool firstRun = true;

	private IIKTargetBehavior ikTarget;

	private void Start()
	{
		ikTarget = GetComponent<IIKTargetBehavior>();
	}

	private void LateUpdate()
	{
		if (firstRun)
		{
			tmpBone = boneList[startBone];
		}
		if (ikTarget != null)
		{
			ikTarget.UpdateIKTargetPosition();
		}
		targetPosition = base.transform.position;
		legLength = CalculateLegLength(boneList);
		Solve(boneList, targetPosition);
		firstRun = false;
	}

	public bool LegTooShort(float legScale = 1f)
	{
		bool result = false;
		if ((targetPosition - boneList[0].transform.position).sqrMagnitude >= legLength * legLength * legScale * legScale)
		{
			result = true;
		}
		return result;
	}

	private float CalculateLegLength(Transform[] bones)
	{
		float[] array = new float[bones.Length - 1];
		float num = 0f;
		for (int i = startBone; i < bones.Length - 1; i++)
		{
			array[i] = (bones[i + 1].position - bones[i].position).magnitude;
			num += array[i];
		}
		return num;
	}

	public void Solve(Transform[] bones, Vector3 target)
	{
		Transform transform = bones[^1];
		Vector3[] array = new Vector3[bones.Length - 2];
		float[] array2 = new float[bones.Length - 2];
		Quaternion[] array3 = new Quaternion[bones.Length - 2];
		for (int i = startBone; i < bones.Length - 2; i++)
		{
			array[i] = Vector3.Cross(bones[i + 1].position - bones[i].position, bones[i + 2].position - bones[i + 1].position);
			array[i] = Quaternion.Inverse(bones[i].rotation) * array[i];
			array[i] = array[i].normalized;
			array2[i] = Vector3.Angle(bones[i + 1].position - bones[i].position, bones[i + 1].position - bones[i + 2].position);
			array3[i] = bones[i + 1].localRotation;
		}
		positionAccuracy = legLength * posAccuracy;
		float magnitude = (transform.position - bones[startBone].position).magnitude;
		float magnitude2 = (target - bones[startBone].position).magnitude;
		minIsFound = false;
		bendMore = false;
		if (magnitude2 >= magnitude)
		{
			minIsFound = true;
			bendingHigh = 1f;
			bendingLow = 0f;
		}
		else
		{
			bendMore = true;
			bendingHigh = 1f;
			bendingLow = 0f;
		}
		_ = array3.Length;
		int num = 0;
		while (Mathf.Abs(magnitude - magnitude2) > positionAccuracy && num < maxIterations)
		{
			num++;
			float num2 = (minIsFound ? ((bendingLow + bendingHigh) / 2f) : bendingHigh);
			for (int j = startBone; j < bones.Length - 2; j++)
			{
				float num3 = (bendMore ? (array2[j] * (1f - num2) + (array2[j] - 30f) * num2) : Mathf.Lerp(180f, array2[j], num2));
				Quaternion localRotation = Quaternion.AngleAxis(array2[j] - num3, array[j]) * array3[j];
				bones[j + 1].localRotation = localRotation;
			}
			magnitude = (transform.position - bones[startBone].position).magnitude;
			if (magnitude2 > magnitude)
			{
				minIsFound = true;
			}
			if (minIsFound)
			{
				if (magnitude2 > magnitude)
				{
					bendingHigh = num2;
				}
				else
				{
					bendingLow = num2;
				}
				if (bendingHigh < 0.01f)
				{
					break;
				}
			}
			else
			{
				bendingLow = bendingHigh;
				bendingHigh += 1f;
			}
		}
		if (firstRun)
		{
			tmpBone.rotation = bones[startBone].rotation;
		}
		bones[startBone].rotation = Quaternion.AngleAxis(Vector3.Angle(transform.position - bones[startBone].position, target - bones[startBone].position), Vector3.Cross(transform.position - bones[startBone].position, target - bones[startBone].position)) * bones[startBone].rotation;
		if ((bool)ikPole)
		{
			Vector3 position = bones[startBone].position;
			Vector3 up = bones[startBone].transform.up;
			Vector3 position2 = transform.position;
			Vector3 vector = Vector3.Cross(rhs: ikPole.position - position, lhs: position2 - position);
			Vector3 vector2 = Vector3.Cross(vector, up);
			Vector3 vecU = Vector3.zero;
			switch (innerAxis)
			{
			case InnerAxis.Right:
				vecU = bones[startBone].transform.right;
				break;
			case InnerAxis.Left:
				vecU = -bones[startBone].transform.right;
				break;
			case InnerAxis.Forward:
				vecU = bones[startBone].transform.forward;
				break;
			case InnerAxis.Backward:
				vecU = -bones[startBone].transform.forward;
				break;
			}
			float num4 = SignedAngle(vecU, vector2, up);
			num4 += poleAngle;
			bones[startBone].rotation = Quaternion.AngleAxis(num4, transform.position - bones[startBone].position) * bones[startBone].rotation;
			Debug.DrawLine(transform.position, bones[startBone].position, Color.red);
			Debug.DrawRay(bones[startBone].position, vector, Color.blue);
			Debug.DrawRay(bones[startBone].position, vector2, Color.yellow);
		}
		tmpBone = bones[startBone];
	}

	private float SignedAngle(Vector3 vecU, Vector3 vecV, Vector3 normal)
	{
		float num = Vector3.Angle(vecU, vecV);
		if (Vector3.Angle(Vector3.Cross(vecU, vecV), normal) < 1f)
		{
			num *= -1f;
		}
		return 0f - num;
	}

	private float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
	{
		float num = Vector3.Dot(Vector3.Cross(fwd, targetDir), up);
		if (num > 0f)
		{
			return 1f;
		}
		if (num < 0f)
		{
			return -1f;
		}
		return 0f;
	}
}
