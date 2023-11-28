using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MultipleSceneManager : MonoBehaviour
{
    [SerializeField]
    private string[] m_sceneToLoad = new string[2];
    [SerializeField]
    private bool m_loadScenesWhenStart = false;
    // Start is called before the first frame update
    void Start()
    {
        if(m_loadScenesWhenStart)
        {
            LoadMultipleScenes();
        }
    }

    public void LoadMultipleScenes()
    {

        foreach (string sceneName in m_sceneToLoad)
        {
            if(sceneName != null || sceneName != "")
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
    }
}
