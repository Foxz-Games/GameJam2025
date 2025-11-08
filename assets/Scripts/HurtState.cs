using UnityEngine;

public class HurtState : State
{
	protected override void OnEnter()
	{
		// start animation
	}

	public override void OnExit()
	{
		// cancel animation
	}

	public override void OnHurt()
	{
		// cancel animation
		sc.ChangeState(sc.hurtState);
	}
	
	public override void UpdateState()
	{
		
	}
}
