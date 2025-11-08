using System.Runtime.CompilerServices;
using UnityEngine;

public class StateController : MonoBehaviour
{
	State currentState;
	
	public IdleState idleState = new IdleState();
	public WalkState walkState = new WalkState();
	public RunState runState = new RunState();
	public JumpState jumpState = new JumpState();
	public HurtState hurtState = new HurtState();
	
	private void Start()
	{
		ChangeState(idleState);
	}
	
	void Update()
	{
		if (currentState != null)
		{
			currentState.UpdateState();
		}
	}

	public void ChangeState(State state)
	{
		if (currentState != null)
		{
			currentState.OnExit();
		}
		currentState = state;
		currentState.OnStateEnter(this);
	}
}

public abstract class State
{
	protected StateController sc;

	public void OnStateEnter(StateController stateController)
	{
		sc = stateController;
		OnEnter();
	}

	protected virtual void OnEnter()
	{
		
	}

	public virtual void OnExit()
	{
		
	}

	public virtual void OnHurt()
	{
		
	}

	public virtual void UpdateState()
	{
	}
}
