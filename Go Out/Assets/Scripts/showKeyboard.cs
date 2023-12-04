using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

public class showKeyboard : MonoBehaviour
{
    private TMP_InputField inputField;
    public float distance = 0.5f;
    public float verticaloffset = -0.5f;

    public Transform positionSource;
    // Start is called before the first frame update
    void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        inputField.onSelect.AddListener(x => OpenKeyboard());
    }

    public void OpenKeyboard()
    {
        NonNativeKeyboard.Instance.InputField = inputField;
        NonNativeKeyboard.Instance.PresentKeyboard(inputField.text);

        Vector3 direction = positionSource.forward;
        direction.y = 0;
        direction.Normalize();

        Vector3 targetPosition = positionSource.position + direction * distance + Vector3.up * verticaloffset;

        NonNativeKeyboard.Instance.RepositionKeyboard(targetPosition);

        //SetCaretColorAlpha(1);
        NonNativeKeyboard.Instance.OnClosed += Instance_OnClosed;

    }

    private void Instance_OnClosed(object sender, System.EventArgs e)
    {

        //SetCaretColorAlpha(0);
    } 
    //private void SetCaretColorAlpha(float value)
    //{
    //    inputField.customCaretColor = true;
    //    Color caretColor = inputField.caretColor;
    //    caretColor.a = value;
    //    inputField.caretColor = caretColor;
    //}
}
