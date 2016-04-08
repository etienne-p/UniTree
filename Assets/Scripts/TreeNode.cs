using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System.Collections.Generic;

public class TreeNode
{
	public int firstVertexIndex = 0;
	public int generation = 0;
	public int depth = 0;
	public int maxDepthAhead = 0;

	public Vector3 position, tangent, normal;
	public List<TreeNode> childs;
	public TreeNode parent = null;
	public TreeNode root = null;
	public TreeNode nextSibling = null; // useful for iterating over the tree

	public TreeNode()
	{
		childs = new List<TreeNode> ();
	}

	public TreeNode addChild()
	{
		var child = new TreeNode();
		child.parent = this;
		child.root = root;
		child.depth = this.depth + 1;
		if (childs.Count > 0) childs[childs.Count - 1].nextSibling = child;
		childs.Add(child);
		return child;
	}

	public void Clear()
	{
		foreach(var child in childs) child.Clear();
		nextSibling = null;
		parent = null;
		root = null;
		childs.Clear();
	}

	List<TreeNode> GetExtremities()
	{
		var it = MapNodeIter();
		var extremities = new List<TreeNode>();
		while(!it.IsDone())
		{
			if (it.Get().childs.Count == 0) extremities.Add(it.Get());
			it.Next();
		}
		return extremities;
	}

	public Vector3 ComputeCenter()
	{
		var it = MapNodeIter ();
		int c = 0;
		Vector3 v = Vector3.zero;
		while (!it.IsDone ()) {
			v += it.Get ().position;
			++c;
			it.Next ();
		}
		return v * (1.0f / (float)c);
	}

	public void ComputeMaxDepthAhead()
	{
		Assert.IsTrue(parent == null); // meant to run from root node
		var extremities = GetExtremities();
		// then propagate back from every extremity
		foreach(var extremity in extremities)
		{
			var node = extremity;
			node.maxDepthAhead = 0;
			while(node.parent != null)
			{
				node.parent.maxDepthAhead = Mathf.Max(node.parent.maxDepthAhead, node.maxDepthAhead + 1);
				node = node.parent;
			}
		}
	}
	/*
	void draw()
	{
		for (auto& child : childs)
		{
			cinder::gl::drawLine(position, child->position);
			child->draw();
		}
	}
	*/

	public class TreeNodeIter // Iterator
	{
		private TreeNode currentNode;
		private bool done;

		public TreeNodeIter(TreeNode node)
		{
			done = false;
			currentNode = node;
		}

		public bool IsDone() { return done; }

		public void Next() // post increment
		{
			if (currentNode.childs.Count > 0) // forward: go to first child
			{
				currentNode = currentNode.childs[0];
			}
			else // roll back to first unvisited parent node
			{
				while(currentNode.nextSibling == null)
				{
					if (currentNode.parent == null)
					{
						done = true;
						return;
					}
					currentNode = currentNode.parent;
				}
				currentNode = currentNode.nextSibling;
			}
		}

		public TreeNode Get() { return currentNode; }
	}

	public TreeNodeIter MapNodeIter() { return new TreeNodeIter(this); }
}
