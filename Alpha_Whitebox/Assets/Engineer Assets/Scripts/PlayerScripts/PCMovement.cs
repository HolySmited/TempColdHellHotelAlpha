﻿using UnityEngine;
using System.Collections;

public class PCMovement : MonoBehaviour
{
    /*  Script: PlayerController.cs
        Author: Zackary Hoyt
        Date:   2016 January 12

        Description:
        Allows for player control, including movement,
        camera rotation, and use of abilities.

        Allows the player to pass through walls, but
        not the level's border.
    */

    //  Cached Component References
    Rigidbody rbody;

    void Start() { rbody = GetComponent<Rigidbody>(); }

    //  Movement
    Vector3 rightward = new Vector3(), upward = new Vector3(), forward = new Vector3();
    float rightwardSpeed = 0, upwardSpeed = 0, forwardSpeed = 0;
    
    public void moveForwards(int dir)
    {
        //  Deceleration
        if (dir == 0)
        {
            if (forward.magnitude <= PCSettings.pcSettings.accel * Time.deltaTime)
            {
                forward = new Vector3();
                forwardSpeed = 0;

                //  Update Velocity
                setVelocity();
                //  return since stopped
                return;
            }
            dir = -(int)(Mathf.Round((Mathf.Abs(forwardSpeed) / forwardSpeed)));    //  Determine Residual Velocity's Direction
        }
        //  Change of Direction
        else if (PCSettings.pcSettings.instantDirChange && dir == -(int)(Mathf.Round((Mathf.Abs(forwardSpeed) / forwardSpeed))))
            forwardSpeed = 0;   //  Instantly stop (not decelerate) to proceed in new direction without slowing down

        //  Accelerate to a Max Speed
        forwardSpeed += dir * PCSettings.pcSettings.accel * Time.deltaTime;
        if (Mathf.Abs(forwardSpeed) > PCSettings.pcSettings.speed)
            forwardSpeed = dir * PCSettings.pcSettings.speed;

        //  Lock Velocity to XZ-Plane if Altitude-Soft-Locked
        if (PCSettings.pcSettings.walkMode)
            forward = forwardSpeed * Vector3.Normalize(new Vector3(transform.forward.x, 0, transform.forward.z));
        else
            forward = forwardSpeed * new Vector3(transform.forward.x, transform.forward.y, transform.forward.z);

        //  Update Velocity
        setVelocity();
    }
    public void moveRightwards(int dir)
    {
        //  Deceleration
        if (dir == 0)
        {
            if (rightward.magnitude <= PCSettings.pcSettings.accel * Time.deltaTime)
            {
                rightward = new Vector3();
                rightwardSpeed = 0;

                //  Update Velocity
                setVelocity();
                //  return since stopped
                return;
            }
            dir = -(int)(Mathf.Round((Mathf.Abs(rightwardSpeed) / rightwardSpeed)));    //  Determine Residual Velocity's Direction
        }
        else if (PCSettings.pcSettings.instantDirChange && dir == -(int)(Mathf.Round((Mathf.Abs(rightwardSpeed) / rightwardSpeed))))
            rightwardSpeed = 0;   //  Instantly stop (not decelerate) to proceed in new direction without slowing down

        //  Accelerate to a Max Speed
        rightwardSpeed += dir * PCSettings.pcSettings.accel * Time.deltaTime;
        if (Mathf.Abs(rightwardSpeed) > PCSettings.pcSettings.speed)
            rightwardSpeed = dir * PCSettings.pcSettings.speed;
        
        //  Strafing is Always Locked to the Current Altitude
        rightward = rightwardSpeed * new Vector3(transform.right.x, 0, transform.right.z);

        setVelocity();
    }
    public void moveUpwards(int dir)
    {
        if (dir == 0)
        {
            if (PCSettings.pcSettings.walkMode)
                return;
            //  Decelerate to a Halt
            if (upward.magnitude <= PCSettings.pcSettings.accel * Time.deltaTime)
            {
                upward = new Vector3();
                upwardSpeed = 0;

                //  Update Velocity
                setVelocity();
                //  return since stopped
                return;
            }
            dir = -(int)(Mathf.Round((Mathf.Abs(upwardSpeed) / upwardSpeed)));  //  Determine Residual Velocity's Direction
        }
        else {
            PCSettings.pcSettings.walkMode = false;  //  Cancel Altitude Soft-Lock if Manually Vertically Moe

            if (PCSettings.pcSettings.instantDirChange && dir == -(int)(Mathf.Round((Mathf.Abs(upwardSpeed) / upwardSpeed))))
                upwardSpeed = 0;   //  Instantly stop (not decelerate) to proceed in new direction without slowing down
        }

        //  Accelerate to a Max Speed
        upwardSpeed += dir * PCSettings.pcSettings.accel * Time.deltaTime;
        if (Mathf.Abs(upwardSpeed) > PCSettings.pcSettings.speed)
            upwardSpeed = dir * PCSettings.pcSettings.speed;

        //  Rising is Only Ever Vertical
        upward = upwardSpeed * new Vector3(0, 1, 0);

        //  Update Velocity
        setVelocity();
    }

    void setVelocity()
    {
        float fallSpeed = 0;
        if (PCSettings.pcSettings.walkMode)
        {
            fallSpeed = rbody.velocity.y;
            upwardSpeed = 0;
            upward = new Vector3();
        }

        //  Cap the Velocity's Magnitude
        Vector3 velocity = (forward + rightward + upward);
        if (velocity.magnitude > PCSettings.pcSettings.speed)
            velocity = PCSettings.pcSettings.speed * Vector3.Normalize(velocity);

        //  Don't Normalize Falling Speed
        if (fallSpeed != 0)
            velocity = new Vector3(velocity.x, velocity.y + fallSpeed, velocity.z);

        //  Apply New Velocity
        rbody.velocity = velocity;
    }

    //  Stop Moving
    public void stop()
    {
        //  Force the Player to Stop all Movements
        rbody.velocity = rightward = upward = forward = new Vector3();
        rightwardSpeed = upwardSpeed = forwardSpeed = 0;
    }
}
