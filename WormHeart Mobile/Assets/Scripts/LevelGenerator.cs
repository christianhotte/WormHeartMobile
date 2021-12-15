using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    //Description: Procedurally generates level as player digs deeper

    //Objects & Components:
    public GameObject[] dirtPrefabs; //Array of level objects which can be spawned by the level generator
    public Transform latestLayer;    //The lowest layer of dirt (should also be set in scene)

    //Runtime Methods:
    private void Update()
    {
        float currentLayerDist = latestLayer.position.y;
        if (currentLayerDist >= -0.64f) //Check if latest layer has passed threshold for spawning a new layer
        {
            GameObject newLayer = Instantiate(dirtPrefabs[Random.Range(0, dirtPrefabs.Length)], transform); //Instantiate a new level object (inside digSpace)
            latestLayer = newLayer.transform; //Replace latest layer with newly-spawned layer
            latestLayer.transform.position = new Vector3(0, -1.28f, 0); //Move new layer to correct position
        }
    }
}
