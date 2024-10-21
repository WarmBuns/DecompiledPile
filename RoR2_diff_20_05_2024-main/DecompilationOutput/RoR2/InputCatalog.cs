using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Rewired;

namespace RoR2;

public static class InputCatalog
{
	private struct ActionAxisPair : IEquatable<ActionAxisPair>
	{
		[NotNull]
		private readonly string actionName;

		private readonly AxisRange axisRange;

		public ActionAxisPair([NotNull] string actionName, AxisRange axisRange)
		{
			this.actionName = actionName;
			this.axisRange = axisRange;
		}

		public bool Equals(ActionAxisPair other)
		{
			if (string.Equals(actionName, other.actionName))
			{
				return axisRange == other.axisRange;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is ActionAxisPair)
			{
				return Equals((ActionAxisPair)obj);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((-1879861323 * -1521134295 + base.GetHashCode()) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(actionName)) * -1521134295 + axisRange.GetHashCode();
		}
	}

	private static readonly Dictionary<ActionAxisPair, string> actionToToken;

	static InputCatalog()
	{
		actionToToken = new Dictionary<ActionAxisPair, string>();
		Add("MoveHorizontal", "ACTION_MOVE_HORIZONTAL", AxisRange.Full);
		Add("MoveVertical", "ACTION_MOVE_VERTICAL", AxisRange.Full);
		Add("AimHorizontalMouse", "ACTION_AIM_HORIZONTAL_MOUSE", AxisRange.Full);
		Add("AimVerticalMouse", "ACTION_AIM_VERTICAL_MOUSE", AxisRange.Full);
		Add("AimHorizontalStick", "ACTION_AIM_HORIZONTAL_STICK", AxisRange.Full);
		Add("AimVerticalStick", "ACTION_AIM_VERTICAL_STICK", AxisRange.Full);
		Add("Jump", "ACTION_JUMP", AxisRange.Full);
		Add("Sprint", "ACTION_SPRINT", AxisRange.Full);
		Add("Interact", "ACTION_INTERACT", AxisRange.Full);
		Add("Equipment", "ACTION_EQUIPMENT", AxisRange.Full);
		Add("PrimarySkill", "ACTION_PRIMARY_SKILL", AxisRange.Full);
		Add("SecondarySkill", "ACTION_SECONDARY_SKILL", AxisRange.Full);
		Add("UtilitySkill", "ACTION_UTILITY_SKILL", AxisRange.Full);
		Add("SpecialSkill", "ACTION_SPECIAL_SKILL", AxisRange.Full);
		Add("Info", "ACTION_INFO", AxisRange.Full);
		Add("Ping", "ACTION_PING", AxisRange.Full);
		Add("MoveHorizontal", "ACTION_MOVE_HORIZONTAL_POSITIVE", AxisRange.Positive);
		Add("MoveHorizontal", "ACTION_MOVE_HORIZONTAL_NEGATIVE", AxisRange.Negative);
		Add("MoveVertical", "ACTION_MOVE_VERTICAL_POSITIVE", AxisRange.Positive);
		Add("MoveVertical", "ACTION_MOVE_VERTICAL_NEGATIVE", AxisRange.Negative);
		static void Add(string actionName, string token, AxisRange axisRange)
		{
			actionToToken[new ActionAxisPair(actionName, axisRange)] = token;
		}
	}

	public static string GetActionNameToken(string actionName, AxisRange axisRange = AxisRange.Full)
	{
		if (actionToToken.TryGetValue(new ActionAxisPair(actionName, axisRange), out var value))
		{
			return value;
		}
		throw new ArgumentException($"Bad action/axis pair {actionName} {axisRange}.");
	}
}
