using System.Collections;
using UnityEngine;

namespace SwissArmySpline
{
    public class SAS_DemoCameraController : MonoBehaviour
    {
        // Public Variables ========================================================================
        [Header("Camera Control")]
        [Range(0.1f, 3.0f)]
        public float lookSensitivity = 1;
        [Range(0.1f, 10.0f)]
        public float moveSensitivity = 5;
        public Vector3 worldLimits = new Vector3(1000, 500, 1000);

        // Private Variables =======================================================================
        Transform t;
        Transform child;
        Transform camT;
        float targetPan, targetPitch;
        Vector3 targetPos;
        //==========================================================================================




        void Start()
        {
            SetupCameraRig();
            targetPos = transform.position;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        } //========================================================================================


        void Update()
        {
            CameraControl();
        } //========================================================================================

        void CameraControl()
        {

            targetPan += Input.GetAxisRaw("Mouse X") * lookSensitivity;
            targetPitch -= Input.GetAxisRaw("Mouse Y") * lookSensitivity;
            t.localEulerAngles += new Vector3(0, Input.GetAxisRaw("Mouse X") * lookSensitivity, 0);
            child.localEulerAngles -= new Vector3(0, 0, Input.GetAxisRaw("Mouse Y") * lookSensitivity);
            Vector3 motion = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) motion.z += 1;
            if (Input.GetKey(KeyCode.S)) motion.z -= 1;
            if (Input.GetKey(KeyCode.D)) motion.x += 1;
            if (Input.GetKey(KeyCode.A)) motion.x -= 1;
            if (Input.GetKey(KeyCode.E)) motion.y += 1;
            if (Input.GetKey(KeyCode.Q)) motion.y -= 1;
            if (Input.GetKey(KeyCode.LeftShift)) motion *= 5;
            t.position += camT.TransformDirection(motion) * Time.unscaledDeltaTime * 16 * moveSensitivity;

            // Limits
            if (t.position.y > worldLimits.y) t.position = new Vector3(t.position.x, worldLimits.y, t.position.z);
            if (t.position.y < 2) t.position = new Vector3(t.position.x, 2, t.position.z);
            if (t.position.x > worldLimits.x) t.position = new Vector3(worldLimits.x, t.position.y, t.position.z);
            if (t.position.x < -worldLimits.x) t.position = new Vector3(-worldLimits.x, t.position.y, t.position.z);
            if (t.position.z > worldLimits.z) t.position = new Vector3(t.position.x, t.position.y, worldLimits.z);
            if (t.position.z < -worldLimits.z) t.position = new Vector3(t.position.x, t.position.y, -worldLimits.z);
        } //========================================================================================

        void SetupCameraRig()
        {
            t = transform;
            Transform[] children = t.GetComponentsInChildren<Transform>();
            child = new GameObject("Tilt").transform;
            child.parent = t;
            child.localPosition = Vector3.zero;
            child.transform.localEulerAngles = new Vector3(0, 90, 33);
            for (int i = 0; i < children.Length; i++) { if (children[i] != t) children[i].parent = child; }
            camT = Camera.main.transform;
            camT.parent = child;
            camT.forward = t.forward;
            camT.localPosition = Vector3.zero;
            camT.localRotation = Quaternion.Euler(0, -90, 0);
        } //========================================================================================
    }
}
