using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoolToggler : MonoBehaviour
{
    [SerializeField]
    private Animator animator;
    public void ToggleState(string name = "UIState")
    {
        animator.SetBool(name, !animator.GetBool(name));
    }
    public void SetState(bool state)
    {
        string name = "UIState";
        animator.SetBool(name, state);
    }
}
