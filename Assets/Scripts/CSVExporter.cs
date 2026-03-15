using Oculus.Platform.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using static CSVExporter;

public class CSVExporter : MonoBehaviour
{
    // 인스펙터에서 채울 리스트
    //public List<PilotData> pilotdataList = new List<PilotData>();
    public List<StudyData> studydataList = new List<StudyData>();
    string date = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd");
    string time = DateTime.Now.ToLocalTime().ToString("HH mm ss");

    // 버튼이나 기타 이벤트에 연결
    public void ExportPilotDataToCSV(PilotData pilotData)
    {
        // 저장 경로: Android/iOS 에는 persistentDataPath, 에디터/PC 빌드에서는 dataPath 등 상황에 맞게 설정
        string filePath = Application.dataPath + $"/CSV/PilotData/{date} {time}.csv";

        // UTF-8 BOM 없이 저장하려면 Encoding.UTF8, 한글 깨짐이 있으면 Encoding.UTF8WithBOM 사용
        using (var sw = new StreamWriter(filePath, false, new UTF8Encoding(false)))
        {
            // 헤더
            sw.WriteLine("FoV 감소 인지 시점, FoV 감소 인지 FoV, 경험 저해 FoV, 선호 최소 FoV, PRV 감소 인지 시점, RPV 감소 인지 PRV, 경험 저해 PRV, 선호 최소 PRV, 선호 최소 Falloff Degree");

            // 쉼표가 포함될 수 있는 필드는 따옴표로 감싸거나, CSV 규격에 맞게 escaping 필요
            sw.WriteLine($"{pilotData.time_of_fov_reduction_detection},{pilotData.fov_reduction_detection},{pilotData.fovDiscomfortThreshold},{pilotData.preferredMinFoV}," +
                $"{pilotData.time_of_prv_reduction_detection},{pilotData.prv_reduction_detection},{pilotData.prvDiscomfortThreshold},{pilotData.preferredMinPRV}," + 
                $"{pilotData.preferredFalloffDegree}");
        }

        Debug.Log($"CSV 저장 완료 → {filePath}");
    }

    public void ExportStudyDataToCSV()
    {
        // 저장 경로: Android/iOS 에는 persistentDataPath, 에디터/PC 빌드에서는 dataPath 등 상황에 맞게 설정
        string filePath = Application.dataPath + $"/CSV/StudyData/{date} {time} ";

        // UTF-8 BOM 없이 저장하려면 Encoding.UTF8, 한글 깨짐이 있으면 Encoding.UTF8WithBOM 사용
        using (var sw = new StreamWriter(filePath, false, new UTF8Encoding(false)))
        {
            // 헤더
            sw.WriteLine("ㅇㅇ, ㅇㅇ, ");

            // 데이터 행
            foreach (var studydata in studydataList)
            {
                // 쉼표가 포함될 수 있는 필드는 따옴표로 감싸거나, CSV 규격에 맞게 escaping 필요
                //sw.WriteLine($"{},{},{}");
            }
        }

        Debug.Log($"CSV 저장 완료 → {filePath}");
    }

    [System.Serializable]
    public class PilotData
    {
        public float time_of_fov_reduction_detection;   // FoV 감소를 인지한 시점까지의 시간
        public float time_of_prv_reduction_detection;   // PRV 감소를 인지한 시점가지의 시간
        public float fov_reduction_detection;           // FoV 감소를 인지한 시점의 FoV
        public float prv_reduction_detection;           // PRV 감소를 인지한 시점의 PRV
        public float fovDiscomfortThreshold;            // 감소하는 FoV가 경험을 저해한다고 느끼는 FoV
        public float prvDiscomfortThreshold;            // 감소하는 PRV가 경험을 저해한다고 느끼는 PRV
        public float preferredMinFoV;                   // 실험 참가자의 선호 최소 FoV
        public float preferredMinPRV;                   // 실험 참가자의 선호 최소 PRV
        public float preferredFalloffDegree;            // 실험 참가자의 선호 Falloff Degree

        //public PilotData(float time_of_fov_reduction_detection, float time_of_oprs_reduction_detection, float fov_reduction_detection, float oprs_reduction_detection,
        //    float fovDiscomfortThreshold, float oprsDiscomfortThreshold, float preferredMinFoV, float preferredMinOPRS)
        //{
        //    this.time_of_fov_reduction_detection = time_of_fov_reduction_detection;
        //    this.time_of_oprs_reduction_detection = time_of_oprs_reduction_detection;
        //    this.fov_reduction_detection = fov_reduction_detection;
        //    this.oprs_reduction_detection = oprs_reduction_detection;
        //    this.fovDiscomfortThreshold = fovDiscomfortThreshold;
        //    this.oprsDiscomfortThreshold = oprsDiscomfortThreshold;
        //    this.preferredMinFoV = preferredMinFoV;
        //    this.preferredMinOPRS = preferredMinOPRS;
        //}
    }

    [System.Serializable]
    public class StudyData
    {


        public StudyData()
        {

        }
    }
}
