using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ItemXRInteractable : XRGrabInteractable
{
    private bool isGrabbed = false;
    private bool isDroppable = false;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        if (!isGrabbed)
        {
            isGrabbed = true;
            isDroppable = true;
        }
        else
        {
            isGrabbed = false;
            isDroppable = false;
            Drop();
        }
    }
    public void ForceDrop(XRBaseInteractor interactor)
    {
        OnSelectExited(interactor);
    }
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        if (!isGrabbed && isDroppable)
        {
            isDroppable = false;
        }
    }

}
