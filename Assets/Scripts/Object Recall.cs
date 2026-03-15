using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectRecall : MonoBehaviour
{
    [Header("후보 위치 (18개)")]
    public Transform[] candidatePositions; // 반드시 18개 할당

    [Header("오브젝트 세트 (각 9개씩, 총 4세트)")]
    public GameObject[] set1Objects;
    public GameObject[] set2Objects;
    public GameObject[] set3Objects;
    public GameObject[] set4Objects;

    [Header("세트 활성화 여부 (한 개만 true)")]
    public bool useSet1;
    public bool useSet2;
    public bool useSet3;
    public bool useSet4;

    [Header("더미 오브젝트 (6개)")]
    public GameObject[] dummyObjects; // 반드시 6개 할당

    private GameObject[] selectedSet;

    void Start()
    {
        SelectSet();
        SpawnObjects();
    }

    void SelectSet()
    {
        if (useSet1) selectedSet = set1Objects;
        else if (useSet2) selectedSet = set2Objects;
        else if (useSet3) selectedSet = set3Objects;
        else if (useSet4) selectedSet = set4Objects;
        else
        {
            Debug.LogError("세트를 하나도 선택하지 않았습니다! bool 값 중 하나를 true로 설정하세요.");
        }

        if (selectedSet.Length != 9)
        {
            Debug.LogError("선택된 세트에 오브젝트가 정확히 9개가 아닙니다!");
        }

        if (dummyObjects.Length != 6)
        {
            Debug.LogError("더미 오브젝트는 정확히 6개여야 합니다!");
        }
    }

    void SpawnObjects()
    {
        if (selectedSet == null || candidatePositions.Length < 15) return;

        // 후보 위치 18개 중 15개 랜덤 선택
        List<Transform> shuffledPositions = candidatePositions.OrderBy(x => Random.value).ToList();
        List<Transform> chosenPositions = shuffledPositions.Take(15).ToList();

        // 타겟 9개 + 더미 6개 합치고 셔플
        List<GameObject> allObjects = new List<GameObject>();
        allObjects.AddRange(selectedSet);
        allObjects.AddRange(dummyObjects);

        List<GameObject> shuffledObjects = allObjects.OrderBy(x => Random.value).ToList();

        // 매칭해서 배치
        for (int i = 0; i < 15; i++)
        {
            //Instantiate(shuffledObjects[i], chosenPositions[i].position, chosenPositions[i].rotation);
            Instantiate(shuffledObjects[i], chosenPositions[i].position, shuffledObjects[i].transform.rotation);
        }
    }
}
