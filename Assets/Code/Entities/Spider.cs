﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class Spider : Entity
{
	public GameObject BabySpider;
    public float jumpVelocity;
	public float gravity;
	public bool aggro;
	public GameObject player;
	public bool collide;
	public bool facing;
    private float rotation;

	private void Start()
		=> player = GameObject.FindWithTag("Player");

    private void Update()
    {
        float PlayerY = player.transform.position.y;
		float PlayerX = player.transform.position.x;

		rotation = 0.0f;

		if (Math.Abs(PlayerX - Position.x) <= 6 && Math.Abs(PlayerY - Position.y) < 6)
		{
            audioManager.Play("Spider Cry");
            aggro = true;
		}

		if (Math.Abs(PlayerX - Position.x) >= 12 || Math.Abs(PlayerY - Position.y) >= 12)
			aggro = false;

		Vector2 accel = Vector2.zero;

        if (PlayerX < Position.x && aggro)
		{
			if(Math.Abs(PlayerX - Position.x) >= 0.9f)
			{
				accel = Vector2.left;
				facing = true;
			}

			SetFacingDirection(true);
		}
		else if (aggro)
		{
			if(Math.Abs(PlayerX - Position.x) >= 0.9f)
			{
				accel = Vector2.right;
				facing = false;
			}

			SetFacingDirection(false);
		}

		if (CollidedLeft() || CollidedRight() && aggro)
		{
			velocity.y = jumpVelocity;
            gravity = 0;
			if (facing)
				rotation = -90f;
			else rotation = 90f;
		}
		else if (CollidedBelow() && aggro)
        {
			collide = false;
            gravity = -30;

        }
		else if (CollidedAbove() && aggro)
        {
            rotation = 180.0f;
            gravity = 0;
        }
		else gravity = -30;

		Vector3 pivot = Position;
		pivot.y += 0.5f;

		transform.rotation = Quaternion.Euler(0.0f, 0.0f, rotation);
		Move(accel, gravity);
    }

	protected override void OnCollide(CollideResult col)
	{
		if (CollidedSides() && CollidedBelow())
			collide = true;
    }

	protected override void OnKill()
	{
		int revive = Random.Range(1, 26);
		player.GetComponent<Player>().enemiesKilled += 1;
		if (revive == 13)
			Instantiate(BabySpider, transform.position, Quaternion.identity);

		base.OnKill();
	}

	protected override void HandleOverlaps(List<CollideResult> overlaps)
	{
		for (int i = 0; i < overlaps.Count; ++i)
		{
			CollideResult result = overlaps[i];
			Entity target = result.entity;

			if (target != null && target is Player)
			{
				Vector2 diff = PositionDifference(target);

				if (diff.y > 0.4f)
				{
					float direction = 15f;
					float PosoNeg = Random.Range(0,2)*2-1;
					direction = direction * PosoNeg;
					Damage(3);
					target.ApplyKnockback( direction, 8f);
				}
				else
				{
					Vector2 force = diff * knockbackForce;
					target.Damage(3, force);
				}
			}
		}
	}
}
