using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class LSystem : MonoBehaviour 
{
	[Range(.0f, 90.0f)]
	public float baseAngle = 20.0f;
	[Range(1.0f, 60.0f)]
	public float baseDistance = 20.0f;
	[Range(.0f, 1.0f)]
	public float delta = .5f;
	[Range(.0f, 24.0f)]
	public float noiseFactor = 1.0f;
	[Range(.0f, 1.0f)]
	public float noiseScale = .02f;
	public Vector3 noiseOffset = Vector3.zero;
	[Range(1, 6)]
	public int generation = 2;
	public string rule = "|[<F][>F]|[^F][&F]|[+F][-F]|";

	public UnityEvent changed;

	TreeNode node = null;
	LinkedList<DrawState> stateStack;
	DrawState currentState;

	void OnValidate () 
	{
		Generate ();
	}
	
	void Generate () 
	{
		// validate the rule
		var nPop = 0;
		var nPush = 0;
		foreach (char c in rule)
		{
			switch(c)
			{
			case '[': nPush++; break;
			case ']': nPop++;  break;
			}
		}

		if (nPop > nPush) 
		{
			Debug.LogError ("rule has to have more Pop than Push operations");
			return;
		}

		stateStack = new LinkedList<DrawState> ();
		currentState = new DrawState();
		node = MakeTree (rule, generation);

		var it = node.MapNodeIter ();
		it.Next (); // pass root
		while(!it.IsDone())
		{
			it.Get ().position += Perlin3D (it.Get ().position);
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

	public TreeNode MakeTree(string rule, int generation)
	{
		var root = new TreeNode();

		root.root = root;
		root.generation = 0;
		root.position = Vector3.zero;
		root.tangent = Vector3.up;
		root.normal = Vector3.forward;

		currentState = new DrawState();
		currentState.node = root;
		currentState.position = root.position;
		currentState.tangent = root.tangent;
		currentState.normal = root.normal;
		currentState.rotation = Quaternion.identity;
		currentState.angle = baseAngle;
		currentState.distance = baseDistance;

		ProcessNode(root, rule, 0, generation);

		root.ComputeMaxDepthAhead ();

		return root;
	}

	struct DrawState
	{
		public TreeNode node;
		public Vector3 position, tangent, normal;
		public Quaternion rotation;
		public float angle;
		public float distance;
	};

	void PushState() { stateStack.AddLast(currentState); }

	void PopState()
	{
		currentState = stateStack.Last.Value;
		stateStack.RemoveLast();
	}

	void ProcessNode(TreeNode node, string rule, int generation, int maxGeneration)
	{
		var val = 1.0f;
		foreach (char c in rule)
		{
			if (Char.IsDigit(c)) // intercept value updates
			{
				int i = c - '0'; // convert char to int
				val = (float)i;
			}
			else
			{
				ProcessCmd(c, rule, val, generation, maxGeneration);
				val = 1.0f; // reset val;
			}
		}
	}

	void AppendNode(int generation)
	{
		currentState.node = currentState.node.addChild();
		currentState.node.position = currentState.position;
		currentState.node.tangent = currentState.tangent;
		currentState.node.normal = currentState.normal;
		currentState.node.generation = generation;
	}

	void Rotate(Quaternion q)
	{
		currentState.rotation *= q;
		currentState.tangent = q * currentState.tangent;

		float angle = .0f;
		Vector3 axis = Vector3.zero;
		q.ToAngleAxis (out angle, out axis);

		// prevent "twisted" mesh in case the rotation happens along an axis colinear to the current tangent
		if (Mathf.Abs(Vector3.Dot (axis, currentState.tangent)) != 1.0f) 
		{
			currentState.normal = q * currentState.normal;
		}
	}

	void ProcessCmd(char cmd, string rule, float val, int generation, int maxGeneration)
	{
		switch (cmd)
		{
		case '|':
			{
				currentState.distance = baseDistance * Mathf.Pow (delta, generation);
				currentState.position += currentState.rotation * new Vector3(.0f, currentState.distance * val, .0f);
				AppendNode (generation);
			}
			break;

		case 'F':
			{
				currentState.position += currentState.rotation * new Vector3(.0f, currentState.distance * val, .0f);
				AppendNode (generation);
				if (generation < maxGeneration) ProcessNode(currentState.node, rule, generation + 1, maxGeneration);
			}
			break;

		case '[': PushState(); break;
		case ']': PopState();  break;

		case '+': Rotate(Quaternion.Euler(.0f, currentState.angle * val,         .0f)); break;
		case '-': Rotate(Quaternion.Euler(.0f, currentState.angle * val * -1.0f, .0f)); break;

		case '^': Rotate(Quaternion.Euler(currentState.angle * val,         .0f, .0f)); break;
		case '&': Rotate(Quaternion.Euler(currentState.angle * val * -1.0f, .0f, .0f)); break;

		case '>': Rotate(Quaternion.Euler(.0f, .0f, currentState.angle * val));         break;
		case '<': Rotate(Quaternion.Euler(.0f, .0f, currentState.angle * val * -1.0f)); break;

		default: break;
		}
	}

	Vector3 Perlin3D(Vector3 input)
	{
		var p = input + noiseOffset;
		var nx = Mathf.PerlinNoise (p.y * noiseScale, p.z * noiseScale);
		var ny = Mathf.PerlinNoise (p.x * noiseScale, p.z * noiseScale);
		var nz = Mathf.PerlinNoise (p.x * noiseScale, p.y * noiseScale);
		return (new Vector3(nx, ny, nz) - Vector3.one * .5f) * 2.0f * noiseFactor;
	}
}
