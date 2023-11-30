using UnityEngine;
using UnityEngine.Events;

public class StartFunctionBehaviour : MonoBehaviour
{
    [SerializeField]
    private UnityEvent startFunction;

    private void Start()
        => startFunction?.Invoke(); 
}
