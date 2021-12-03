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
    public float brakeSnapThresh;    //Lower threshhold at which, when decelerating, drillship will come to a complete stop
    [Space()]
    public float shaftAlignTolerance; //How close center of drillship must be to the shaft for it to enter from a branch (will lerp into proper position from there)

    //Memory & Status Vars:
    internal LocomotionStatus locoStatus; //Drillship's current locomotion behavior (mutually exclusive states based on most recent input)
    internal Vector2 vel = new Vector2(); //Drillship's current velocity along both axes
    private bool waitingToDeploy;         //Indicates that a switch mode command has been called but drillship has not yet come to a halt

    //Temp Debug Stuff:
    [Space()]
    public bool debugAccelLeft;
    public bool debugAccelRight;
    public bool debugBrake;
    public bool debugDeploy;
    public bool useMobileDebug;


    //Runtime Methods:
    private void Awake()
    {
        //Initialization:
        if (main == null) { main = this; } else { Destroy(this); }
    }
    private void Update()
    {
        //Temp Debug Stuff:
        if (useMobileDebug)
        {
            DeviceOrientation DO = Input.deviceOrientation;
            if (ShipAnimator.main.mode != AnimMode.transitioning && CameraManipulator.main.mode != AnimMode.transitioning)
            {
                if (DO == DeviceOrientation.Portrait) Deploy(true);
                if (DO == DeviceOrientation.LandscapeLeft || DO == DeviceOrientation.LandscapeRight) Deploy(false);
            }
            if (Input.touchCount > 1) { Accel(true); } else if (Input.touchCount > 0) { Accel(false); } else if (locoStatus != LocomotionStatus.braking) { Brake(); }
        }
        else
        {
            if (debugAccelLeft)
            {
                Accel(true);
            }
            else if (debugAccelRight)
            {
                Accel(false);
            }
            else
            {
                ReleaseAccel();
            }

            if (debugBrake)
            {
                debugBrake = false;
                debugAccelLeft = false;
                debugAccelRight = false;
                Brake();
            }
            if (debugDeploy)
            {
                debugAccelLeft = false;
                debugAccelRight = false;
                debugDeploy = false;
                ToggleDeploy();
            }
        }

        //Move DrillShip:
        if (ShipAnimator.main.mode == AnimMode.vertical && vel.y < 0) //Drillship is currently moving downward through shaft
        {
            DigVisualizer.main.digSpace.Translate(0, -vel.y * Time.deltaTime, 0); //Move digspace past drill at proper speed
        }
        else if (ShipAnimator.main.mode == AnimMode.horizontal && vel.x != 0) //Drillship is currently moving horizontally through branch
        {
            transform.Translate(vel.x * Time.deltaTime, 0, 0); //Move drillship along branch horizontally
        }
    }
    private void FixedUpdate()
    {
        //Apply Brakes:
        if (locoStatus == LocomotionStatus.braking) Brake(); //Keep braking if brakes have been triggered (braking is cancelled by acceleration)

        //Check Deploy Flag:
        if (waitingToDeploy) //Ship is currently waiting to deploy
        {
            if (vel == Vector2.zero) //Ship has come to a halt
            {
                waitingToDeploy = false; //Indicate that ship has finished waiting
                ToggleDeploy();          //Toggle deployment state (using unchecked function because target deployment state is unknown)
            }
        }
    }

    //Input Methods:
    public void Accel(bool left)
    {
        //Function: Contextual acceleration method, increases speed at which drill is digging, direction depends on current shipmode
        //NOTE: Directional parameter is ignored if ship is in vertical mode, in this case it can be set to whatever

        //Initialization:
        AnimMode mode = CameraManipulator.main.mode; //Get mode from CameraManipulator (because this is always the first to be updated)
        if (mode == AnimMode.transitioning) return;                   //Ignore if camera is currently transitioning
        if (ShipAnimator.main.mode == AnimMode.transitioning) return; //Ignore if ship is currently transitioning

        //Add Velocity (depending on mode):
        if (mode == AnimMode.vertical) //Ship is digging vertically
        {
            vel.y -= accelVertical * Time.deltaTime;     //Apply acceleration to velocity (factoring in deltaTime)
            vel.y = Mathf.Max(vel.y, -maxSpeedVertical); //Clamp velocity based on max speed
        }
        else //Ship is digging horizontally (transitioning mode has already been filtered out)
        {
            float dir = 1; if (left) { dir *= -1; }                              //Get multiplier for direction depending on directional parameter
            vel.x += accelHorizontal * dir * Time.deltaTime;                     //Apply acceleration to velocity (factoring in direction and deltaTime)
            vel.x = Mathf.Clamp(vel.x, -maxSpeedHorizontal, maxSpeedHorizontal); //Clamp velocity based on max speed
        }

        //Update Statuses:
        if (vel.y == -maxSpeedVertical || Mathf.Abs(vel.x) == maxSpeedHorizontal) locoStatus = LocomotionStatus.atSpeed; //Indicate that drillship is at top speed
        else locoStatus = LocomotionStatus.accelerating; //Indicate that drillship is currently accelerating
        waitingToDeploy = false; //Get rid of scheduled deployment since player wants to keep accelerating
    }
    public void Accel() { Accel(true); } //Overflow to get rid of that pesky unnecessary parementer when digging vertically
    public void ReleaseAccel()
    {
        //Function: Weird method for ensuring locomotion statemachine stays accurate, called when player takes their finger off the accelerator button

        //Initialization:
        if (locoStatus != LocomotionStatus.accelerating) return; //Disallow method from overriding any locomotion state other than "accelerating"

        //Override Locomotion State:
        locoStatus = LocomotionStatus.neutral; //Indicate that locomotion input is no longer being given
    }
    public void Brake()
    {
        //Function: Slows drillship down based on current direction of travel and corresponding deceleration factor

        //Initialization:
        if (vel == Vector2.zero) //Drillship is currently halted
        {
            locoStatus = LocomotionStatus.neutral; //Indicate that drillship is neither accelerating nor braking
            return; //Ignore deceleration computation
        }
        locoStatus = LocomotionStatus.braking; //Indicate that drillship is slowing down

        //Decrease Velocity:
        vel.y = Mathf.Lerp(vel.y, 0, brakeIntensityVert);  //Decrease vertical velocity based on corresponding brake intensity
        vel.x = Mathf.Lerp(vel.x, 0, brakeIntensityHoriz); //Decrease horizontal velocity based on corresponding brake intensity

        //Check For Brake Snap:
        if (Mathf.Abs(vel.y) < brakeSnapThresh) vel.y = 0; //Snap vertical velocity to zero if it comes close enough
        if (Mathf.Abs(vel.x) < brakeSnapThresh) vel.x = 0; //Snap horizontal velocity to zero if it comes close enough
    }
    public void Deploy(bool vertical)
    {
        //Function: Triggers chain of events which either deploys or stows drillship (or begins braking sequence if ship is not still)

        //Initialization:
        AnimMode mode = CameraManipulator.main.mode;  //Find out what current mode is (from CameraManipulator)
        if (vertical && mode == AnimMode.vertical ||  //Redundant call to deply vertical mode
            !vertical && mode == AnimMode.horizontal) //Redundant call to deploy horizontal mode
        {
            waitingToDeploy = false; //Ship does not need to wait to deploy for redundant mode
            return; //Ignore mode transition call
        }
        if (vertical && Mathf.Abs(transform.position.x) > shaftAlignTolerance) return; //Do not allow player to leave a branch if they are not close enough to the shaft

        //Check Velocity:
        if (vel != Vector2.zero) //Ship is not currently stationary
        {
            waitingToDeploy = true; //Indicate that ship is halting and will switch modes once halted
            locoStatus = LocomotionStatus.braking; //Begin slowing ship down
            return; //Do not deploy yet
        }

        //Check Branch Validity:
        if (!vertical && !DigVisualizer.main.CheckBranchValidity()) return; //If player is trying to dig a new branch in an invalid location, cancel deployment

        //Toggle Mode:
        locoStatus = LocomotionStatus.neutral; //Ensure locomotion status is locked to neutral for the duration of deployment (probably redundant)
        CameraManipulator.main.ToggleMode();   //Begin mode toggle sequence (handled initially by CameraManipulator which then hands it off to ShipAnimator (with help from DigVisualizer))
    }
    public void ToggleDeploy()
    {
        //Function: Figures out what mode ship is currently in and deploys to opposite mode
        //NOTE: Intended to be used for scheduled deployments and debug input
        //NOTE: Use caution, can cause redundant deployment and mess up script relationships

        switch (CameraManipulator.main.mode) //Determine how to call deployment depending on current mode
        {
            case AnimMode.vertical: //Ship is in vertical mode
                Deploy(false); //Deploy to horizontal mode
                break;
            case AnimMode.horizontal: //Ship is in horizontal mode
                Deploy(true); //Deploy to vertical mode
                break;
            case AnimMode.transitioning: //Ship is transitioning between modes
                CameraManipulator.main.ToggleMode(); //Directly call mode toggle on camera, ignoring deployment checks (because ship should already be stationary and other checks are irrelevant)
                break;
        }
    }
    public void AutoFindShaft()
    {
        //Function: Used to return drillship to shaft automatically while in a branch


    }
}
