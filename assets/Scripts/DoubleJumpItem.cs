using System;
using UnityEngine;

public class DoubleJumpItem : MonoBehaviour, IItem
{
	public static event Action<int> OnDoubleJumpCollect;
	public int step = 1;
	public void Collect()
	{
		OnDoubleJumpCollect.Invoke(step);
		Destroy(gameObject);
	}
}
