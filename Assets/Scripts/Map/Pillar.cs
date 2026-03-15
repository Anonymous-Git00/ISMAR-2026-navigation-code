using UnityEngine;

public class Pillar : MonoBehaviour
{
    [SerializeField]
    private PillarController pillarController;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            pillarController.ActiveNextOne();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            pillarController.ActiveNextOne();
        }
    }
}
