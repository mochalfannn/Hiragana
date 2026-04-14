using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    private float lastBackPressTime = 0f; 
    private const float doubleTapTime = 0.5f; 
    
    void Start()
    {
        
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            if (Time.time - lastBackPressTime < doubleTapTime)
            {
                OnExitButton(); 
            }
            else
            {
                lastBackPressTime = Time.time; 
            }
        }
    }

    public void OnExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Fungsi untuk berpindah ke scene tertentu
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("Level");
    }

    public void BackMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadGame()
    {
        SceneManager.LoadScene("Core-Games");
    }public void LoadQuestion()
    {
        SceneManager.LoadScene("Question");
    }

}