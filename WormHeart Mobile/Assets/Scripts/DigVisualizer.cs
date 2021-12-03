using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DigVisualizer : MonoBehaviour
{
    //Description: Processes data from ship control scripts and visualizes digging process based on current behavior and statuses

    //Objects & Components:
    public static DigVisualizer main; //Singleton instance of this script
    [Space()]
    public Transform branchRef;       //Container in scene with references for default positions of horizontal tunnel elements
    public GameObject branchInstance; //Prefab containing objects used to build horizontal tunnel
    public Transform digSpace;        //Moving object which drillShip travels through and branches are built in (but not main shaft)
    public Transform branchDigTool;   //Transform marker used to determine behavior of branch start animation

    //Settings:

    //Memory & Status Vars:
    private Transform currentBranch;   //Branch ship is currently in (if any)

    //Runtime Methods:
    private void Awake()
    {
        //Initialization:
        if (main == null) { main = this; } else { Destroy(this); }

        //Get Objects & Components:

    }
    private void Update()
    {
        if (currentBranch != null) UpdateBranch(); //Update current branch
    }

    //Functionality Methods:
    public void BuildBranch()
    {
        //Function: Deploys branch instance and plays build animation

        //Set Up New Branch:
        GameObject branch = Instantiate(branchInstance); //Instantiate a new instance of branch
        branch.transform.position = transform.position;  //Align branch with current position of ship
        branch.transform.parent = digSpace;              //Child branch to digSpace so that it moves with the rest of the ground
        currentBranch = branch.transform;                //Save reference of current branch to script
    }
    public void UpdateBranch()
    {
        //Initialization:
        if (currentBranch == null) return; //Skip if ship is not currently in a branch

        //Determine Build Behavior Based on Ship State:
        if (ShipAnimator.main.mode == ShipAnimator.ShipMode.transitioning) //Ship is currently transitioning
        {
            //Initialization:
            float branchYPos = currentBranch.Find("endL").position.y; //Get current Y position of tunnel elements
            float digExtent = branchDigTool.position.x; //Get current X position of dig tool (attached to ship mode transition animation)

            //Extend Both Tunnels Evenly:
            currentBranch.Find("endL").position = new Vector3(-digExtent, branchYPos, transform.position.z); //Hard set position of tunnel end
            currentBranch.Find("endR").position = new Vector3(digExtent, branchYPos, transform.position.z);  //Hard set position of tunnel end
            currentBranch.Find("shaftL").localScale = new Vector3(currentBranch.Find("shaftL").localScale.x, digExtent * 100, 1); //Hard set position of tunnel shaft
            currentBranch.Find("shaftR").localScale = new Vector3(currentBranch.Find("shaftR").localScale.x, digExtent * 100, 1); //Hard set position of tunnel shaft
        }
        else //Ship is in full horizontal mode
        {
            //Initialization:
            float tunnelEndOffset = branchRef.Find("endR").position.x;         //Get base position offset from center of DrillShip
            float currentLeftExtent = transform.position.x - tunnelEndOffset;  //Get current furthest position left
            float currentRightExtent = transform.position.x + tunnelEndOffset; //Get current furthest position right

            //Extend Tunnel:
            if (currentBranch.Find("endL").position.x > currentLeftExtent) //Branch needs to be extended
            {
                //Extend Branch End:
                Vector3 v = currentBranch.Find("endL").position; //Record current position of branch
                v.x = currentLeftExtent;                         //Modify position to fit extent
                currentBranch.Find("endL").position = v;         //Apply position to branch end

                //Extend Branch Shaft:
                v = currentBranch.Find("shaftL").localScale; //Reassign vector container to get record from shaft
                v.y = Mathf.Abs(currentLeftExtent * 100);    //Scale tunnel to reach new end position (tunnels are rotated so scale needs to be absolute)
                currentBranch.Find("shaftL").localScale = v; //Apply scale to branch tunnel
            }
            if (currentBranch.Find("endR").position.x < currentRightExtent) //Branch needs to be extended
            {
                //Extend Branch End:
                Vector3 v = currentBranch.Find("endR").position; //Record current position of branch
                v.x = currentRightExtent;                        //Modify position to fit extent
                currentBranch.Find("endR").position = v;         //Apply position to branch end

                //Extend Branch Shaft:
                v = currentBranch.Find("shaftR").localScale; //Reassign vector container to get record from shaft
                v.y = currentRightExtent * 100;              //Scale tunnel to reach new end position
                currentBranch.Find("shaftR").localScale = v; //Apply scale to branch tunnel
            }
        }
        
    }
    public void EndBranch()
    {
        //Function: Completes current branch

        currentBranch = null; //Release reference for current branch
    }
    //Utility Functions:

}

//BONEYARD:
/*
    //Initialization: 
    Sprite cuttingSprite = cutterMask.sprite; //Get mask with which to cut sprite
    Sprite cutSprite = cutVolume.sprite; //Get existing sprite from cutting volume
    Vector2 cutPos = cutterMask.transform.position; //Get position of cutting mask (to offset cutting action)
        

    //Get New Geometry:
    ushort[] cuttingTris = cuttingSprite.triangles;
    Vector2[] cuttingVerts = cuttingSprite.vertices;
    //for (int i = 0; i < cuttingVerts.Length; i++) { cuttingVerts[i] += cutPos; } //Offset vertices by world transform

    //DEBUG Draw Triangles:
    int a, b, c;
    for (int i = 0; i < cuttingTris.Length; i = i + 3)
    {
        a = cuttingTris[i];
        b = cuttingTris[i + 1];
        c = cuttingTris[i + 2];
        Debug.DrawLine(cuttingVerts[a], cuttingVerts[b], Color.white, 100.0f);
        Debug.DrawLine(cuttingVerts[b], cuttingVerts[c], Color.white, 100.0f);
        Debug.DrawLine(cuttingVerts[c], cuttingVerts[a], Color.white, 100.0f);
    }

    SpriteRenderer oog = new SpriteRenderer();
    Sprite testSprite = oog.sprite;
    Bounds test = oog.bounds;
    //testSprite.
*/
