using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Unity.Netcode;
public class TimeManager : MonoBehaviour
{
    [Header("Unit: second")]
    [SerializeField] private NetworkVariable<float> m_TimeRemain = new NetworkVariable<float>();
    public float TimeRemain { get { return m_TimeRemain.Value; } }
    [SerializeField] private float m_MaxTime = 600;
    [SerializeField] private bool isActive = false;
    [SerializeField] private UnityEvent m_EndTimeEvent;
    [SerializeField] private float returnTime = 5f;
    public float ReturnTime { get { return returnTime; } }
    [SerializeField] private bool isReturning = false;
    [Header("Display Settings")]
    [SerializeField] private TMP_Text m_DisplayText;
    [SerializeField] private string m_TimeDisplayHeader = "";
    public string m_ReturnDisplayHeader = "";
    [SerializeField] private float m_ResetTimer = 0f;
    public void SetValueTimeDisplay(string value)
    {
        m_DisplayText.text = value;
    } 
    private void Start()
    {
        m_TimeRemain.Value = 0f;
    }
    public void ActiveTimer()
    {
        m_TimeRemain.Value = m_MaxTime;
        isActive = true;

    }
    public void ActiveReturnTimer()
    {
        m_TimeRemain.Value = returnTime;
        isActive = true;
        isReturning = true;
    }
    public void Reset()
    {
        isActive = false;
        m_TimeRemain.Value = 0f;
    }
    private void EndTimeInvoke()
    {
        if(!isReturning)
         m_EndTimeEvent.Invoke();
        isActive = false;
        m_TimeRemain.Value = 0f;
        isReturning = false;
    }
    private void Update()
    {
        if(isActive)
        {
            RunTimer();
        }
    }
    private void RunTimer()
    {
        if (m_TimeRemain.Value > 0)
        {
            m_TimeRemain.Value -= Time.deltaTime;
            if(!isReturning)
                m_DisplayText.text = m_TimeDisplayHeader + "\n"+ TimeUnit.getTimeUnit(m_TimeRemain.Value);
            else
                m_DisplayText.text = m_ReturnDisplayHeader +"\n"+ "Return in " + (int)(m_TimeRemain.Value) + "s";
        } else
        {

            StartCoroutine(EndTimerText());
            EndTimeInvoke();
        }

    }
    private IEnumerator EndTimerText()
    {
        
        yield return new WaitForSeconds(m_ResetTimer);
        m_DisplayText.text = "";
    }
}
