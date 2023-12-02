using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheckInput : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField inputField;

    [SerializeField]
    private string expectedValue;

    public void CheckIput()
    {
        string enteredValue = inputField.text;

        if (enteredValue == expectedValue)
        {
            Debug.Log("Input is correct!");
        }
        else
        {
            Debug.Log("Input is incorrect. Please try again.");
        }
    }

}
