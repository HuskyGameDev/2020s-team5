﻿//
// When We Fell
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Flags]
public enum MoveState
{
	Normal,
	Flying,
	Climbing
}

public class Player : Entity
{
	public float jumpVelocity;
	public float gravity;
	public float maxHealth = 20;

	public bool flying;
	public int jumps;

	private float savedHealth = -1.0f;

	public int[] collectables = new int[3];

	private void Start()
	{
		damage = 5;
		jumps = 0;
		invincibleWait = new WaitForSeconds(0.5f);

		for(int i = 0; i < collectables.Length; i++)
			collectables[i] = 0;

		if (PlayerPrefs.HasKey("Health"))
		{
			health = PlayerPrefs.GetFloat("Health");
			savedHealth = health;
		}
	}

	private Vector2 SetNormal()
	{
		Vector2 accel = new Vector2(Input.GetAxisRaw("Horiz"), 0.0f);

		if (jumps < 1)
		{
			if (Input.GetButtonDown("jump"))
			{
				audioManager.Play("Jump");
                velocity.y = jumpVelocity;
				jumps++;
			}
		}

		if (CollidedBelow())
			jumps = 0;

		return accel;
	}

	private Vector2 SetFlying()
	{
		Vector2 accel = new Vector2(Input.GetAxisRaw("Horiz"), Input.GetAxisRaw("Vert"));

		if (accel != Vector2.zero)
			accel = accel.normalized;

		return accel;
	}

	private Vector2 SetClimbing()
	{
		Vector2 accel = new Vector2(Input.GetAxisRaw("Horiz"), Input.GetAxisRaw("Vert"));

		if (accel != Vector2.zero)
			accel = accel.normalized;

		jumps = 0;
		return accel;
	}

	private void Update()
	{
		Vector2 accel;
		float currentGravity = gravity;

		if (Debug.isDebugBuild && Input.GetKeyDown(KeyCode.Tab))
			flying = !flying;

		if (flying)
		{
			accel = SetFlying();
			currentGravity = 0.0f;
		}
		else
		{
			if ((moveState & MoveState.Climbing) != 0)
			{
                accel = SetClimbing();
				currentGravity = 0.0f;
			}
			else accel = SetNormal();
		}

		Move(accel, currentGravity);

		if(accel != Vector2.zero)
            PlayAnimation("Walking animation");
		else
		{
            audioManager.Play("Walk");
            PlayAnimation("Static animation");
		}

		// If health doesn't match saved health, write the health
		// to disk. Any time health changes, this will run.
		if (!Mathf.Approximately(health, savedHealth))
		{
			PlayerPrefs.SetFloat("Health", health);
			savedHealth = health;
		}
	}

	protected override void HandleOverlaps(List<CollideResult> overlaps)
	{
		for (int i = 0; i < overlaps.Count; ++i)
		{
			CollideResult result = overlaps[i];

			if (result.entity == null)
			{
				TileData data = TileManager.GetData(result.tile);

				if (data.overlapType == TileOverlapType.Climb)
					moveState |= MoveState.Climbing;

				if (result.tile == TileType.EndLevelTile)
					SceneManager.LoadScene("Game");
			}
		}
	}

	protected override void OnKill()
	{
		rend.enabled = false;
		enabled = false;
		GetComponent<PlayerAttack>().enabled = false;
		StartCoroutine(LoadGameOver());
	}

	private IEnumerator LoadGameOver()
	{
		yield return new WaitForSeconds(3.0f);
		SceneManager.LoadScene("Lose Menu");
	}
}
