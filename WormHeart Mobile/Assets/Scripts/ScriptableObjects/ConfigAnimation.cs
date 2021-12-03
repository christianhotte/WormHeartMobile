using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/ConfigAnimation", order = 1)]
public class ConfigAnimation : ScriptableObject
{
    //Description: Contains data which describes an animation between two configurations
    //NOTE: Multiple curves can be used at a time, just be careful about using element masks to prevent them from overlapping (proto feature)

    //Objects & Components:
    internal Configuration originConfig; //Origin configuration for animation
    internal Configuration targetConfig; //Target configuration for animation

    //Settings:
    public string origin;        //User-set configuration origin
    public string target;        //User-set configuration target
    public float time;           //Total animation length (in seconds)
    public float startingTime;   //Time (in seconds) where animation starts by default
    public bool playBackwards;   //Sets initial speedMultiplier to -1
    public MaskedCurve[] curves; //Curves describing motion of the animation

    //Memory & Status Vars:
    internal float currentTime = 0;     //Current time marker in animation (in seconds)
    internal float speedMultiplier = 1; //Modifies speed at which animation plays
    internal bool playing = false;      //True if animation is currently being played

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

    //Utility Methods:
    public void Initialize(Transform parent)
    {
        //Function: Used to set up animation in scene (by acquiring in-scene configurations)

        //Initialize Configurations:
        originConfig = new Configuration(parent.Find(origin)); //Find matching configuration
        targetConfig = new Configuration(parent.Find(target)); //Find matching configuration

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
    public float InterpolantTime() { return (currentTime / time); } //Returns currentTime as value between 0 and 1
}
