using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Node : MonoBehaviour
{
	public static readonly Dictionary<string, List<Node>> WorldNodes = new Dictionary<string, List<Node>>();
	public static readonly List<Node> testNodes = new List<Node>();

	public enum NodeType
	{
		NavPoint,
		Door,
		Vehicle,
		Seat,
		Crosswalk
	}

	public NodeType Type;
	public int Area = -1;
	public Vector3 Direction { get { return transform.forward; } }    

	// pas aan als je de lijnen wil zien of niet (true or false)
	public static bool ShowLines = false;

	public List<Node> Neighbours = new List<Node>();
	private const int NeighbourCount = 3;

	void OnDrawGizmos()
	{
		Gizmos.color = Color.white;
		if(ShowLines==true) Gizmos.DrawLine(transform.position, transform.position + (Direction.normalized * 0.5f));
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(transform.position, 0.1f);

		if(Neighbours != null && ShowLines==true)
		{
			Gizmos.color = Color.white;
			for (int i = 0; i < Neighbours.Count; ++i)
			{
				Gizmos.DrawLine(transform.position, Neighbours[i].transform.position);
			}
		}        
	}

	// Use this for initialization
	void OnAwake()
	{
		addToWorldNodes ();
	}

	public static void BuildWorldNodes()
	{
		Object[] nodesInScene = Object.FindObjectsOfType(typeof(Node));
		Node cur;
		WorldNodes.Clear();
		for (int i = 0; i < nodesInScene.Length; ++i)
		{
			cur = (Node)nodesInScene[i];
			if (cur != null)
			{
				addKeyToWorldNodes (cur.tag);
				WorldNodes[cur.tag].Add(cur);
			}
		}
		foreach(KeyValuePair<string, List<Node>> node in WorldNodes)
		{
			for (int j = 0; j < node.Value.Count; ++j)
			{
				node.Value[j].AssignNeighbours();
			}
		}
	}

	private static void addKeyToWorldNodes(string key)
	{
		if (!WorldNodes.ContainsKey(key))
		{
			WorldNodes.Add (key, new List<Node> ());
		}
	}

	private void addToWorldNodes()
	{
		addKeyToWorldNodes (this.tag);
		if(!WorldNodes[this.tag].Contains(this))
		{
			WorldNodes[this.tag].Add(this);
		}
	}

	void Start ()
	{
		addToWorldNodes ();
		if (!testNodes.Contains(this)) testNodes.Add(this);	}

	void OnValidate()
	{
		//if (!WorldNodes.Contains(this)) WorldNodes.Add(this);
	}

	void OnDestroy()
	{
		if (WorldNodes[this.tag].Contains(this)) WorldNodes[this.tag].Remove(this);
	}

	// Update is called once per frame
	void Update ()
	{

	}

	public void AssignNeighbours()
	{
		Neighbours.Clear();

		for (int i = 0; i < WorldNodes [this.tag].Count; ++i)
		{
			if(WorldNodes [this.tag][i] != this && IsGoodNeighbour(WorldNodes [this.tag][i]))
			{
				if (Neighbours.Count < NeighbourCount) Neighbours.Add(WorldNodes [this.tag][i]);
				else
				{					
					for (int j = 0; j < Neighbours.Count; ++j)
					{
						// Isn't a neighbour already
						if (!Neighbours.Contains(WorldNodes [this.tag][i]))
						{
							// Is the furthest neighbour 
							Node furthestNeighbour = GetFurthestNeighbour();
							if (DistanceToNode(furthestNeighbour.transform.position) > DistanceToNode(WorldNodes [this.tag][i].transform.position))
							{
								Neighbours.Remove(furthestNeighbour);
								Neighbours.Add(WorldNodes [this.tag][i]);
							}
						}						
					}
				}
			}			
		}
	}    
	private Node GetFurthestNeighbour()
	{
		Node furthest = Neighbours[0];
		for (int i = 1; i < Neighbours.Count; ++i)
		{
			if(DistanceToNode(Neighbours[i].transform.position) > DistanceToNode(furthest.transform.position))
			{
				furthest = Neighbours[i];
			}
		}

		return furthest;
	}

	public static Node GetRandomNode(string group)
	{
		Node newNode;
		do
		{
			newNode = testNodes[Random.Range(0, testNodes.Count)];
		}
		while(newNode.tag != group);
		return newNode;
	}

	public static Node GetClosestNode(Vector3 pos, string group)
	{
		Node closest = null;
		// Get first 
		int s = 0;
		for (int i = 0; i < WorldNodes [group].Count && closest == null; ++i)
		{
			if (WorldNodes [group][i].Area != -1) closest = WorldNodes [group][i];
			s = i;
		}

		//if (closest == null) return null;

		for (int i = s; i < WorldNodes [group].Count; ++i)
		{
			if ((pos - closest.transform.position).magnitude > (pos - WorldNodes [group][i].transform.position).magnitude && WorldNodes [group][i].Area != -1)
			{
				closest = WorldNodes [group][i];
			}
		}

		return closest;
	}

	private float DistanceToNode(Vector3 pos)
	{
		return (pos - transform.position).magnitude;
	}

	private bool IsGoodNeighbour(Node potentialNeighbour)
	{
		// Good neighbour if not
		// A seat and neighbour is a seat
		bool good = !(Type == NodeType.Seat && potentialNeighbour.Type == NodeType.Seat);

		// Is in the same area OR is a crosswalk

		good &= Area == potentialNeighbour.Area || (Type == NodeType.Crosswalk && potentialNeighbour.Type == NodeType.Crosswalk);

		return good;
	}

	public Node GetNextNode()
	{
		if (Neighbours.Count == 0) return null;    
		Node next = Neighbours[Random.Range(0, Neighbours.Count - 1)];

		// Add bias away from current node? So character doesn't have such a big chance to go back

		return next;
	}
}