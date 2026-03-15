using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.InputSystem;
using static CSVExporter;

public class OVRControllHandler : MonoBehaviour
{
    private OVRInputActions ovrInput;
    [SerializeField]
    private VignetteController vignetteController;
    [SerializeField]
    private Vignette vignette;
    [SerializeField]
    private OVRPassthroughLayer passthroughLayer;
    [SerializeField]
    private CSVExporter csvExporter;

    private bool keyboard9Flag = true;

    // 인터벌 측정용
    private float lastDecreaseFOVTime = -1f;
    private float lastIncreasePRVTime = -1f;

    public float DecreaseFOVInterval { get; private set; }
    public float IncreasePRVInterval { get; private set; }

    int keyboard1PressCount = 0;
    int keyboard2PressCount = 0;
    int keyboard3PressCount = 0;
    int keyboard4PressCount = 0;

    PilotData pilotData;
    StudyData studyData;

    private void Awake()
    {
        ovrInput = new OVRInputActions();
    }

    private void Start()
    {
        // VignetteController 초기화
        if (vignetteController == null)
        {
            vignetteController = FindFirstObjectByType<VignetteController>();
        }

        pilotData = new PilotData();
        studyData = new StudyData();
    }

    private void OnEnable()
    {
        // 각 버튼 액션 활성화
        ovrInput.OVRController.Enable();

        // 개별 버튼 이벤트 구독
        ovrInput.OVRController.ButtonA.performed += OnButtonAPressed;
        ovrInput.OVRController.ButtonB.performed += OnButtonBPressed;
        ovrInput.OVRController.ButtonX.performed += OnButtonXPressed;
        ovrInput.OVRController.ButtonY.performed += OnButtonYPressed;

        ovrInput.OVRController.Keyboard1.performed += OnKeyboard1Pressed;
        ovrInput.OVRController.Keyboard2.performed += OnKeyboard2Pressed;
        ovrInput.OVRController.Keyboard3.performed += OnKeyboard3Pressed;
        ovrInput.OVRController.Keyboard4.performed += OnKeyboard4Pressed;
        ovrInput.OVRController.Keyboard5.performed += OnKeyboard5Pressed;
        ovrInput.OVRController.Keyboard6.performed += OnKeyboard6Pressed;

        ovrInput.OVRController.Keyboard9.performed += OnKeyboard9Pressed;
        ovrInput.OVRController.Keyboard0.performed += OnKeyboard0Pressed;

        //ovrInput.OVRController.Keyboard2.performed += vignetteController.ToggleDecreaseFOV();
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        ovrInput.OVRController.ButtonA.performed -= OnButtonAPressed;
        ovrInput.OVRController.ButtonB.performed -= OnButtonBPressed;
        ovrInput.OVRController.ButtonX.performed -= OnButtonXPressed;
        ovrInput.OVRController.ButtonY.performed -= OnButtonYPressed;

        ovrInput.OVRController.Keyboard1.performed -= OnKeyboard1Pressed;
        ovrInput.OVRController.Keyboard2.performed -= OnKeyboard2Pressed;
        ovrInput.OVRController.Keyboard3.performed -= OnKeyboard3Pressed;
        ovrInput.OVRController.Keyboard4.performed -= OnKeyboard4Pressed;
        ovrInput.OVRController.Keyboard5.performed -= OnKeyboard5Pressed;
        ovrInput.OVRController.Keyboard6.performed -= OnKeyboard6Pressed;

        ovrInput.OVRController.Keyboard9.performed -= OnKeyboard9Pressed;
        ovrInput.OVRController.Keyboard0.performed -= OnKeyboard0Pressed;

        ovrInput.OVRController.Disable();
    }

    private void OnButtonBPressed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        Debug.Log("B Button Pressed (Right Controller)");

        if (vignette.vignetteFalloffDegrees == 0)
        {
            return;
        }

        vignette.forceVignetteValue += 10;
        vignette.vignetteFalloffDegrees -= 10;
    }

    private void OnButtonAPressed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        Debug.Log("A Button Pressed (Right Controller)");

        if (vignette.forceVignetteValue - 10 <= 0)
        {
            return;
        }

        vignette.forceVignetteValue -= 10;
        vignette.vignetteFalloffDegrees += 10;
    }

    private void OnButtonYPressed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        Debug.Log("Y Button Pressed (Left Controller)");


    }

    private void OnButtonXPressed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        Debug.Log("X Button Pressed (Left Controller)");


    }

    private void OnKeyboard1Pressed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        Debug.Log("Keyboard1 Pressed");
        keyboard1PressCount++;

        vignetteController.ToggleIncreaseFOV();

        if (keyboard1PressCount == 2)
        {
            pilotData.preferredMinFoV = vignette.forceVignetteValue;
            Debug.Log($"[Handler] 선호 최소 FOV: {vignette.forceVignetteValue:F2}도");
        }
    }

    private void OnKeyboard2Pressed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        Debug.Log("Keyboard2 Pressed");
        keyboard2PressCount++;

        vignetteController.ToggleDecreaseFOV();

        float now = Time.time;

        if (lastDecreaseFOVTime >= 0f)
        {
            DecreaseFOVInterval = now - lastDecreaseFOVTime;

            if (keyboard2PressCount == 2)
            {
                pilotData.time_of_fov_reduction_detection = DecreaseFOVInterval;
                pilotData.fov_reduction_detection = vignette.forceVignetteValue;
                Debug.Log($"[Handler] DecreaseFOV 인터벌 : {DecreaseFOVInterval:F2}초");
                Debug.Log($"[Handler] FOV 감소 인지 시점의 FOV : {vignette.forceVignetteValue:F2}도");
            }
        }
        lastDecreaseFOVTime = now;

        if (keyboard2PressCount == 4)
        {
            pilotData.fovDiscomfortThreshold = vignette.forceVignetteValue;
            Debug.Log($"[Handler] 사용자 경험 저하 FOV : {vignette.forceVignetteValue:F2}도");
        }
    }

    private void OnKeyboard3Pressed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        Debug.Log("Keyboard3 Pressed");
        keyboard3PressCount++;

        vignetteController.ToggleIncreasePRV();

        //if (keyboard3PressCount == 2)
        //{
        //    pilotData.preferredMinPRV = passthroughLayer.textureOpacity;
        //    Debug.Log($"[Handler] 선호 최소 PRV: {passthroughLayer.textureOpacity:F2}");
        //}

        float now = Time.time;

        if (lastIncreasePRVTime >= 0f)
        {
            IncreasePRVInterval = now - lastIncreasePRVTime;

            if (keyboard3PressCount == 2)
            {
                pilotData.time_of_prv_reduction_detection = IncreasePRVInterval;
                pilotData.prv_reduction_detection = passthroughLayer.textureOpacity;
                Debug.Log($"[Handler] IncreasePRV 인터벌: {IncreasePRVInterval:F2}초");
                Debug.Log($"[Handler] PRV 감소 인지 시점의 PRV : {passthroughLayer.textureOpacity:F2}");
            }
        }
        lastIncreasePRVTime = now;

        if (keyboard3PressCount == 4)
        {
            pilotData.prvDiscomfortThreshold = passthroughLayer.textureOpacity;
            Debug.Log($"[Handler] PRVDiscomfortThreshold: {passthroughLayer.textureOpacity:F2}");
        }
    }

    private void OnKeyboard4Pressed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        Debug.Log("Keyboard4 Pressed");
        keyboard4PressCount++;

        vignetteController.ToggleDecreasePRV();

        //float now = Time.time;

        //if (lastDecreasePRVTime >= 0f)
        //{
        //    DecreasePRVInterval = now - lastDecreasePRVTime;

        //    if (keyboard4PressCount == 2)
        //    {
        //        pilotData.time_of_prv_reduction_detection = DecreasePRVInterval;
        //        pilotData.prv_reduction_detection = passthroughLayer.textureOpacity;
        //        Debug.Log($"[Handler] DecreasePRV 인터벌: {DecreasePRVInterval:F2}초");
        //        Debug.Log($"[Handler] PRV 감소 인지 시점의 PRV : {passthroughLayer.textureOpacity:F2}");
        //    }
        //}
        //lastDecreasePRVTime = now;

        //if (keyboard4PressCount == 4)
        //{
        //    pilotData.prvDiscomfortThreshold = passthroughLayer.textureOpacity;
        //    Debug.Log($"[Handler] PRVDiscomfortThreshold: {passthroughLayer.textureOpacity:F2}");
        //}

        if (keyboard4PressCount == 2)
        {
            pilotData.preferredMinPRV = passthroughLayer.textureOpacity;
            Debug.Log($"[Handler] 선호 최소 PRV: {passthroughLayer.textureOpacity:F2}");
        }
    }

    private void OnKeyboard5Pressed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        Debug.Log("Keyboard5 Pressed");

        if (vignette.forceVignetteValue - 10 <= 0)
        {
            return;
        }

        vignette.forceVignetteValue -= 10;
        vignette.vignetteFalloffDegrees += 10;
    }

    private void OnKeyboard6Pressed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        Debug.Log("Keyboard6 Pressed");

        if (vignette.vignetteFalloffDegrees == 0)
        {
            return;
        }

        vignette.forceVignetteValue += 10;
        vignette.vignetteFalloffDegrees -= 10;
    }

    private void OnKeyboard9Pressed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        Debug.Log("Keyboard9 Pressed");

        if (keyboard9Flag)
        {
            passthroughLayer.enabled = false;
        }
        else
        {
            passthroughLayer.enabled = true;
        }

        keyboard9Flag = !keyboard9Flag;
    }

    private void OnKeyboard0Pressed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        Debug.Log("Keyboard0 Pressed");

        pilotData.preferredFalloffDegree = vignette.vignetteFalloffDegrees;

        csvExporter.ExportPilotDataToCSV(pilotData);
    }
}