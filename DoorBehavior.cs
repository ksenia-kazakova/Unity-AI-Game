using UnityEngine;

public class DoorBehavior : MonoBehaviour
{
    private GameBehavior gameBehavior;

    void Start()
    {
        gameBehavior = FindFirstObjectByType<GameBehavior>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            gameBehavior.WinGame();
        }
    }
}
