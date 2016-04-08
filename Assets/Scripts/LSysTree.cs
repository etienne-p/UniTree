using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[ExecuteInEditMode]
public class LSysTree : MonoBehaviour 
{
	[Range(.0f, 90.0f)]
	public float baseAngle = 20.0f;
	[Range(1.0f, 60.0f)]
	public float baseDistance = 20.0f;
	[Range(.0f, 1.0f)]
	public float delta = .5f;
	[Range(.0f, 12.0f)]
	public float noiseFactor = 1.0f;
	[Range(.0f, .1f)]
	public float noiseScale = 1.0f;
	public Vector3 noiseOffset = Vector3.zero;
	[Range(1, 3)]
	public int generation = 2;
	public string rule = "|[<F][>F]|[^F][&F]|[+F][-F]|";

	public UnityEvent changed;

	TreeNode node = null;

	void OnValidate () 
	{
		Generate ();
	}
	
	void Generate () 
	{
		var lSys = new LSystem ();
		lSys.baseAngle = baseAngle;
		lSys.baseDistance = baseDistance;
		lSys.delta = delta;
		node = lSys.MakeTree (rule, generation);

		var it = node.MapNodeIter ();
		it.Next (); // pass root
		while(!it.IsDone())
		{
			var p = it.Get ().position + noiseOffset;
			var nx = Mathf.PerlinNoise (p.y * noiseScale, p.z * noiseScale);
			var ny = Mathf.PerlinNoise (p.x * noiseScale, p.z * noiseScale);
			var nz = Mathf.PerlinNoise (p.x * noiseScale, p.y * noiseScale);
			it.Get ().position += (new Vector3(nx, ny, nz) - Vector3.one * .5f) * 2.0f * noiseFactor;
			it.Next ();
		}

		changed.Invoke ();
	}

	public TreeNode GetTreeNode()
	{
		if (node == null)
			Generate ();
		return node;
	}
}
