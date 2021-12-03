using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class CutoutMaskUI : Image
{
    //Description: Replaces the image component of an object, creates cutout mask with masked parent
    //CREDIT: This script was made based on a tutorial by Code Monkey (https://www.youtube.com/watch?v=XJJl19N2KFM).  None of the code in this script is mine, but for what it's worth I do understand it

    public override Material materialForRendering
    {
        //Function (as I understand it): Overrides stencil properties of material on an image to reverse the stencil function (used in masking)

        get
        {
            Material material = new Material(base.materialForRendering);    //Create a new material so that base material is not changed
            material.SetInt("_StencilComp", (int)CompareFunction.NotEqual); //Reverse stencil operation
            return material;
        }
    }
}
