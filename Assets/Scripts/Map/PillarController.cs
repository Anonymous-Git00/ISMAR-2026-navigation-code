using UnityEngine;

public class PillarController : MonoBehaviour
{
    [SerializeField]
    private GameObject[] pillars;
    private int pillarIndex = 0;

    public void ActiveNextOne()
    {
        pillars[pillarIndex++].SetActive(false);
        pillars[pillarIndex].SetActive(true);
    }
}
