using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchManager : MonoBehaviour
{
    //Created: 10-3-21
    //Purpose: Detecting and translating commands from player input and triggering events in GameDirector

    //Classes, Enums & Structs:
    [System.Serializable] public class TouchData
    {
        //Purpose: Object containing data relevant for a touch being actively tracked by the program
        //         This is used to create an organized layer of information separate from the touch list itself, in order to simplify the program and prevent errors

        //Data:
        public Vector2 position; //The current point of contact for the touch (in screen pixel space)
        public Vector2 delta;    //How much this touch has moved since last update
        public Vector2 origin;   //The initial point of contact for the touch
        public int fingerID;     //The ID number linking this object to an existing touch
        public float touchTime = 0; //The amount of time this touch has been active for

        //Meta:
        public bool markedForDisposal = false; //Set true once this object's associated touch has ended
        public bool markedComplete = false;    //Set true once this touch creates an input event (does not destroy object but prevents it from triggering more events)
    }

    //Objects & Components:
    public static TouchManager inputManager;

    //Conditions & Memory Vars:
    private Touch[] touches; //Array of active touch inputs
    internal List<TouchData> touchDataList = new List<TouchData>(); //Companion list to touches array for tracking origin positions (replacement for touch.rawPosition)

    private void Awake()
    {
        //Initialize as sole input manager:
        if (inputManager == null) inputManager = this;
        else Destroy(this);
    }

    private void Update()
    {
        //Detect Input (parse touch arrays):
        touches = Input.touches; //Get most recent array of touches
        if (touches.Length > 0) //TOUCH INPUT:
        {
            foreach (Touch touch in touches) //Parse through list of touches
            {
                TouchData touchData = GetTouchDataByID(touch.fingerId); //Try and retrieve existing touch data
                if (touchData == null) //Create new touch data object
                {
                    touchData = new TouchData();
                    touchData.position = touch.position;
                    touchData.origin = touch.position;
                    touchData.fingerID = touch.fingerId;
                    touchDataList.Add(touchData); //Add new data to list
                    TouchStarted(touchData); //Indicate to program that this touch has begun
                }
            }
        }
        if (touchDataList.Count > 0) //TOUCH INPUT DATA:
        {
            foreach (TouchData data in touchDataList) //Parse through list of touchData
            {
                if (data.markedForDisposal) continue; //Pass items marked for disposal
                bool foundTouch = false;
                foreach (Touch touch in touches) //Look for matching touch
                {
                    if (touch.fingerId == data.fingerID) //Data still has a matching touch
                    {
                        foundTouch = true;
                        if (data.position != touch.position) //Touch has moved since last update
                        {
                            data.delta = touch.position - data.position; //Calculate delta between before and after positions
                            data.position = touch.position; //Update position data
                            TouchMoved(data);
                        }
                        break;
                    }
                }
                if (!foundTouch) //Touch is no longer in array and has ended
                {
                    TouchEnded(data); //Indicate to program that this touch has ended
                    data.markedForDisposal = true;
                }
                else //Touch has continued to this frame
                {
                    data.touchTime += Time.deltaTime;
                }
            }
            for (int i = 0; i < touchDataList.Count;) //Very smart cool nifty totally efficient way to clean up list without throwing indexoutofrange errors
            {
                TouchData data = touchDataList[i]; //Get first item from touchData list
                if (data.markedForDisposal) touchDataList.RemoveAt(i); //Remove item if marked for deletion
                else i++; //Move on to next item if not marked for deletion
            }
        }
    }

    //INPUT EVENTS:
    private void TouchStarted(TouchData data)
    {
        //Process Input Event:
        
    }
    private void TouchMoved(TouchData data)
    {
        //Process Input Event:
        
    }
    private void TouchEnded(TouchData data)
    {
        //Process Input Event:

    }

    //UTILITY METHODS:
    public Vector3 ActualScreenToWorldPoint(Vector2 screenPosition)
    {
        //Function: Does what ScreenToWorldPoint should do ):

        Vector3 worldPosition = screenPosition; //Make it a V3
        worldPosition.z = -Camera.main.transform.position.z; //Offset given position by Z position of camera
        worldPosition = Camera.main.ScreenToWorldPoint(worldPosition); //Then do the thing
        return worldPosition;
    }
    private TouchData GetTouchDataByID(int ID)
    {
        //Function: Returns item from touch data array with ID matching given number (or null if none exists)

        if (touchDataList.Count == 0) return null; //Return null if there are no items to return
        foreach (TouchData item in touchDataList) if (item.fingerID == ID) return item; //Parse through list and return matching item if found
        return null; //If matching item is never found, return null
    }
}
