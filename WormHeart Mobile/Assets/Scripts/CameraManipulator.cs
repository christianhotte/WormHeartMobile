using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManipulator : MonoBehaviour
{
    //Description: Moves the camera based on input and game state

    //Classes, Enums & Structs:


    //Objects & Components:
    public static CameraManipulator main; //Single instance of this script in scene
    private Camera cam;                   //Reference to main camera object

    //Settings:
    [Header("Mode Transition:")]
    public AnimationCurve modeSwapCurve; //Curve describing the transition between horizontal mode camera and vertical mode camera
    public float modeSwapTime;           //Time (in seconds) camera takes to swap modes
    public float horizCamSize;           //Size camera lerps to when in horizontal position

    //Status & Memory Vars:
    internal AnimMode mode = AnimMode.vertical; //Animation mode camera is currently in
    private float currentTime;                  //Current progression (in seconds) through mode swap animation
    private float speedMultiplier = 1;          //Determines which direction camera transition animation is going in
    private float vertCamSize;                  //Size camera initially starts at
    private bool prevHorizOrientationLeft;      //Stores the direction of last landscape orientation (so that either version of landscape can be used)

    //Runtime Methods:
    private void Awake()
    {
        //Initialization:
        if (main == null) { main = this; } else { Destroy(this); }

        //Get Objects & Components:
        cam = Camera.main; //Get reference to camera (not super necessary but eckgh)

        //Save Initial Settings:
        vertCamSize = cam.orthographicSize;
    }
    private void Update()
    {
        //Try Get Horizontal Orientation:
        if (mode == AnimMode.vertical) //Only check this while camera is in vertical position (to prevent jank)
        {
            DeviceOrientation currentOrientation = Input.deviceOrientation; //Get current device orientation
            if (currentOrientation == DeviceOrientation.LandscapeLeft) prevHorizOrientationLeft = true; //Record that most recent horizontal orientation was left
            else if (currentOrientation == DeviceOrientation.LandscapeRight) prevHorizOrientationLeft = false; //Record that most recent horizontal orientation was right
        }

        //Update Camera Animation:
        if (mode == AnimMode.transitioning) //Animate camera while transitioning
        {
            //Compute Current Animation Time:
            currentTime += Time.deltaTime * speedMultiplier; //Apply timeStep to time tracker
            if (currentTime > modeSwapTime) //Camera mode swap animation has ended
            {
                currentTime = modeSwapTime; //Cap time to total animation length
                mode = AnimMode.horizontal; //Indicate that camera is now in horizontal mode

                //Triggers:
                ShipAnimator.main.ToggleMode(AnimMode.horizontal); //Trigger mode switch on ship
            }
            else if (currentTime < 0) //Camera mode swap animation has ended (in reverse)
            {
                currentTime = 0; //Cap time to zero
                mode = AnimMode.vertical; //Indicate that camera is now in vertical mode

                //Triggers:
                ShipAnimator.main.ToggleMode(AnimMode.vertical); //Trigger mode switch on ship
            }
            float t = currentTime / modeSwapTime; //Get interpolant value for current time (0-1 range)

            //Move Camera:
            float o = 90; if (!prevHorizOrientationLeft) { o *= -1; } //Create variable to align horizontal orientation with last landscape direction
            cam.transform.eulerAngles = new Vector3(0, 0, Mathf.LerpUnclamped(0, o, modeSwapCurve.Evaluate(t))); //Lerp camera rotation based on curve and current animation time
            cam.orthographicSize = Mathf.LerpUnclamped(vertCamSize, horizCamSize, modeSwapCurve.Evaluate(t));    //Lerp camera size based on curve and current animation time
        }
    }

    //Functionality Methods:
    public void ToggleMode()
    {
        //Function: Switches camera mode between horizontal and vertical

        //Determine Animation Behavior:
        switch (mode) //New behavior is based on current mode
        {
            case AnimMode.vertical:
                speedMultiplier = 1; //Play animation forward
                break;
            case AnimMode.horizontal:
                speedMultiplier = -1; //Play animation backward
                break;
            case AnimMode.transitioning:
                speedMultiplier *= -1; //Reverse direction of animation
                break;
        }

        //Cleanup:
        mode = AnimMode.transitioning; //Begin mode transition
    }
    public void ToggleMode(AnimMode targetMode)
    {
        //Overflow: Toggles to specific mode

        //Initialization:
        if (targetMode == mode) return; //Skip if ship is already in target mode
        ToggleMode(); //Call base function
    }
}
