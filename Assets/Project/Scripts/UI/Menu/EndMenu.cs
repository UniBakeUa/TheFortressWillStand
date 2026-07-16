using UnityEngine;
using UnityEngine.SceneManagement;

public class EndMenu : MonoBehaviour
{
    public static EndMenu Instance;

    [SerializeField] private GameObject endMenu;

    private void Awake()
    {
        Instance = this;
    }

    public void Show()
    {
        endMenu.SetActive(true);
    }

    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
