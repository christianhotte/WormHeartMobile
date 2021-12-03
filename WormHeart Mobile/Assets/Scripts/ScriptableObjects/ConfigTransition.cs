using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/ConfigTransition", order = 1)]
public class ConfigTransition : ScriptableObject
{
    //Description: Used to smoothly transition from the middle of one animation to that of another

    //Settings:
    public float transitionTime; //Time taken between beginning of transition and entry into natural lerp state of animation

    //Memory Vars:
    internal Vector3 origPos;      //Saved position origin generated at start of transition
    internal Quaternion origRot;   //Saved rotation origin generated at start of transition
    internal Vector3 origScale;    //Saved scale origin generated at start of transition
    internal float timePassed = 0; //Time (in seconds) which has been spent in this transition if active
}
