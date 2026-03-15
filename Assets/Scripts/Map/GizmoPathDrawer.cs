using UnityEngine;

public class GizmoPathDrawer : MonoBehaviour
{
    // 인스펙터 창에서 연결할 게임 오브젝트들을 담을 배열
    public GameObject[] waypoints;

    void OnDrawGizmos()
    {
        // 배열이 비어있거나 요소가 1개 이하면 선을 그릴 수 없으므로 함수 종료
        if (waypoints == null || waypoints.Length <= 1)
        {
            return;
        }

        // 기즈모 선 색상 설정
        Gizmos.color = Color.cyan; // 밝은 청록색

        // 배열의 첫 번째 요소부터 마지막 직전 요소까지 반복
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            // 현재 오브젝트와 다음 오브젝트가 모두 할당되었는지 확인
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                // 현재 지점(i)과 다음 지점(i+1)의 위치를 가져옴
                Vector3 currentPoint = waypoints[i].transform.position;
                Vector3 nextPoint = waypoints[i + 1].transform.position;

                // 두 지점 사이에 선을 그립니다.
                Gizmos.DrawLine(currentPoint, nextPoint);
            }
        }
    }
}