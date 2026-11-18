using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneControl : MonoBehaviour
{
    void Update()
    {
		if (Input.GetKeyDown(KeyCode.Space))
		{
            Debug.Log("Button was clicked!");
            SceneManager.LoadScene("SampleScene");
        }
    }
}
