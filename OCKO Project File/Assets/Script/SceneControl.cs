using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneControl : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("Button was clicked!");
            SceneManager.LoadScene("SampleScene");
        }
    }
}
