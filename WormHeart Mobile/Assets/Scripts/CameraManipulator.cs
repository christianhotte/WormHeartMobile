using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManipulator : MonoBehaviour
{
    //Description: Moves the camera based on input and game state

    //Classes, Enums & Structs:

    //Objects & Components:
    private Camera cam; //Reference to main camera object


    //Runtime Methods:
    private void Awake()
    {
        //Get Objects & Components:
        cam = Camera.main; //Get reference to camera (not super necessary but eckgh)
    }
}
