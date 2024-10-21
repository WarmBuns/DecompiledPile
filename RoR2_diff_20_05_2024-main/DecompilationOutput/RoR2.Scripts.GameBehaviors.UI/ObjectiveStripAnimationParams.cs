using System.Collections.Generic;
using UnityEngine;

namespace RoR2.Scripts.GameBehaviors.UI;

public class ObjectiveStripAnimationParams : MonoBehaviour
{
	[SerializeField]
	protected List<ObjectiveStripAnimationParam> Params;

	public float GetParam(string ParamName, float defaultValue)
	{
		int num = Params.FindIndex((ObjectiveStripAnimationParam objectiveStripAnimationParam) => objectiveStripAnimationParam.Param == ParamName);
		if (num >= 0)
		{
			return Params[num].Value;
		}
		return defaultValue;
	}
}
