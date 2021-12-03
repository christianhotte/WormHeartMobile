using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    //Description: Receives method calls from input scripts and triggers drillship functions/behavior

    //Objects & Components:
    public static ShipController main; //Singleton instance of this script in scene

    //Settings:
    [Header("Settings:")]
    public float maxSpeedVertical;   //Maximum speed drillship can travel vertically (in units per second)
    public float maxSpeedHorizontal; //Maximum speed drillship can travel horizontally (in units per second)
    public float accelVertical;      //Vertical acceleration factor (in units per second per second)
    public float accelHorizontal;    //Horizontal acceleration factor (in units per second per second)
    [Range(0, 1)] public float brakeIntensityVert;  //Vertical deceleration factor (how fast drillship slows down)
    [Range(0, 1)] public float brakeIntensityHoriz; //Horizontal deceleration factor (how fast drillship slows down)
    [Space()]
    public float brakeSnapThresh;    //Lower threshhold at which, when decelerating, drillship will come to a complete stop

    //Memory & Status Vars:
    internal Vector2 vel = new Vector2(); //Drillship's current velocity along both axes

    //Temp Debug Stuff:
    public bool debugAccelerate;
    public bool debugStartBranch;


    //Runtime Methods:
    private void Awake()
    {
        //Initialization:
        if (main == null) { main = this; } else { Destroy(this); }
    }
    private void Update()
    {
        //Temp Debug Stuff:
        if (debugAccelerate) { DigDeeper(); DigLeft(true); }
        else Brake();
        if (debugStartBranch)
        {
            debugStartBranch = false;
        }

        //Move DrillShip:
        if (ShipAnimator.main.mode == ShipAnim.vertical && vel.y < 0) //Drillship is currently moving downward through shaft
        {
            DigVisualizer.main.digSpace.Translate(0, -vel.y * Time.deltaTime, 0); //Move digspace past drill at proper speed
        }
        else if (ShipAnimator.main.mode == ShipAnim.horizontal && vel.x != 0) //Drillship is currently moving horizontally through branch
        {
            transform.Translate(vel.x * Time.deltaTime, 0, 0); //Move drillship along branch horizontally
        }
    }

    //Input Methods:
    public void DigDeeper()
    {
        //Function: Accelerates drillship downward while in main shaft

        //Initialization:
        if (ShipAnimator.main.mode != ShipAnim.vertical) return; //Ignore if ship is not in vertical mode

        //Add Velocity:
        vel.y -= accelVertical * Time.deltaTime;     //Apply acceleration to velocity (factoring in deltaTime)
        vel.y = Mathf.Max(vel.y, -maxSpeedVertical); //Clamp velocity based on max speed
    }
    public void DigLeft(bool digRight)
    {
        //Function: Accelerates drillship either left or right while in a branch

        //Initialization:
        if (ShipAnimator.main.mode != ShipAnim.horizontal) return; //Ignore if ship is not in horizontal mode
        float dir = -1; if (digRight) { dir *= -1; }               //Get multiplier for direction depending on parameter

        //Add Velocity:
        vel.x += accelHorizontal * dir * Time.deltaTime; //Apply acceleration to velocity (factoring in direction and deltaTime)
        vel.x = Mathf.Clamp(vel.x, -maxSpeedHorizontal, maxSpeedHorizontal); //Clamp velocity based on max speed
    }
    public void Brake()
    {
        //Function: Slows drillship down based on current direction of travel and corresponding deceleration factor

        //Initialization:
        if (vel == Vector2.zero) return; //Ignore if drillShip is already halted

        //Decrease Velocity:
        vel.y = Mathf.Lerp(vel.y, 0, brakeIntensityVert);  //Decrease vertical velocity based on corresponding brake intensity
        vel.x = Mathf.Lerp(vel.x, 0, brakeIntensityHoriz); //Decrease horizontal velocity based on corresponding brake intensity

        //Check For Brake Snap:
        if (Mathf.Abs(vel.y) < brakeSnapThresh) vel.y = 0; //Snap vertical velocity to zero if it comes close enough
        if (Mathf.Abs(vel.x) < brakeSnapThresh) vel.x = 0; //Snap horizontal velocity to zero if it comes close enough
    }
}
