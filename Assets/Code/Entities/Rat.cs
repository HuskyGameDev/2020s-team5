﻿//
// When We Fell
//

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class Rat : Entity
{

	public float jumpVelocity;
	public float gravity;
	public bool aggro;
	public bool movingRight, movingLeft;
	[SerializeField]
	public GameObject player;
	public float count = 0;
	public Vector2 stay;
	public Vector2Int tilePos;
	public bool facing;
    int i = 0;

    private void Start()
	{
		player = GameObject.Find("Player");
    }

    private void Update()
	{
		float PlayerY = player.transform.position.y;
		float PlayerX = player.transform.position.x;

		if(Math.Abs(PlayerX - transform.position.x) <= 10 && Math.Abs(PlayerY - transform.position.y) < 3.2)
		{
            aggro = true;
		}

		if(Math.Abs(PlayerX - transform.position.x) >= 15)
		{
            aggro = false;
		}

		Vector2 accel = Vector2.zero;

		if(PlayerX < transform.position.x && aggro)
		{
            if (Math.Abs(PlayerX - transform.position.x) >= .9)
			{
                accel = Vector2.left;
				facing = true;
            }

			SetFacingDirection(true);
		} else if (aggro)
		{
            if (Math.Abs(PlayerX - transform.position.x) >= .9)
			{
				accel = Vector2.right;
				facing = false;
			}

			SetFacingDirection(false);
		}
		
		tilePos = Utils.TilePos(Position);

		if(TileManager.GetData(world.GetTile(tilePos.x+1, tilePos.y-1)).passable && CollidedBelow() && aggro && !facing) {
			velocity.y = jumpVelocity;
		} else if(TileManager.GetData(world.GetTile(tilePos.x-1, tilePos.y-1)).passable && CollidedBelow() && aggro && facing) {
			velocity.y = jumpVelocity;
		}
		
		if((PlayerY - transform.position.y) > 1.99) {
			Move(world, -accel, gravity);
			StartCoroutine(wait());
			Move(world, accel, gravity);
		} else {
			Move(world, accel, gravity);
		}
        if (aggro && i==0)
        {
            i++;
            FindObjectOfType<Audiomanager>().Play("Rat Cry");
        }
    }

	protected override void HandleOverlaps(List<CollideResult> overlaps)
	{
		for (int i = 0; i < overlaps.Count; ++i)
		{
			CollideResult result = overlaps[i];
			Entity target = result.entity;

			if (target != null && target is Player)
			{
                Vector2 diff = (target.Position - Position).normalized;

				if (diff.y > 0.4f)
				{
					Damage(5);
					target.ApplyKnockback(0.0f, 7.5f);
				}
				else
				{
					Vector2 force = diff * knockbackForce;
					target.Damage(3, force);
				}
			}
		}
	}
	IEnumerator wait() {
		yield return new WaitForSeconds(5.0f);
	}
}
