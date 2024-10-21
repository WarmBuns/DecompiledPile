using System;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.UI;

[Serializable]
public struct TooltipContent : IEquatable<TooltipContent>
{
	public string titleToken;

	public string overrideTitleText;

	public Color titleColor;

	public string bodyToken;

	public string overrideBodyText;

	public Color bodyColor;

	public bool disableTitleRichText;

	public bool disableBodyRichText;

	public bool Equals(TooltipContent other)
	{
		if (string.Equals(titleToken, other.titleToken) && string.Equals(overrideTitleText, other.overrideTitleText) && titleColor.Equals(other.titleColor) && string.Equals(bodyToken, other.bodyToken) && string.Equals(overrideBodyText, other.overrideBodyText) && bodyColor.Equals(other.bodyColor) && disableTitleRichText == other.disableTitleRichText)
		{
			return disableBodyRichText == other.disableBodyRichText;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is TooltipContent other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((((((((((((titleToken != null) ? titleToken.GetHashCode() : 0) * 397) ^ ((overrideTitleText != null) ? overrideTitleText.GetHashCode() : 0)) * 397) ^ titleColor.GetHashCode()) * 397) ^ ((bodyToken != null) ? bodyToken.GetHashCode() : 0)) * 397) ^ ((overrideBodyText != null) ? overrideBodyText.GetHashCode() : 0)) * 397) ^ bodyColor.GetHashCode()) * 397) ^ disableTitleRichText.GetHashCode()) * 397) ^ disableBodyRichText.GetHashCode();
	}

	public static bool operator ==(TooltipContent left, TooltipContent right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(TooltipContent left, TooltipContent right)
	{
		return !left.Equals(right);
	}

	[NotNull]
	public string GetTitleText()
	{
		if (!string.IsNullOrEmpty(overrideTitleText))
		{
			return overrideTitleText;
		}
		if (!string.IsNullOrEmpty(titleToken))
		{
			return Language.GetString(titleToken);
		}
		return string.Empty;
	}

	[NotNull]
	public string GetBodyText()
	{
		if (!string.IsNullOrEmpty(overrideBodyText))
		{
			return overrideBodyText;
		}
		if (!string.IsNullOrEmpty(bodyToken))
		{
			return Language.GetString(bodyToken);
		}
		return string.Empty;
	}
}
