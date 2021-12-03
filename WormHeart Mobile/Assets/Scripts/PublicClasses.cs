using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Configuration
{
    //Description: Contains transform data for every element in the ship (used for lerping between poses)

    //Ship Elements:
    internal Transform drill;    //Transform of drill object
    internal Transform winch;    //Transform of winch object
    internal Transform screw_FL; //Transform of forward-left screw object
    internal Transform screw_FR; //Transform of forward-right screw object
    internal Transform screw_BL; //Transform of back-left screw object
    internal Transform screw_BR; //Transform of back-right screw object
    internal Transform core;     //Transform of core object

    internal Transform[] elements; //Array of elements in configuration

    //Methods:
    public Configuration(Transform container)
    {
        //Function: Constructor, gets element transforms from given container

        //Get Elements:
        drill = container.Find("drill");
        winch = container.Find("winch");
        screw_FL = container.Find("screw_FL");
        screw_FR = container.Find("screw_FR");
        screw_BL = container.Find("screw_BL");
        screw_BR = container.Find("screw_BR");
        core = container.Find("core");

        //Organize Elements in Array:
        elements = new Transform[] { drill, winch, screw_FL, screw_FR, screw_BL, screw_BR, core }; //Initialize array of all elements
    }
    public void FillGaps(Configuration refConfig)
    {
        //Function: Fills in gaps in this configuration with elements from given reference configuration (to minimize redundant game objects)

        //Initialization:
        if (this == refConfig) return; //Skip if reference config is the same as given config

        //Check for Missing Elements:
        for (int i = 0; i < elements.Length; i++) //Iterate through list of elements
        {
            if (elements[i] == null) //The given configuration did not contain this element
            {
                elements[i] = refConfig.elements[i]; //Substitute element from reference configuration
            }
        }
    }
}

[System.Serializable]
public class MaskedCurve
{
    //Description: Animation curve with mask options which allow the user to move different elements at different speeds
    //NOTE: Element mask may be left empty (or incomplete) if items do not need to be masked out

    //Data:
    public AnimationCurve curve; //AnimationCurve being used
    public bool[] elementMask; //Used to mask out specific elements (order depends on order of element hierarchy)
    public bool includePos;    //Used to mask out positional changes (if needed)
    public bool includeRot;    //Used to mask out rotational changes (if needed)
    public bool includeScl;    //Used to mask out scalar changes (if needed)

    //Query Methods:
    public int GetMask()
    {
        //Function: Returns bitmask based on given inspector inputs

        //Initialization:
        int mask = 0; //Initialize mask to return

        //Generate Mask:
        for (int i = elementMask.Length - 1; i >= 0; i--) //Iterate (backwards) through selections in given element mask
        {
            mask = mask << 1; //Shift previous bit one position to the left
            if (elementMask[i]) mask |= 0b1; //Add bit to mask if selected
        }


        //Cleanup:
        return mask; //Return generated mask
    }
}