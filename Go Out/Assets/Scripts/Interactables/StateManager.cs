using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class StateManager : MonoBehaviour
{
    [SerializeField] private UnityEvent toggleOnAction;
    [SerializeField] private UnityEvent toggleOffAction;
    [SerializeField] private bool isOn = false;
    [SerializeField] private AudioClip m_ToggleSound;
    private AudioSource audioSource;
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
        audioSource = GetComponent<AudioSource>();
        if(audioSource!=null && m_ToggleSound != null)
        {
            audioSource.clip = m_ToggleSound;
            audioSource.PlayOneShot(m_ToggleSound);
        }
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
