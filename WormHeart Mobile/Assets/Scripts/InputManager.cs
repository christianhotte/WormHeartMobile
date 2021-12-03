using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    //Description: Gets information from phone and refines to variables readable by control scripts

    //Objects & Components:
    internal static InputManager main; //Global object other scripts can reference easily

    //Settings:
    [Header("Settings:")]
    public float checkOrientFreq;  //How frequently (in seconds between checks) the phone's orientation is checked
    public int orientAvgGroupSize; //How many orientation readings to remember at a time and average between (to smooth orientation data)
    public bool debugStuff;        //Used to turn debug logs and lines off and on

    //Input Vars:
    internal Vector3 orientation;    //Normalized vector representing the spatial direction the screen of the phone is facing (when phone is being held still)
    internal Vector3 avgOrientation; //Average of the last [orientationSmoothingGroupSize] orientation vectors

    //Memory Vars:
    private List<Vector3> orientationMem = new List<Vector3>(); //List of past orientation readings used for smoothing

    //RUNTIME METHODS:
    private void Awake()
    {
        //Singleton-ize:
        if (main == null) main = this; //Set this script to global input manager
        else Destroy(this); //Destroy this script if there is more than one of it in scene

        //Begin Coroutines:
        StartCoroutine(OrientationDataChecker()); //Start phone orientation checks (will repeat at given frequency for program runtime)
    }
    private void Update()
    {
        //Input Debugging:
        if (debugStuff)
        {
            Debug.DrawLine(transform.position, orientation, Color.yellow);
            Debug.DrawLine(transform.position, avgOrientation, Color.green);
            print(orientation);
        }
    }
    IEnumerator OrientationDataChecker()
    {
        //Function: Checks orientation data at a set interval, and fills in input (and memory) variables related to phone orientation

        for (;;) //Run code indefinitely
        {
            //Get Orientation Data:
            Vector3 rawOrientation = Input.acceleration; //Get acceleration from phone (should be relatively normalized to gravity, floats around 1 when phone is still, and points down from bottom of phone)
            orientation = Quaternion.Euler(90, 0, 0) * rawOrientation; //Rotate raw orientation vector so that it points in the direction the phone is facing

            //Update Orientation Average:
            orientationMem.Add(orientation); //Add orientation to memory
            if (orientationMem.Count > orientAvgGroupSize) orientationMem.RemoveAt(0); //Trim memory to ensure it stays within desired group size (allowing memory to be tracked across specific length of time)
            Vector3 totalOrientation = new Vector3(); //Initialize empty vector to store totals of all orientation variables
            foreach (Vector3 memOrient in orientationMem) totalOrientation += memOrient; //Scrub through all orientation vectors in memory and add them together
            avgOrientation = totalOrientation / orientationMem.Count; //Get average orientation in memory by dividing totals of all orientation vectors by number of vectors in memory

            //Wait for Given Interval:
            yield return new WaitForSeconds(checkOrientFreq); //Check orientation again after the frame closest to when given amount of seconds has passed
        }
    }
}
