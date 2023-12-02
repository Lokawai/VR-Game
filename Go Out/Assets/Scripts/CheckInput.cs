using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class CheckInput : MonoBehaviour
{
    [SerializeField]
    private List<InputToCheck> inputs;

    [SerializeField]
    private UnityEvent onFailed;

    [SerializeField]
    private UnityEvent onPassed;

    private void Awake()
    {
        foreach (InputToCheck input in inputs)
            input.inputField.onEndEdit.AddListener(text => PassCheck());
    }

    public void PassCheck()
    {
        foreach (InputToCheck input in inputs)
        {
            // Fail if any of the input is incorrect
            if (input.inputField.text != input.expectedValue)
            {
                onFailed.Invoke();
                return;
            }
        }

        // All input check passed
        onPassed.Invoke();
    }

    [System.Serializable]
    public struct InputToCheck
    {
        public TMP_InputField inputField;
        public string expectedValue;
    }
}
