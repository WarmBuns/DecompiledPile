namespace RoR2;

public struct HullDef
{
	public float height;

	public float radius;

	private static HullDef[] hullDefs;

	static HullDef()
	{
		hullDefs = new HullDef[3];
		hullDefs[0] = new HullDef
		{
			height = 2f,
			radius = 0.5f
		};
		hullDefs[1] = new HullDef
		{
			height = 8f,
			radius = 1.8f
		};
		hullDefs[2] = new HullDef
		{
			height = 20f,
			radius = 5f
		};
	}

	public static HullDef Find(HullClassification hullClassification)
	{
		return hullDefs[(int)hullClassification];
	}
}
