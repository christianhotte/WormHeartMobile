using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipAnimator : MonoBehaviour
{
    //Description: Handles programmatic animation of DrillShip, called on by other scripts to do so
    //             Also contains state data on DrillShip

    //Objects & Components:
    public static ShipAnimator main;           //Singleton instance of this script in scene
    private Configuration baseConfig;          //Visible ship components which are moved by this script
    public ConfigAnimation[] configAnimations; //Array of all player ship animations

    //Settings:
    [Header("Settings:")]
    public float screwSpeedMultiplier;           //Determines mathematical relationship between ship speed (in units per second) and screw animation speed
    public float drillSpeedMultiplier;           //Determines mathematical relationship between ship speed (in units per second) and drill andimation speed
    [Range(0, 1)] public float screwAccelFactor; //Determines how fast screws accelerate to target speed (interpolant calculation in FixedUpdate, used 50 times per second)
    public float brakeScrewLockThresh;           //Speed threshold at which, while braking, all screws on drillship will lock in place

    //Memory & Status Variables:
    internal AnimMode mode = AnimMode.vertical; //What animation mode the drillship is currently in

    //Runtime Methods:
    private void Awake()
    {
        //Base Initialization:
        if (main == null) { main = this; } else Destroy(this); //Singleton-ize

        //Get Objects and Components:
        baseConfig = new Configuration(transform.Find("baseConfig")); //Initialize base configuration

        //Initiailize Animations:
        foreach (ConfigAnimation animation in configAnimations) animation.Initialize(transform); //Iterate through animations and initialize each one
        HideConfigGhosts();

    }
    private void Update()
    {
        //Run Animations:
        foreach (ConfigAnimation animation in configAnimations) //Iterate through list of all animations
        {
            //Initialization:
            if (!animation.playing) continue;   //Skip inactive animations
            animation.TimeStep(Time.deltaTime); //Increment animation time
            ComputeAnimation(animation);        //Compute and apply animation movement
        }

    }
    private void FixedUpdate()
    {
        //Animate Screws & Drill:
        switch (mode) //Determine screw behavior based on mode
        {
            case AnimMode.vertical:
                //Set Initial Animation Speed:
                SetScrewSpeed(-ShipController.main.vel.y * screwSpeedMultiplier, screwAccelFactor, 0b0000); //Apply ship speed to all screws (in the same direction)
                SetDrillSpeed(-ShipController.main.vel.y * drillSpeedMultiplier, screwAccelFactor);         //Apply ship speed to drill
                //Determine Additional Animation Behaviors:
                if (ShipController.main.locoStatus == LocomotionStatus.braking) //Drillship is currently braking
                {
                    SetScrewSpeed(0, 1, 0b0011); //Lock down rear screws as part of braking animation
                    if (ShipController.main.vel.y > -brakeScrewLockThresh) //Drillship is near end of braking phase
                    {
                        SetScrewSpeed(0, 1, 0b1100); //Lock down other two screws (preventing slow animation jank)
                        if (GetAnimationByName("ConfigAnim_Braking").InterpolantTime() != 0) SetBrakes(false);   //Stow brakes when almost halted
                    } else if (GetAnimationByName("ConfigAnim_Braking").InterpolantTime() == 0) SetBrakes(true); //Deploy brakes while braking
                } else if (GetAnimationByName("ConfigAnim_Braking").InterpolantTime() != 0) SetBrakes(false);    //Stow brakes when not braking
                break;
            case AnimMode.horizontal:
                //Set Initial Animation Speed:
                SetScrewSpeed(ShipController.main.vel.x * screwSpeedMultiplier, 1, 0b1001);  //Apply ship speed to screws (compensating for alternating directions)
                SetScrewSpeed(-ShipController.main.vel.x * screwSpeedMultiplier, 1, 0b0110); //Apply ship speed to screws (compensating for alternating directions)
                break;
            case AnimMode.transitioning:
                SetScrewSpeed(0, 1, 0b0000); //Lock all screws in stationary position
                SetDrillSpeed(0, 1);         //Lock drill in stationary position
                break;
        }
    }

    //Configuration Methods:
    private void LerpConfig(Configuration originConfig, Configuration targetConfig, float[] interpolants, int mask)
    {
        //Function: Lerps elements in base configuration between the two given positions based on the given interpolants (ignoring elements which are masked out)
        //NOTE: Interpolant values correspond to position, rotation and scale respectively.  Method will throw exception if this array does not contain three elements
        //NOTE: Mask bits work in order that config array is arranged in, with right-to-left coordinating to index 0-to-Length
        //NOTE: Interpolation process is raw.  All refinements to interpolant value should be made outside this method
        //NOTE: If an interpolant value is passed as less than -100, it is used as a mask and the lerp for that value is ignored

        //Initialization:
        if (interpolants.Length != 3) { Debug.LogError("Bad interpolant array passed to LerpConfig in ShipAnimator"); return; } //Ensure interpolant array is correct length

        //Move Elements:
        for (int i = 0; i < baseConfig.elements.Length; i++) //Iterate through all elements in rig
        {
            //Initialization:
            if ((mask >> i) % 2 != 0) continue; //Skip if specific element is masked out
            Transform baseElement = baseConfig.elements[i];   //Get transform of corresponding element in base configuration
            Transform origElement = originConfig.elements[i]; //Get transform of corresponding element in origin configuration
            Transform targElement = targetConfig.elements[i]; //Get transform of corresponding element in target configuration

            //Get Interpolated Values:
            Vector3 newPos = Vector3.LerpUnclamped(origElement.position, targElement.position, interpolants[0]);       //Interpolate position between origin and target configs
            Quaternion newRot = Quaternion.LerpUnclamped(origElement.rotation, targElement.rotation, interpolants[1]); //Interpolate rotation between origin and target configs
            Vector3 newScale = Vector3.LerpUnclamped(origElement.localScale, targElement.localScale, interpolants[2]); //Interpolate scale between origin and target configs

            //Apply New Values:
            if (interpolants[0] > -100) baseElement.position = newPos;     //Apply new position (unless value is masked out)
            if (interpolants[1] > -100) baseElement.rotation = newRot;     //Apply new rotation (unless value is masked out)
            if (interpolants[2] > -100) baseElement.localScale = newScale; //Apply new scale (unless value is masked out)
        }
    }
    private void LerpConfig(Configuration originConfig, Configuration targetConfig, float interpolant, int mask)
    {
        //Overflow: Applies a single interpolant uniformly to all aspects of element transform

        LerpConfig(originConfig, targetConfig, new float[3]{ interpolant, interpolant, interpolant }, mask); //Call base method
    }
    private void LerpConfig(Configuration originConfig, Configuration targetConfig, float[] interpolants)
    {
        //Overflow: Assumes all elements in configuration are being used (no mask)

        LerpConfig(originConfig, targetConfig, interpolants, 0); //Call base method
    }
    private void LerpConfig(Configuration originConfig, Configuration targetConfig, float interpolant)
    {
        //Overflow: -Applies a single interpolant uniformly to all aspects of element transform
        //          -Assumes all elements in configuration are being used (no mask)

        LerpConfig(originConfig, targetConfig, new float[3] { interpolant, interpolant, interpolant }); //Call base method
    }
    private void SetConfig(Configuration targetConfig, byte mask)
    {
        //Function: Directly sets base config transform to target (still includes mask if needed)

        LerpConfig(baseConfig, targetConfig, 1, mask); //Call base method
    }
    private void SetConfig(Configuration targetConfig)
    {
        //Overflow: Assumes all elements in configuration are being used (no mask)

        LerpConfig(baseConfig, targetConfig, 1); //Call base method
    }

    //Animation Methods:
    public void ToggleMode()
    {
        //Function: Toggles between horizontal and vertical ship modes

        //Begin Mode Transition Animation:
        switch (mode) //Determine behavior based on current mode
        {
            case AnimMode.vertical:
                GetAnimationByName("ConfigAnim_ModeTransition").speedMultiplier = 1; //Set animation to play forward
                GetAnimationByName("ConfigAnim_ModeTransition").playing = true;      //Play animation
                DigVisualizer.main.BuildBranch(); //Generate a new branch
                break;
            case AnimMode.horizontal:
                GetAnimationByName("ConfigAnim_ModeTransition").speedMultiplier = -1; //Set animation to play backward
                GetAnimationByName("ConfigAnim_ModeTransition").playing = true;       //Play animation
                DigVisualizer.main.EndBranch(); //End current branch
                break;
            case AnimMode.transitioning:
                GetAnimationByName("ConfigAnim_ModeTransition").speedMultiplier *= -1; //Reverse direction of animation
                if (GetAnimationByName("ConfigAnim_ModeTransition").speedMultiplier > 0) //Animation is now playing forward
                { DigVisualizer.main.BuildBranch(); } //Generate a new branch
                else //Animation is now playing backward
                { DigVisualizer.main.EndBranch(); } //Cancel in-progress branch if applicable
                break;
        }

        //Cleanup:
        mode =  AnimMode.transitioning; //Indicate that ship is now transitioning
    }
    public void ToggleMode(AnimMode targetMode)
    {
        //Overflow: Toggles to specific mode

        //Initialization:
        if (targetMode == mode) return; //Skip if ship is already in target mode
        ToggleMode(); //Call base function
    }
    private void SetBrakes(bool on)
    {
        //Function: Plays brake animation (either forward or backward depending on parameter)

        //Initialization:
        ConfigAnimation anim = GetAnimationByName("ConfigAnim_Braking"); //Get reference for braking animation

        //Play Animation:
        if (on) anim.speedMultiplier = 1; //Play braking animation forward
        else anim.speedMultiplier = -1;   //Play braking animation in reverse

        //Cleanup:
        anim.playing = true;
    }
    private void ComputeAnimation(ConfigAnimation animation)
    {
        //Function: Processes given animation and computes movement accordingly

        //Initialization:
        MaskedCurve[] activeCurves = animation.GetActiveCurves(); //Get currently-active curves from animation

        //Move Elements:
        foreach (MaskedCurve curve in activeCurves) //Iterate through list of active curves on animation
        {
            //Get Lerp Elements:
            int mask = curve.GetMask(); //Get element mask from curve
            float interpolant = curve.curve.Evaluate(animation.RealInterpolantTime()); //Evaluate curve based on animation time (between current target and origin)

            //Check for Masked Transforms:
            if (!curve.includePos || !curve.includeRot || !curve.includeScl) //Curve has at least one interpolant masked out
            {
                //Get New Interpolant Set:
                float[] interpolants = new float[3] { -100, -100, -100 };  //Initialize interpolant set as array of three masked-out values
                if (curve.includePos) interpolants[0] = interpolant; //Unmask position if selected
                if (curve.includeRot) interpolants[1] = interpolant; //Unmask rotation if selected
                if (curve.includeScl) interpolants[2] = interpolant; //Unmask scale if selected

                //Apply Movement:
                LerpConfig(animation.GetCurrentOrigin(), animation.GetCurrentTarget(), interpolants, mask); //Lerp base config
                continue; //Prevent additional application of movement
            }

            //Apply Movement:
            if (animation.GetCurrentOrigin() == animation.GetCurrentTarget()) Debug.Log("poog");
            LerpConfig(animation.GetCurrentOrigin(), animation.GetCurrentTarget(), interpolant, mask); //Lerp base config
        }
    }
    public ConfigAnimation GetAnimationByName(string animName)
    {
        //Function: Returns animation of given name (if one exists)

        //Look For Animation:
        foreach (ConfigAnimation animation in configAnimations) //Iterate through list of animations
        {
            if (animation.name == animName) return animation; //Return corresponding animation if found
        }

        //Animation Not Found:
        Debug.LogError("Animation by name " + animName + " not found");
        return null; //Return empty
    }
    public void OnAnimationEnd(ConfigAnimation animation)
    {
        //Function: Called when an animation ends (either hits end or hits beginning)

        //Check For Mode Transition:
        if (animation.name == "ConfigAnim_ModeTransition") //Mode transition animation has just ended
        {
            if (animation.currentTime <= 0) mode = AnimMode.vertical; //Ship is now horizontal
            else mode = AnimMode.horizontal;                          //Ship is now vertical
        }
    }

    //Animator Methods:
    private void SetScrewSpeed(float targetSpeed, float interpolant, int mask)
    {
        //Function: Sets the animation speed for the screws in the base configuration (ignoring those which are masked out)
        //NOTE: Mask bits work in same order as screws are organized in hierarchy (with screw_FL being represented by the first bit)
        //NOTE: Interpolant value is used to move speed toward target, make 1 to hard set the speed
        //NOTE: DeltaTime should be applied to the interpolant externally (if necessary)

        for (int i = 2; i < 6; i++) //Iterate through all four screw elements in rig
        {
            //Initialization:
            if ((mask >> i - 2) % 2 != 0) continue; //Skip if specific tread is masked out
            Animator screwAnimator = baseConfig.elements[i].GetComponentInChildren<Animator>(); //Get animator from current screw

            //Set New Speed:
            float newSpeed = Mathf.Lerp(screwAnimator.GetFloat("speed"), targetSpeed, interpolant); //Get new speed to set (based on given parameters)
            screwAnimator.SetFloat("speed", newSpeed); //Set new speed
        }
    }
    private void SetDrillSpeed(float targetSpeed, float interpolant)
    {
        //Function: Sets the animation speed of the drill in the base configuration

        Animator drillAnimator = baseConfig.elements[0].GetComponentInChildren<Animator>(); //Get animator from drill
        float newSpeed = Mathf.Lerp(drillAnimator.GetFloat("speed"), targetSpeed, interpolant); //Get new speed to set (based on given parameters)
        drillAnimator.SetFloat("speed", newSpeed); //Set new speed
    }

    //Utility Methods:
    private void HideConfigGhosts()
    {
        //Function: Hides unnecessary sprites on reference configs during runtime
        //NOTE: This uses the "Config" name component to identify configs, ensure all Config containers are named correctly (configs should also be children of main drillship object)

        for (int i = 0; i < transform.childCount; i++) //Iterate through each child in DrillShip object
        {
            //Find Non-Base Configs:
            Transform child = transform.GetChild(i); //Get child in current index
            if (child.name.Contains("Config") && !child.name.Contains("base")) //Child is a config but not the base config
            {
                //Hide Sprites:
                for (int n = 0; n < child.childCount; n++) //Iterate through each element in DrillShip
                {
                    child.GetChild(n).GetComponentInChildren<SpriteRenderer>().enabled = false; //Disable renderer on each element
                }
            }
        }
    }
}
