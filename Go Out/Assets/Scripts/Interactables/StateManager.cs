using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class StateManager : MonoBehaviour
{
    [SerializeField] private UnityEvent toggleOnAction;
    [SerializeField] private UnityEvent toggleOffAction;
    [SerializeField] private bool isOn = false;
    public void ToggleOn()
    {
        isOn = !isOn;
        InvokeAction();
    }
    public void ChangeOnState(bool value)
    {
        isOn = value;
        InvokeAction();
    }
    private void InvokeAction()
    {
        switch (isOn)
        {
            case true:
                toggleOnAction.Invoke();
                break;
            case false:
                toggleOffAction.Invoke();
                break;
        }
    }
}
