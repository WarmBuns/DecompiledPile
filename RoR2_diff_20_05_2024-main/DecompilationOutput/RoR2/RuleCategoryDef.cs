using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class RuleCategoryDef
{
	public int position;

	public string displayToken;

	public string subtitleToken;

	public string emptyTipToken;

	public string editToken;

	public Color color;

	public List<RuleDef> children = new List<RuleDef>();

	public RuleCatalog.RuleCategoryType ruleCategoryType;

	public Func<bool> hiddenTest;

	public bool isHidden => hiddenTest?.Invoke() ?? false;
}
