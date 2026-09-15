using UnityEngine;
using UnityEngine.SceneManagement;

public class ReplayGame : MonoBehaviour
{
    public void Restart()
    {
        SceneLoader.Instance.FullRestart();
    }
    
}
