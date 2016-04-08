using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(LSysTree))]
[RequireComponent(typeof(MeshFilter))]
public class TreeMesh : MonoBehaviour
{
	[Range(3, 8)]
	public int angularResolution = 4;
	[Range(1, 6)]
	public int segmentsPerLink = 3;
	[Range(.0f, 1.0f)]
	public float radiusMul = 1.0f;
	[Range(.0f, 10.0f)]
	public float hermiteFactor = 2.0f;

	TreeNode tree = null;

	void OnValidate()
	{
		MakeMesh ();
	}

	public void MakeMesh()
    {
		tree = GetComponent<LSysTree> ().GetTreeNode ();

        int indicesPerSegment = angularResolution * 2 * 3;
        
        // count nodes
        var nodesCount = 0;
        var iterator = tree.MapNodeIter();
        while(!iterator.IsDone())
        {
            ++nodesCount;
			iterator.Next();
        }
        
        // deduce number of indices and vertices
        var segmentCount = (nodesCount - 1) * segmentsPerLink;
        
        var indicesCount = segmentCount * indicesPerSegment;
        
        var verticesCount = segmentCount * angularResolution * 2;

		if (verticesCount > 65000)
		{
			Debug.LogError("Tree mesh has too much vertices, generation aborted.");
			return;
		}

		var meshFilter = GetComponent<MeshFilter> ();
		var mesh = meshFilter.sharedMesh == null ? new Mesh() : meshFilter.sharedMesh;

		Vector3[] vertices = new Vector3[verticesCount];
		Vector3[] normals = new Vector3[verticesCount];
		int[] triangles = new int[indicesCount];
        
        // positions must be evaluated before indices
        computeVertices(vertices, normals);
        
		computeIndices(triangles);
        
		mesh.Clear ();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.normals = normals;
		meshFilter.sharedMesh = mesh;
    }

	void computeIndices(int[] indices)
    {
        var iter = tree.MapNodeIter();
		var index = -1;
		iter.Next(); // pass root, every other node has a parent, and forms a segment with it
        while(!iter.IsDone())
        {
            for (var i = 0; i < segmentsPerLink; ++i)
            {
				var n = iter.Get().firstVertexIndex + i * angularResolution;
                var m = n + angularResolution;
                
                for (var j = 0; j < angularResolution; ++j)
                {
                    // Tri 1
                    indices[++index] = m + ((j + 1) %  angularResolution);
					indices[++index] = n + j;
					indices[++index] = m + j;

                    // Tri2
					indices[++index] = m + ((j + 1) %  angularResolution);
					indices[++index] = n + ((j + 1) %  angularResolution);
					indices[++index] = n + j;
                }
            }
			iter.Next();
        }
    }
    
	void computeVertices(Vector3[] positions, Vector3[] normals)
    {
		var dAngle = 360.0f / (float)angularResolution;
		var dt = .8f * (1.0f / (float)segmentsPerLink);
        var iter = tree.MapNodeIter();
        var currentVertexIndex = 0;

		iter.Next(); // pass root node

        while(!iter.IsDone())
        {
			var node = iter.Get();
			node.firstVertexIndex = currentVertexIndex; // node is reached at t = 1

            for (var i = 0; i < segmentsPerLink + 1; ++i)
            {
                // time on segment, 0 : parent, 1 : child (current iterator position)
                // remember that we pushed indices with child first
                var t = 1.0f - (float)i / (float)(segmentsPerLink);
                
                // linear interpolation of radius
				var maxDepthAhead = t * (float)node.maxDepthAhead + (1.0f - t) *
					(float)node.parent.maxDepthAhead;
                
				var tangent = Vector3.zero;
				var position = Vector3.zero;
                
                if (t == .0f)
                {
					position = node.parent.position;
					tangent = node.parent.tangent;
                }
                else if (t == 1.0f)
                {
					position = node.position;
					tangent = node.tangent;
                }
                else
                {
					position = Util.Hermite(
						node.parent.position,
						node.parent.tangent * hermiteFactor,
						node.position,
						node.tangent * hermiteFactor, t);
                    
                    var a = Util.Hermite(
						node.parent.position,
						node.parent.tangent * hermiteFactor,
						node.position,
						node.tangent * hermiteFactor, t + dt);
                    
					var b = Util.Hermite(
						node.parent.position,
						node.parent.tangent * hermiteFactor,
						node.position,
						node.tangent * hermiteFactor, t - dt);
                    
                    tangent = a - b;
                }
                
				var normal = Vector3.Slerp (node.parent.normal, node.normal, t);

                for (var j = 0; j < angularResolution; ++j)
                {
					var rot = Quaternion.AngleAxis((float)j * dAngle, tangent);
					var norm = rot * normal;
					positions [currentVertexIndex] = position + norm * radiusMul * maxDepthAhead;
					normals [currentVertexIndex] = norm.normalized;
                    ++currentVertexIndex;
                }
            }
			iter.Next();
        }
    }
}

