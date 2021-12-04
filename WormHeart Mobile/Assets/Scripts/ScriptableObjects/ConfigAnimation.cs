using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/ConfigAnimation", order = 1)]
public class ConfigAnimation : ScriptableObject
{
    //Description: Contains data which describes an animation between two configurations
    //NOTE: Multiple curves can be used at a time, just be careful about using element masks to prevent them from overlapping (proto feature)
    //NOTE: Animation may use multiple target configs (for more complex movements with multiple keyframes), just ensure they are arranged chronologically

    //Classes, Enums & Structs:
    [System.Serializable] public class TargetData
    {
        //Description: Data required to designate a target configuration in the animator

        public string name; //Name of the requested config's container
        [Range(0, 1)] public float startTime; //Time at which this config becomes the current target (for animations with three or more configs)
    }

    //Objects & Components:
    internal Configuration originConfig;    //Origin configuration for animation
    internal Configuration[] targetConfigs; //Target configuration(s) for animation

    //Settings:
    public string origin;        //User-set configuration origin
    public TargetData[] targets; //User-set configuration target(s)
    
    public float time;           //Total animation length (in seconds)
    public float startingTime;   //Time (in seconds) where animation starts by default
    public bool playBackwards;   //Sets initial speedMultiplier to -1
    public MaskedCurve[] curves; //Curves describing motion of the animation

    //Memory & Status Vars:
    internal float currentTime = 0;       //Current time marker in animation (in seconds)
    internal float speedMultiplier = 1;   //Modifies speed at which animation plays
    internal bool playing = false;        //True if animation is currently being played

    //Query Methods:
    public MaskedCurve[] GetActiveCurves()
    {
        //Function: Returns animationCurves which are currently active (along with data required to specifically respond to each one)

        //Initialization:
        List<MaskedCurve> validCurves = new List<MaskedCurve>(); //Initialize list of valid data to return

        //Find Curves:
        foreach (MaskedCurve curve in curves) //Iterate through list of this animation's masked curves
        {
            //Initialization:
            Keyframe[] keys = curve.curve.keys; //Get array of all keys in curve
            float iTime = InterpolantTime();    //Get current time as interpolant

            //Add | Skip:
            if (iTime < keys[0].time || iTime > keys[keys.Length - 1].time) continue; //Skip curve if it does not include data for current time setting
            validCurves.Add(curve); //Otherwise, add curve
        }

        //Cleanup:
        return validCurves.ToArray(); //Return list of found curves as array
    }

    //Functionality Methods:
    public void Initialize(Transform parent)
    {
        //Function: Used to set up animation in scene (by acquiring in-scene configurations)

        //Initialize Configurations:
        originConfig = new Configuration(parent.Find(origin)); //Find matching configuration for origin
        List<Configuration> targetConfigList = new List<Configuration>(); //Initialize list to store created target configs
        for (int i = 0; i < targets.Length; i++) //Iterate through list of requested configs
        {
            targetConfigList.Add(new Configuration(parent.Find(targets[i].name))); //Find matching container and generate new configuration
        }
        targetConfigs = targetConfigList.ToArray(); //Save list of generated target configs in array (since a list will no longer be necessary)
            
        //Initialize Other Stuff:
        if (playBackwards) speedMultiplier = -1; //Set to play in reverse if requested
        currentTime = startingTime;              //Set current time to starting time
    }
    public void TimeStep(float deltaTime)
    {
        //Function: Applies given time to animation and responds accordingly if certain events occur

        //Initialization:
        if (!playing) return; //Prevents timestep from occurring while animation is not playing

        //Time Calculation:
        currentTime += deltaTime * speedMultiplier; //Apply speed multiplier to given time and add
        float clampedTime = Mathf.Clamp(currentTime, 0, time); //Clamp time between start and end values

        //Cleanup:
        if (clampedTime != currentTime) //Animation has hit start or end
        {
            playing = false; //End animation if it has hit the start or the end
            ShipAnimator.main.OnAnimationEnd(this); //Indicate that the animation has ended
        }
        currentTime = clampedTime; //Set new time
    }

    //Query Methods:
    public Configuration GetCurrentOrigin()
    {
        //Function: Returns origin configuration which corresponds to current interpolant time (for animations with three or more configs)

        //Initialization:
        if (targets.Length == 1) return originConfig; //If there is only one target config, automatically return the origin config

        //Find Config Adjacent to Current Target:
        int originIndex = GetIndexOfConfig(GetCurrentTarget()) - 1; //Get index directly before that of current target
        if (originIndex < 0) return originConfig;                   //Return base origin if target is first in array
        return targetConfigs[originIndex];                          //Return config directly before target
    }
    public Configuration GetCurrentTarget()
    {
        //Function: Returns target configuration which corresponds to current interpolant time (for animations with three or more configs)

        //Initialization:
        if (targetConfigs.Length == 1) return targetConfigs[0]; //If there is only one target config, return that one automatically

        //Find Earliest Config to Use:
        for (int i = targets.Length - 1; i >= 0; i--) //Iterate backward through list of potential target configs
        {
            if (targets[i].startTime <= InterpolantTime())
            {
                return targetConfigs[i]; //Return config if its start time has passed but the config after it's hasn't
            }
        }

        //Cleanup:
        return targetConfigs[targets.Length - 1]; //If no config could be found, return the last on in the array
    }
    public float RealInterpolantTime()
    {
        //Function: Returns the more granular interpolant time between the current origin and the current target (may be different from currentTime / time)
        //NOTE: This is necessary when smoothly transitioning an animation from one keyframe to the next

        //Initialization:
        if (targets.Length == 1) return currentTime / time; //If there is only one target config, return basic interpolant time

        //Get Start Time:
        float startTime = 0; //Initialize variable to store start time (assuming default position)
        if (GetCurrentOrigin() != originConfig) //Origin is not at default (start) position
        {
            startTime = targets[GetIndexOfConfig(GetCurrentTarget())].startTime; //Set startTime to that of target
        }

        //Get End Time:
        float endTime = 1; //Initialize variable to store end time (assuming default position)
        int targetIndex = GetIndexOfConfig(GetCurrentTarget()); //Get index of current target
        if (targetIndex < targets.Length - 1) //Target is not at default (end) position
        {
            endTime = targets[targetIndex + 1].startTime; //Get start time of next target and set it to end time of this target
        }

        //Calculate Offset Interpolant Time:
        //Debug.Log("Start Time = " + startTime);
        //Debug.Log("End Time = " + endTime);
        //Debug.Log("RealITime = " + Mathf.InverseLerp(startTime, endTime, InterpolantTime()));
        return Mathf.InverseLerp(startTime, endTime, InterpolantTime()); //Get interpolant between origin and target based on current time interpolant
    }
    public float InterpolantTime() { return currentTime / time; } //Return overall interpolant time

    //Utility Methods:
    private int GetIndexOfConfig(Configuration config)
    {
        //Function: Returns the index of given target config

        if (config == originConfig) return 0; //Return zero if config is the base origin (which it shouldn't be)
        for (int i = 0; i < targets.Length; i++) { if (targetConfigs[i] == config) return i; } //Return index of matching config
        return 0; //Return 0 if config wasn't found (this should never happen)
    }
}
