using UnityEngine;

public class PlayerInputManager : MonoBehaviour
{
    public Transform SpawnPoint1, SpawnPoint2;
    public GameObject Player1, Player2;

    private void Awake()
    {
        Instantiate(Player1, SpawnPoint1.position, SpawnPoint1.rotation);
        Instantiate(Player2, SpawnPoint2.position, SpawnPoint2.rotation);
    }
}
