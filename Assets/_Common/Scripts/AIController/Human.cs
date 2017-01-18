using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Enum
{
	public enum AnimationType : int
	{
		Generic = 0,
		Generic2 = 1,
		Female = 2
	}

	public enum Action
	{
		Standing,
		Walking,
		Running,
		Turning,
		SitDown,
		Sitting,
		StandUp
	}
}

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class Human : MonoBehaviour
{
	//public List<Node> PointsOfInterest = new List<Node>();

	public Node CurrentGoal = null;
	private UnityEngine.AI.NavMeshAgent _navAgent;
	private Animator _animator;

    public Enum.AnimationType AnimationType = Enum.AnimationType.Generic;

    public float _actionDuration = -1.0f;
	public float WalkingSpeed = 1.0f;
	public float RunningSpeed = 3.5f;
	public float RequiredAlignment = 0.3f;

	public bool HasUmbrella = false;
    public bool IsHurried = false;
	public bool WithinGroup = false;
	public string Group = "Untagged";

	//private RuntimeAnimatorController _defaultAnimationType = null;


	private Vector3 _goalDirection;

	public Enum.Action CurrentAction = Enum.Action.Standing;
	public void GoToNode(Node node)
	{
		CurrentGoal = node;
		CurrentAction = Enum.Action.Walking;
	}


	// Use this for initialization
	void Start ()
	{        
		Initialize();
		//_defaultAnimationType = _animator.runtimeAnimatorController;

	}

	void Initialize()
	{
		_navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
		_animator = GetComponent<Animator>();

        ApplyPersonality();

        //if (PointsOfInterest.Count > 0) CurrentGoal = PointsOfInterest[0];
        //_navAgent.SetDestination(CurrentGoal.transform.position);
        //Debug.Log(_navAgent.isOnNavMesh);
        //_navAgent.CalculatePath(CurrentGoal.transform.position, new NavMeshPath());
    }

	void OnValidate()
	{
		Initialize();
	}

	void OnDrawGizmos()
	{        
		float length = 1f;
		float height = _navAgent.height / 2 + transform.position.y;

		// Draw the sides
        if(CurrentGoal != null)
        {
            Gizmos.color = Color.blue;
            Vector3 dir = (CurrentGoal.transform.position - transform.position).normalized;
            Gizmos.DrawLine(IgnoreY(transform.position, height), IgnoreY(transform.position + (Quaternion.AngleAxis(RequiredAlignment * Mathf.Rad2Deg, Vector3.up) * dir) * length, height));
            Gizmos.DrawLine(IgnoreY(transform.position, height), IgnoreY(transform.position + (Quaternion.AngleAxis(-RequiredAlignment * Mathf.Rad2Deg, Vector3.up) * dir) * length, height));
        }

        // Draw the forward vector        
        if (LookingInDirection(_goalDirection, RequiredAlignment)) Gizmos.color = Color.green;
		else Gizmos.color = Color.red;
		Gizmos.DrawLine(IgnoreY(transform.position, height), IgnoreY(transform.position + transform.forward * length, height));       
	}

	Vector3 IgnoreY(Vector3 vec, float height = 0.0f) { return new Vector3(vec.x, height, vec.z); }

	// Update is called once per frame
	void Update ()
	{		
		//if (PointsOfInterest.Count == 0) return;

		if (_actionDuration > 0.0f)
			_actionDuration -= Time.deltaTime;// * World.Instance.Speed;

		// Update actions and check if they are completed
		UpdateAction();	
	}

	void UpdateAction()
	{
		switch (CurrentAction)
		{
			case Enum.Action.Standing:
				if (_actionDuration <= 0.0f) ActionCompleted();
				break;
			case Enum.Action.Walking:
			case Enum.Action.Running:
				if (GoalReached(0.1f)) ActionCompleted();
				break;
			case Enum.Action.Turning:
				if (LookingInDirection(_goalDirection, RequiredAlignment)) ActionCompleted();
				else
				{// Turn
					Vector3 dir = _goalDirection; // (_navAgent.steeringTarget - transform.position).normalized;
					//_navAgent.speed = 0.0f;
					//transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(dir), _navAgent.angularSpeed * Time.deltaTime * World.Speed);
					//transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + dir.y * _navAgent.angularSpeed * Time.deltaTime * World.Speed, transform.rotation.eulerAngles.z);

				float step = _navAgent.angularSpeed * Time.deltaTime;// * World.Instance.Speed;
					Vector3 newDir = Vector3.RotateTowards(transform.forward, dir, step, 0.0F);
					Debug.DrawRay(transform.position, newDir, Color.red);
					transform.rotation = Quaternion.LookRotation(newDir);
				}
				break;
			case Enum.Action.Sitting:
				if (_actionDuration <= 0.0f) ActionCompleted();
				break;
			default:
				break;
		}
		
		if (CurrentAction == Enum.Action.Running)
			_navAgent.speed = RunningSpeed;// * World.Instance.Speed;
		else _navAgent.speed = WalkingSpeed;// * World.Instance.Speed;

		_animator.SetFloat("MoveSpeed", _navAgent.velocity.magnitude);
	}	

	void ActionCompleted()
	{
		switch (CurrentAction)
		{
			case Enum.Action.Walking:
			case Enum.Action.Running:
				switch (CurrentGoal.Type)
				{
                    default:
                    case Node.NodeType.NavPoint:
                        //StartAction(Enum.Action.Standing);
                        StartAction(Enum.Action.Walking);
                        //CurrentAction = Enum.Action.Standing;
                        //GoToNewGoal();
                        break;
					case Node.NodeType.Door:
						break;
					case Node.NodeType.Vehicle:
						break;
					case Node.NodeType.Seat:
						_goalDirection = CurrentGoal.Direction;
						StartAction(Enum.Action.Turning);
						break;
				}
				break;
			case Enum.Action.Standing:
				GoToNewGoal();
				break;
			case Enum.Action.Turning:
				switch(CurrentGoal.Type)
				{
					case Node.NodeType.NavPoint:
						StartAction(Enum.Action.Standing);
						break;
					case Node.NodeType.Door:
						break;
					case Node.NodeType.Vehicle:
						break;
					case Node.NodeType.Seat:
						StartAction(Enum.Action.SitDown);
						break;
					default:
						break;
				}
				break;
			case Enum.Action.SitDown:
				StartAction(Enum.Action.Sitting);
				break;
			case Enum.Action.Sitting:
				StartAction(Enum.Action.StandUp);
				// Or maybe drink
				// Or talk
				break;
			default:
				break;
		}
	}

	void GoToNewGoal()
	{
		if(!WithinGroup)
		{
			if (CurrentGoal == null) CurrentGoal = Node.GetClosestNode(transform.position, "Untagged");
			else CurrentGoal = CurrentGoal.GetNextNode();//PointsOfInterest[Random.Range(0, PointsOfInterest.Count)];
		}
		else
			this.CurrentGoal = Node.GetRandomNode (this.Group);
		
        if (CurrentGoal == null) return;
		_navAgent.SetDestination(CurrentGoal.transform.position);
		_navAgent.enabled = true;
		CurrentAction = Enum.Action.Walking;
		_goalDirection = (CurrentGoal.transform.position - transform.position).normalized;
	}

	bool GoalReached(float maxRange)
	{
		return (transform.position - _navAgent.destination).magnitude <= maxRange;
	}

	void StartAction(Enum.Action action)
	{
		CurrentAction = action;

		switch (action)
		{
			case Enum.Action.Standing:
				_actionDuration = 2.0f;
				break;
			case Enum.Action.Walking:
			case Enum.Action.Running:
                GoToNewGoal();
				break;
			case Enum.Action.Turning:
				break;
			case Enum.Action.SitDown:
				_animator.SetTrigger("SitDown");
				_actionDuration = 15.0f;
				StartAction(Enum.Action.Sitting);
				break;
			case Enum.Action.Sitting:
				break;
			case Enum.Action.StandUp:
				_animator.SetTrigger("StandUp");                
				StartAction(Enum.Action.Standing);
				break;
			default:
				break;
		}
	}

	bool LookingAtTarget(Vector3 target, float maxOffset = 0.2f)
	{
		Vector3 direction = (target - transform.position).normalized;        
		return Vector3.Angle(transform.forward, direction) * Mathf.Deg2Rad <= maxOffset;
	}

	bool LookingInDirection(Vector3 direction, float maxOffset = 0.2f)
	{
		return Vector3.Angle(transform.forward, direction) * Mathf.Deg2Rad <= maxOffset;
	}

	bool IsOutside()
	{
		return true;
	}

    void ApplyPersonality()
    {
        _navAgent.avoidancePriority = Random.Range(30,70);

        // 10% chance to be hurried
        IsHurried = Random.value > 0.9f;

        //if (HasUmbrella)
        //{
        //    _animator.runtimeAnimatorController = AnimationType;
        //}
        //else
        //{
        //    _animator.runtimeAnimatorController = _defaultAnimationType;
        //}
    }
}