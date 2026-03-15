using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class Vignette : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField, Tooltip("표시에 걸리는 시간")] private float _showIntervalTime = 0.125f;
    [SerializeField, Tooltip("숨김에 걸리는 시간")] private float _hideIntervalTime = 0.25f;

    [SerializeField, Tooltip("Vignette용 Mesh의 형태(기본 Normal로 문제 없습니다)")]
    private MeshComplexityLevel meshComplexity = MeshComplexityLevel.Normal;
    [SerializeField, Tooltip("Vignette의 Falloff 설정")]
    private FalloffType falloff = FalloffType.Linear;

    [Header("Vignette Properties")]
    [Range(0, 120), Tooltip("Vignette의 수직 FOV")]
    public float baseVignetteFieldOfView = 60;
    [Tooltip("Vignette의 수평 FOV를 조정하는 Aspect Ratio")]
    public float vignetteAspectRatio = 1f;
    [Tooltip("Vignette의 Falloff 범위(도)")]
    public float vignetteFalloffDegrees = 10f;
    [ColorUsage(true)] public Color vignetteColor;
    [Tooltip("반투명과 불투명 Vignette 사이의 간격")]
    public float middleOffset = 1.02f;

    // FoV 동적 조절을 위한 추가 변수
    [Header("Dynamic FoV Settings")]
    [Tooltip("동적 FoV 조절 활성화")]
    public bool enableDynamicFOV = true;
    [Range(0, 120), Tooltip("모션에 따른 최대 FoV 증가량 (기본 FoV에 더해짐)")]
    public float maxFovIncreaseFromMotion = 20f;

    [Header("Dynamic Alpha Settings")]
    [Tooltip("동적 Alpha 조절 활성화")]
    public bool enableDynamicAlpha = true;
    [Range(0f, 1f), Tooltip("기본 Alpha 값 (0-1 범위)")]
    public float baseAlpha = 0.8f;
    [Range(0f, 1f), Tooltip("모션에 따른 최대 Alpha 감소량")]
    public float maxAlphaIncreaseFromMotion = 0.01f;
    [Tooltip("댐핑 시간 (작을수록 빠르게, 클수록 부드럽고 느리게)")]
    public float alphaSmoothTime = 0.2f;

    private float alphaVelocity;  // 내부 속도 보관용

    // 내부적으로 사용될 현재 vignetteFieldOfView
    private float _currentVignetteFieldOfView;

    // 내부적으로 사용될 현재 Alpha 값
    private float _currentVignetteAlpha;

    [Header("Motion Detection")]
    [Tooltip("Motion calculated using this Transform. Generally shouldn't use HMD")]
    public Transform motionTarget;

    [Header("Angular Velocity")]
    [Tooltip("Add angular velocity to effect strength?")]
    public bool useAngularVelocity = true;
    [Range(0, 2f)]
    public float angularVelocityStrength = 1f;
    [Tooltip("No effect contribution below this angular velocity. Degrees per second")]
    public float angularVelocityMin = 0f;
    [Tooltip("Clamp effect contribution above this angular velocity. Degrees per second")]
    public float angularVelocityMax = 180f;
    [Tooltip("Smoothing time for angular velocity calculation. 0 for no smoothing")]
    public float angularVelocitySmoothing = 0.15f;

    [Header("Linear Acceleration")]
    [Tooltip("Add linear acceleration to effect strength?")]
    public bool useAcceleration = false;
    [Range(0, 2f)]
    public float accelerationStrength = 1f;
    [Tooltip("No effect contribution below this acceleration. Metres per second squared")]
    public float accelerationMin = 0f;
    [Tooltip("Clamp effect contribution above this acceleration. Metres per second squared")]
    public float accelerationMax = 10f;
    [Tooltip("Smoothing time for acceleration calculation. 0 for no smoothing")]
    public float accelerationSmoothing = 0.15f;

    [Header("Linear Velocity")]
    [Tooltip("Add translation velocity to effect strength?")]
    public bool useVelocity = false;
    [Range(0, 2f)]
    public float velocityStrength = 1f;
    [Tooltip("No effect contribution below this velocity. Metres per second")]
    public float velocityMin = 0f;
    [Tooltip("Clamp effect contribution above this velocity. Metres per second")]
    public float velocityMax = 10f;
    [Tooltip("Smoothing time for velocity calculation. 0 for no smoothing")]
    public float velocitySmoothing = 0.15f;

    //[Header("Effect Coverage")]
    //[Range(0f, 120f), Tooltip("Maximum screen coverage")]
    //public float effectCoverage = 0.75f;

    public enum ForceVignetteMode { NONE = 0, CONSTANT = 1, CONSTANT_FOV = 2 }

    [Header("ForceVignette")]
    [Tooltip("강제로 Vignette 커버리지를 설정할 모드")]
    public ForceVignetteMode forceVignetteMode = ForceVignetteMode.NONE;
    [Range(0f, 120f), Tooltip("CONSTANT 모드일 때 적용할 Vignette 강제값")]
    public float forceVignetteValue = 0f;
    [Range(0f, 1f), Tooltip("CONSTANT 모드일 때 적용할 Alpha 강제값")]
    public float forceAlphaValue = 0.96f;

    // 내부 모션 트래킹 변수들
    private Vector3 _lastPosition;
    private Vector3 _lastForward;
    private Quaternion _lastRotation;
    private float _lastSpeed;
    private Vector3 _lastVelocity;

    // 값을 부드럽게 처리하기 위한 슬루 변수들
    private float _avSmoothed, _avSlew, _speedSmoothed, _speedSlew, _accelSmoothed, _accelSlew;

    // 현재 모션 강도 (0~1)
    private float _currentMotionStrength = 0f;

    // 애니메이션 상태 추적용 변수
    private float _rate = 0f;            // 현재 렌더링 비율
    private float _targetRate = 0f;      // 목표 렌더링 비율
    private bool _isAnimating = false;   // 애니메이션 진행 중 플래그
    private bool _isVignetteStart = true;// 첫 계산 시점 판별용

    /// <summary>
    /// Vignette용 Mesh의 형태
    /// </summary>
    private enum MeshComplexityLevel
    {
        VerySimple,
        Simple,
        Normal,
        Detailed,
        VeryDetailed
    }

    /// <summary>
    /// Vignette의 Falloff 설정
    /// </summary>
    private enum FalloffType
    {
        Linear,
        Quadratic
    }

    // 쉐이더 키워드 문자열
    private static readonly string QUADRATIC_FALLOFF = "QUADRATIC_FALLOFF";

    [SerializeField][HideInInspector] private Shader vignetteShader;

    [SerializeField] private OVRPassthroughLayer ovrpassthroughlayer;

    // 카메라 & 렌더러/메시 필터 참조
    [SerializeField] private Camera _camera;
    private MeshFilter _opaqueMeshFilter;
    private MeshFilter _transparentMeshFilter;
    private MeshRenderer _opaqueMeshRenderer;
    private MeshRenderer _transparentMeshRenderer;

    // 동적으로 생성할 메시와 머티리얼
    private Mesh _opaqueMesh;
    private Mesh _transparentMesh;
    private Material _opaqueMaterial;
    private Material _transparentMaterial;

    // 쉐이더 프로퍼티 ID
    private int _shaderScaleAndOffset0Property;
    private int _shaderScaleAndOffset1Property;

    // Eye별(양안 렌더링) Scale·Offset 계산을 위한 배열
    private readonly float[] _innerScaleX = new float[2];
    private readonly float[] _innerScaleY = new float[2];
    private readonly float[] _middleScaleX = new float[2];
    private readonly float[] _middleScaleY = new float[2];
    private readonly float[] _outerScaleX = new float[2];
    private readonly float[] _outerScaleY = new float[2];
    private readonly float[] _offsetX = new float[2];
    private readonly float[] _offsetY = new float[2];
    private readonly float[] _maxVignetteRange = new float[2];

    // 최종적으로 쉐이더에 전달할 Vector4 배열
    private readonly Vector4[] _TransparentScaleAndOffset0 = new Vector4[2];
    private readonly Vector4[] _TransparentScaleAndOffset1 = new Vector4[2];
    private readonly Vector4[] _OpaqueScaleAndOffset0 = new Vector4[2];
    private readonly Vector4[] _OpaqueScaleAndOffset1 = new Vector4[2];

    // 가시성 검사 결과 플래그
    private bool _opaqueVignetteVisible = false;
    private bool _transparentVignetteVisible = false;

#if UNITY_EDITOR
    // 에디터용 초기값 저장
    private MeshComplexityLevel _InitialMeshComplexity;
    private FalloffType _InitialFalloff;
#endif

    private void OnEnable()
    {
        // 렌더링 파이프라인 후킹
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        ResetMotion();  // 모션 초기화
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        DisableRenderers(); // 렌더러 끄기
    }

    public void Awake()
    {
        // motionTarget이 설정되지 않았다면 카메라를 사용
        if (motionTarget == null && _camera != null)
            motionTarget = _camera.transform;
        else if (_camera == null)
        {
            Debug.LogError("Vignette: Camera is not assigned and motionTarget is null!", this);
            enabled = false; // 컴포넌트 비활성화
            return;
        }

        // 쉐이더 프로퍼티 ID 캐싱
        _shaderScaleAndOffset0Property = Shader.PropertyToID("_ScaleAndOffset0");
        _shaderScaleAndOffset1Property = Shader.PropertyToID("_ScaleAndOffset1");

        // FoV 초기값 설정
        _currentVignetteFieldOfView = baseVignetteFieldOfView;

        // Alpha 초기값 설정
        ovrpassthroughlayer.textureOpacity = vignetteColor.a;

        // Opaque/Transparent GameObject & 컴포넌트 생성
        GameObject opaqueObject = new GameObject("Opaque Vignette") { hideFlags = HideFlags.HideAndDontSave };
        opaqueObject.transform.SetParent(_camera.transform, false);
        _opaqueMeshFilter = opaqueObject.AddComponent<MeshFilter>();
        _opaqueMeshRenderer = opaqueObject.AddComponent<MeshRenderer>();
        // 렌더러 설정: 그림자·라이트·리플렉션 비활성
        _opaqueMeshRenderer.receiveShadows = false;
        _opaqueMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _opaqueMeshRenderer.lightProbeUsage = LightProbeUsage.Off;
        _opaqueMeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _opaqueMeshRenderer.allowOcclusionWhenDynamic = false;
        _opaqueMeshRenderer.enabled = false;

        GameObject transparentObject = new GameObject("Transparent Vignette") { hideFlags = HideFlags.HideAndDontSave };
        transparentObject.transform.SetParent(_camera.transform, false);
        _transparentMeshFilter = transparentObject.AddComponent<MeshFilter>();
        _transparentMeshRenderer = transparentObject.AddComponent<MeshRenderer>();
        _transparentMeshRenderer.receiveShadows = false;
        _transparentMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _transparentMeshRenderer.lightProbeUsage = LightProbeUsage.Off;
        _transparentMeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _transparentMeshRenderer.allowOcclusionWhenDynamic = false;
        _transparentMeshRenderer.enabled = false;

        // 배열 초기화
        _innerScaleX.Initialize();
        _innerScaleY.Initialize();
        _middleScaleX.Initialize();
        _middleScaleY.Initialize();
        _outerScaleX.Initialize();
        _outerScaleY.Initialize();
        _offsetX.Initialize();
        _offsetY.Initialize();
        _maxVignetteRange[0] = 1.0f;
        _maxVignetteRange[1] = 1.0f;

        BuildMeshes();        // 메시 생성
        BuildMaterials();     // 머티리얼 생성
        ResetMotion();        // 모션 초기화
        CalculateVignetteScaleAndOffset(); // 최초 스케일 계산
    }

    private void Update()
    {
        if (_camera == null || motionTarget == null || _opaqueMaterial == null) // 필수 컴포넌트 체크
            return;

#if UNITY_EDITOR
        // 에디터 중 변경 감지
        if (meshComplexity != _InitialMeshComplexity)
        {
            BuildMeshes();
        }
        if (falloff != _InitialFalloff)
        {
            BuildMaterials();
        }
#endif

        //if (_opaqueMaterial == null)
        //    return;

        // CONSTANT 모드일 때는 모션 계산을 건너뛰고 직접 설정
        if (forceVignetteMode == ForceVignetteMode.CONSTANT)
        {
            _targetRate = 1f; // 항상 활성화
            _rate = _targetRate; // 즉시 적용
            _isAnimating = false; // 애니메이션 비활성화

            _currentVignetteFieldOfView = forceVignetteValue;

            ovrpassthroughlayer.textureOpacity = forceAlphaValue;

            Refresh();
            return;
        }

        // 모션 계산 및 동적 비네팅 적용
        float deltaTime = Time.deltaTime;
        _currentMotionStrength = CalculateMotion(deltaTime);

        // --- FOV 동적 조절 로직 ---
        if (enableDynamicFOV)
        {
            // 모션 강도에 따라 목표 FoV 계산 (예: 모션이 강할수록 FoV 감소 = 비네팅 영역 축소)
            float targetFov = baseVignetteFieldOfView - (_currentMotionStrength * maxFovIncreaseFromMotion);
            // 현재 FoV를 목표 FoV로 부드럽게 변경하거나 직접 설정
            _currentVignetteFieldOfView = targetFov; // 여기서는 직접 설정, 필요시 Mathf.Lerp 등 사용

            Debug.Log("CurrentVignetteFieldOfView : " + _currentVignetteFieldOfView);

            // FoV가 변경되었으므로 스케일 재계산
            CalculateVignetteScaleAndOffset();
        }
        // --- FOV 동적 조절 로직 끝 ---

        // --- Alpha 동적 조절 로직 추가 ---
        if (enableDynamicAlpha)
        {
            // 모션 강도에 따라 목표 Alpha 계산 (모션이 강할수록 Alpha 감소 = 더 투명)
            /* OVRPassthrough는 검은색, 불투명 배경에 투영됨.
             * baseAlpha가 1이고 baseAlpha - (_currentMotionStrength * maxAlphaIncreaseFromMotion);가 논리상 맞음
             * 하지만 OVRPassthrough는 Alpha가 감소할수록 보이지 않고 0이되면 검은 화면이 됨.
             * baseAlpha가 0이고 baseAlpha + (_currentMotionStrength * maxAlphaIncreaseFromMotion);가 되어야함
            */
            float targetAlpha = baseAlpha + (_currentMotionStrength * maxAlphaIncreaseFromMotion);
            //_currentVignetteAlpha = Mathf.Clamp01(targetAlpha);
            targetAlpha = Mathf.Clamp01(targetAlpha);

            // SmoothDamp로 자연스럽게 전환
            ovrpassthroughlayer.textureOpacity = Mathf.SmoothDamp(
                ovrpassthroughlayer.textureOpacity,
                targetAlpha,
                ref alphaVelocity,
                alphaSmoothTime
            );

            //ovrpassthroughlayer.textureOpacity = _currentVignetteAlpha;
          
            Debug.Log("CurrentMotionStrength : " + _currentMotionStrength);
            Debug.Log("CurrentVignetteAlpha : " + ovrpassthroughlayer.textureOpacity);
        }
        else
        {
            // 동적 Alpha가 비활성화된 경우 기본값 사용
            ovrpassthroughlayer.textureOpacity = vignetteColor.a;
        }

        if (forceVignetteMode == ForceVignetteMode.CONSTANT_FOV)
        {
            _targetRate = 1f; // 항상 활성화
            _rate = _targetRate; // 즉시 적용
            _isAnimating = false; // 애니메이션 비활성화

            _currentVignetteFieldOfView = forceVignetteValue;

            Refresh();
            return;
        }

        // 모션 강도에 따라 자동으로 비네팅 조절
        UpdateVignetteFromMotion();

        if (!_isAnimating)
        {
            SetVignetteMaterial();  // 애니메이션이 없으면 바로 셋
            return;
        }

        _transparentVignetteVisible = false;
        _opaqueVignetteVisible = false;

        // 애니메이션 진행: rate 값을 목표로 선형 보간
        if (_targetRate > _rate)
        {
            var rateDelta = deltaTime / _showIntervalTime;
            _rate = Mathf.Min(_rate + rateDelta, _targetRate); // Clamp 대신 Min/Max 사용으로 목표값 정확히 도달
            if (_rate >= _targetRate)
            {
                _rate = _targetRate;
                _isAnimating = false;
            }
        }
        else if (_targetRate < _rate) // _targetRate < _rate 조건 명확화
        {
            var rateDelta = deltaTime / _hideIntervalTime;
            _rate = Mathf.Max(_rate - rateDelta, _targetRate); // Clamp 대신 Min/Max 사용
            if (_rate <= _targetRate)
            {
                _rate = _targetRate;
                _isAnimating = false;
            }
        }
        else // _targetRate == _rate
        {
            _isAnimating = false;
        }

        SetVignetteMaterial();
    }

    /// <summary>
    /// 모션을 계산하고 효과 강도를 반환
    /// </summary>
    private float CalculateMotion(float deltaTime)
    {
        if (motionTarget == null)
            return 0f;

        deltaTime = Mathf.Max(deltaTime, 0.000001f);
        float fx = 0f;

        // Angular Velocity 계산
        Vector3 forward = motionTarget.forward;
        if (useAngularVelocity)
        {
            float av = Vector3.Angle(_lastForward, forward) / deltaTime;
            av = Mathf.InverseLerp(angularVelocityMin, angularVelocityMax, av);
            _avSmoothed = Mathf.SmoothDamp(_avSmoothed, av, ref _avSlew, angularVelocitySmoothing);
            fx += _avSmoothed * angularVelocityStrength;
            Debug.Log("Angular Velocity : " + fx);
        }

        // Velocity & Acceleration 계산
        Vector3 currentPos = motionTarget.position;
        Vector3 deltaPos = currentPos - _lastPosition;
        Vector3 velocity = deltaPos / deltaTime;
        float speed = velocity.magnitude;

        if (useVelocity)
        {
            _speedSmoothed = Mathf.SmoothDamp(_speedSmoothed, speed, ref _speedSlew, velocitySmoothing);
            float lm = 0f;
            if (!Mathf.Approximately(velocityMax, velocityMin))
            {
                lm = Mathf.Clamp01((_speedSmoothed - velocityMin) / (velocityMax - velocityMin));
            }
            fx += lm * velocityStrength;
            Debug.Log("Linear Velocity : " + fx);
        }

        if (useAcceleration)
        {
            float accel = Mathf.Abs(speed - _lastSpeed) / deltaTime;
            if (!Mathf.Approximately(accelerationMax, accelerationMin))
            {
                accel = Mathf.Clamp01((accel - accelerationMin) / (accelerationMax - accelerationMin));
            }
            _accelSmoothed = Mathf.SmoothDamp(_accelSmoothed, accel, ref _accelSlew, accelerationSmoothing);
            fx += _accelSmoothed * accelerationStrength;
            Debug.Log("Linear Acceleration : " + fx);
        }

        // 이전 프레임 값 저장
        _lastForward = forward;
        _lastPosition = currentPos;
        _lastSpeed = speed;
        _lastVelocity = velocity;
        _lastRotation = motionTarget.rotation;

        // 최종 효과 강도 계산 (0-1 범위로 정규화)
        //float coverage01 = Mathf.Clamp01(effectCoverage / 120f);
        //fx = RemapRadius(fx) * RemapRadius(coverage01);

        //fx = RemapRadius(fx);

        Debug.Log("Last Effect Calculation : " + fx);
        return Mathf.Clamp01(fx);
    }

    /// <summary>
    /// 모션 강도에 따라 비네팅을 자동 조절
    /// </summary>
    private void UpdateVignetteFromMotion()
    {
        // 모션이 감지되면 비네팅 표시, 아니면 숨김
        float threshold = 0.1f; // 모션 감지 임계값

        if (_currentMotionStrength > threshold)
        {
            // 모션 강도에 따라 타겟 레이트 조절
            _targetRate = Mathf.Lerp(0.3f, 1f, _currentMotionStrength);
            if (!_isAnimating || _targetRate > _rate)
            {
                _isAnimating = true;
                if (_isVignetteStart)
                {
                    CalculateVignetteScaleAndOffset();
                    _isVignetteStart = false;
                }
            }
        }
        else
        {
            // 모션이 없으면 비네팅 숨김
            if (_rate > 0f)
            {
                _targetRate = 0f;
                _isAnimating = true;
            }
        }
    }

    /// <summary>
    /// 반지름 기반 정규화. 최소 COVERAGE_MIN부터 1까지 매핑
    /// </summary>
    private float RemapRadius(float radius)
    {
        const float COVERAGE_MIN = 0.65f;
        return Mathf.Lerp(COVERAGE_MIN, 1f, radius);
    }

    /// <summary>
    /// 모션 관련 변수 초기화
    /// </summary>
    private void ResetMotion()
    {
        if (motionTarget == null)
            return;

        _lastForward = motionTarget.forward;
        _lastPosition = motionTarget.position;
        _lastRotation = motionTarget.rotation;
        _lastSpeed = 0f;
        _avSmoothed = _avSlew = _speedSmoothed = _speedSlew = _accelSmoothed = _accelSlew = 0f;
    }

    /// <summary>
    /// 현재 meshComplexity 값에 대응하는 삼각형 개수 반환
    /// </summary>
    private int GetTriangleCount()
    {
        switch (meshComplexity)
        {
            case MeshComplexityLevel.VerySimple: return 32;
            case MeshComplexityLevel.Simple: return 64;
            case MeshComplexityLevel.Normal: return 128;
            case MeshComplexityLevel.Detailed: return 256;
            case MeshComplexityLevel.VeryDetailed: return 512;
            default: return 128;
        }
    }

    //    /// <summary>
    //    /// Vignette용 불투명 Mesh와 반투명 Mesh를 생성
    //    /// </summary>
    private void BuildMeshes()
    {
#if UNITY_EDITOR
        // 에디터 플레이 중 파라미터 변경 감지용 초기값 갱신
        _InitialMeshComplexity = meshComplexity;
#endif

        // Vignette용 Mesh를 구성하는 삼각형의 수 ※ 사각형 Vignette를 위해서는 8개의 Vertex가 필요
        int triangleCount = GetTriangleCount();

        // Vignette용 반투명 Mesh의 Vertex 버퍼 및 UV 정보 저장
        Vector3[] innerVerts = new Vector3[triangleCount];
        Vector2[] innerUVs = new Vector2[triangleCount];

        // Vignette용 불투명 Mesh의 Vertex 버퍼 및 UV 정보 저장
        Vector3[] outerVerts = new Vector3[triangleCount];
        Vector2[] outerUVs = new Vector2[triangleCount];

        // Vignette용 반투명 Mesh와 불투명 Mesh의 Index 버퍼(Vertex의 Triangle 순서) 저장
        int[] tris = new int[triangleCount * 3];

        // Vignette용 Mesh 생성
        // 2개씩 짝지어 "선"으로 접근 → 쉐이더에서 UV.x 로 삼각형 형태로 변형
        // 이 처리만으로는 Vertex가 삼각형이 아니라 선이 되지만, Shader 측에서 UV의 X 값으로 Outer인지 Inner인지 판정하여 이동시키면 삼각형이 됨
        for (int i = 0; i < triangleCount; i += 2)
        {
            // Vertex 좌표가 원주 위가 되도록 계산
            float angle = 2 * i * Mathf.PI / triangleCount;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            // 불투명 Mesh의 Vertex, UV 정보 저장
            // outerVerts[i]와 outerVerts[i + 1]는 같은 좌표
            outerVerts[i] = new Vector3(x, y, 0);
            outerVerts[i + 1] = new Vector3(x, y, 0);

            // UV의 X는 Vertex Shader에서 Vertex를 외측/내측으로 이동할지 판정하는 데 사용
            // 결과적으로 outerVerts[i]는 외측, outerVerts[i + 1]는 내측으로 이동
            // UV의 Y는 Vignette Mesh의 반투명도(알파)에 사용되므로, 불투명도는 1로 설정
            outerUVs[i] = new Vector2(0, 1);
            outerUVs[i + 1] = new Vector2(1, 1);

            // 반투명 Mesh의 Vertex, UV 정보 저장
            // innerVerts[i]와 innerVerts[i + 1]는 같은 좌표
            innerVerts[i] = new Vector3(x, y, 0);
            innerVerts[i + 1] = new Vector3(x, y, 0);

            // UV의 X는 Vertex Shader에서 Vertex를 외측/내측으로 이동할지 판정하는 데 사용
            // 결과적으로 innerVerts[i]는 내측, innerVerts[i + 1]는 외측으로 이동
            // UV의 Y는 Vignette Mesh의 반투명도(알파)에 사용
            // 외측->내측이 1->0이므로 반투명 Vignette가 그라데이션이 됨
            innerUVs[i] = new Vector2(0, 1);
            innerUVs[i + 1] = new Vector2(1, 0);

            // 삼각형의 Index 계산
            int ti = i * 3;
            tris[ti] = i;
            tris[ti + 1] = i + 1;
            tris[ti + 2] = (i + 2) % triangleCount;
            tris[ti + 3] = i + 1;
            tris[ti + 4] = (i + 3) % triangleCount;
            tris[ti + 5] = (i + 2) % triangleCount;

            // 예를 들어, triangleCount가 8일 경우, Mesh의 모양은 아래와 같다 (불투명, 반투명도 같은 모양)
            //                      , - ~V23~ - ,
            //                  , '    I     I    ' ,
            //                ,     I           I     ,
            //               ,   I                 I   ,
            //              , I                       I ,
            //              V45                         V01
            //              , I                       I ,
            //               ,   I                 I   ,
            //                ,     I           I     ,
            //                  ,      I     I     , '
            //                    ' - , _V67_ ,  '
            // (V는 Vertex와 그 번호, I는 Index 순서대로 그린 선)
            //
            // 같은 좌표에 있는 Vertex는 Vertex Shader에서 이동되어 삼각형이 된다
            // UV의 X 값으로 Vertex를 바깥쪽으로 이동할지, 안쪽으로 이동할지 판정한다
            // 이동 값은 CalculateVignetteScaleAndOffset에서 계산하여 Shader에 전달한다
            //
            // 예를 들어, triangleCount가 8일 경우의 Mesh 모양은 아래와 같다
            //                            V2
            //                         I  I  I
            //                     I    I I I    I
            //                  I   ,  I~ I ~I  ,   I
            //               I  , '   I   I   I   ' ,  I
            //            I   ,      I    V3    I     ,   I 
            //         I     ,      I   I    I   I     ,     I
            //      I       ,      I  I        I  I     ,       I
            //    V4 I I I I I I I V5            V1 I I I I I I I V0
            //      I       ,      I  I        I  I     ,       I
            //         I     ,      I   I    I   I     ,     I
            //            I   ,      I    V7    I     ,   I
            //               I  ,     I   I   I    , '  I
            //                  I ' - ,I_ I _I,  '   I
            //                     I    I I I    I
            //                         I  I  I
            //                           V6
            // (V는 Vertex와 그 번호, I는 Index 순서대로 그린 선)
            //
            // 위의 모양은 반투명 Vignette와 불투명 Vignette 모두 동일하다
            // 단, 반투명 Vignette가 불투명 Vignette의 가장 안쪽 사각형(실제로는 원)에 맞춰지도록
            // CalculateVignetteScaleAndOffset에서 계산하여 Shader에 전달하므로,
            // 반투명 Vignette와 불투명 Vignette가 구분되어 Vignette를 그릴 수 있다
        }

        if (_opaqueMesh != null)
            DestroyImmediate(_opaqueMesh);
        if (_transparentMesh != null)
            DestroyImmediate(_transparentMesh);

        _opaqueMesh = new Mesh()
        {
            name = "Opaque Vignette Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };
        _transparentMesh = new Mesh()
        {
            name = "Transparent Vignette Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };

        _opaqueMesh.vertices = outerVerts;
        _opaqueMesh.uv = outerUVs;
        _opaqueMesh.triangles = tris;
        _opaqueMesh.UploadMeshData(true);
        _opaqueMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        _opaqueMeshFilter.sharedMesh = _opaqueMesh;

        _transparentMesh.vertices = innerVerts;
        _transparentMesh.uv = innerUVs;
        _transparentMesh.triangles = tris;
        _transparentMesh.UploadMeshData(true);
        _transparentMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        _transparentMeshFilter.sharedMesh = _transparentMesh;
    }

    /// <summary>
    /// Vignette용 머티리얼 생성 및 설정
    /// </summary>
    private void BuildMaterials()
    {
#if UNITY_EDITOR
        _InitialFalloff = falloff;
#endif

        if (vignetteShader == null)
            vignetteShader = Shader.Find("App/Vignette");

        if (vignetteShader == null)
        {
            Debug.LogError("Could not find Vignette Shader! Vignette will not be drawn!");
            return;
        }

        if (_opaqueMaterial == null)
        {
            _opaqueMaterial = new Material(vignetteShader)
            {
                name = "Opaque Vignette Material",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = (int)RenderQueue.Background
            };
            _opaqueMaterial.SetFloat("_BlendSrc", (float)BlendMode.One);
            _opaqueMaterial.SetFloat("_BlendDst", (float)BlendMode.Zero);
            _opaqueMaterial.SetFloat("_ZWrite", 1);
        }
        _opaqueMeshRenderer.sharedMaterial = _opaqueMaterial;

        if (_transparentMaterial == null)
        {
            _transparentMaterial = new Material(vignetteShader)
            {
                name = "Transparent Vignette Material",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = (int)RenderQueue.Overlay
            };
            _transparentMaterial.SetFloat("_BlendSrc", (float)BlendMode.SrcAlpha);
            _transparentMaterial.SetFloat("_BlendDst", (float)BlendMode.OneMinusSrcAlpha);
            _transparentMaterial.SetFloat("_ZWrite", 0);
        }

        if (falloff == FalloffType.Quadratic)
            _transparentMaterial.EnableKeyword(QUADRATIC_FALLOFF);
        else
            _transparentMaterial.DisableKeyword(QUADRATIC_FALLOFF);

        _transparentMeshRenderer.sharedMaterial = _transparentMaterial;
    }

    /// <summary>
    /// 계산된 스케일·오프셋을 바탕으로 쉐이더 프로퍼티 설정
    /// </summary>
    private void SetVignetteMaterial()
    {
        // CONSTANT 모드일 때와 일반 모드 처리 분리
        float globalVignette;

        if (forceVignetteMode == ForceVignetteMode.CONSTANT)
        {
            // forceVignetteValue를 0~120 범위에서 0~1 범위로 정규화
            // 값이 클수록 vignette가 더 커져야 하므로 직접 사용
            globalVignette = Mathf.Clamp01(forceVignetteValue / 120f);
        }
        else
        {
            // 일반 모드에서는 기존 방식 사용
            globalVignette = 1f - _rate;
        }

        for (var i = 0; i < 2; i++)
        {
            var vignette = globalVignette;

            if (forceVignetteMode != ForceVignetteMode.CONSTANT)
            {
                // 모션 강도를 비네팅 크기에 반영 (CONSTANT 모드가 아닐 때만)
                vignette = Mathf.Lerp(vignette, vignette * (1f + _currentMotionStrength), 0.5f);
                vignette = Remap(vignette, 0f, 1f, 1f, _maxVignetteRange[i]);
            }
            else
            {
                // CONSTANT 모드에서는 직접 범위 매핑
                //vignette = Remap(vignette, 0f, 1f, 0f, _maxVignetteRange[i]);
                vignette = 1.0f;
            }

            var innerScaleX = _innerScaleX[i] * vignette;
            var innerScaleY = _innerScaleY[i] * vignette;
            var middleScaleX = _middleScaleX[i] * vignette;
            var middleScaleY = _middleScaleY[i] * vignette;
            var outerScaleX = _outerScaleX[i];
            var outerScaleY = _outerScaleY[i];
            var offsetX = _offsetX[i];
            var offsetY = _offsetY[i];

            _OpaqueScaleAndOffset0[i] = new Vector4(outerScaleX, outerScaleY, offsetX, offsetY);
            _OpaqueScaleAndOffset1[i] = new Vector4(middleScaleX, middleScaleY, offsetX, offsetY);

            middleScaleX *= middleOffset;
            middleScaleY *= middleOffset;
            _TransparentScaleAndOffset0[i] = new Vector4(middleScaleX, middleScaleY, offsetX, offsetY);
            _TransparentScaleAndOffset1[i] = new Vector4(innerScaleX, innerScaleY, offsetX, offsetY);

            _transparentVignetteVisible |= VisibilityTest(_innerScaleX[i], _innerScaleY[i], _offsetX[i], _offsetY[i]);
            _opaqueVignetteVisible |= VisibilityTest(_middleScaleX[i], _middleScaleY[i], _offsetX[i], _offsetY[i]);
        }

        // 동적 Alpha 값을 적용한 Color 생성
        Color dynamicVignetteColor = new Color(
            vignetteColor.r,
            vignetteColor.g,
            vignetteColor.b,
            ovrpassthroughlayer.textureOpacity  // 동적으로 계산된 Alpha 값 사용
        );

        Debug.Log("Dynamic Vignette Color : " + dynamicVignetteColor);

        _opaqueMaterial.SetVectorArray(_shaderScaleAndOffset0Property, _OpaqueScaleAndOffset0);
        _opaqueMaterial.SetVectorArray(_shaderScaleAndOffset1Property, _OpaqueScaleAndOffset1);
        //_opaqueMaterial.color = vignetteColor;
        _opaqueMaterial.color = dynamicVignetteColor;  // 동적 Color 적용

        _transparentMaterial.SetVectorArray(_shaderScaleAndOffset0Property, _TransparentScaleAndOffset0);
        _transparentMaterial.SetVectorArray(_shaderScaleAndOffset1Property, _TransparentScaleAndOffset1);
        //_transparentMaterial.color = vignetteColor;
        _transparentMaterial.color = dynamicVignetteColor;  // 동적 Color 적용

        // 쉐이더에 모션 강도 전달
        _opaqueMaterial.SetFloat("_MotionStrength", _currentMotionStrength);
        _transparentMaterial.SetFloat("_MotionStrength", _currentMotionStrength);
    }

    /// <summary>
    /// 외부에서 Vignette를 페이드인 시킴
    /// </summary>
    public void VignetteIn()
    {
        if (_rate >= 1f)
        {
            if (!_isVignetteStart)
                _isVignetteStart = true;
            return;
        }

        _targetRate = 1f;
        _isAnimating = true;
        if (_isVignetteStart)
        {
            CalculateVignetteScaleAndOffset();
            _isVignetteStart = false;
        }
    }

    /// <summary>
    /// 외부에서 Vignette를 페이드아웃 시킴
    /// </summary>
    public void VignetteOut()
    {
        if (_rate <= 0f)
        {
            if (!_isVignetteStart)
                _isVignetteStart = true;
            return;
        }

        _targetRate = 0f;
        _isAnimating = true;
        if (_isVignetteStart)
        {
            CalculateVignetteScaleAndOffset();
            _isVignetteStart = false;
        }
    }

    /// <summary>
    /// 스테레오/모노 카메라용 tan(Fov) 및 오프셋 계산
    /// </summary>
    private void GetTanFovAndOffsetForStereoEye(Camera.StereoscopicEye eye, out float tanFovX, out float tanFovY, out float offsetX, out float offsetY)
    {
        var pt = _camera.GetStereoProjectionMatrix(eye).transpose;
        var right = pt * new Vector4(-1, 0, 0, 1);
        var left = pt * new Vector4(1, 0, 0, 1);
        var up = pt * new Vector4(0, -1, 0, 1);
        var down = pt * new Vector4(0, 1, 0, 1);

        var rightTanFovX = right.z / right.x;
        var leftTanFovX = left.z / left.x;
        var upTanFovY = up.z / up.y;
        var downTanFovY = down.z / down.y;

        offsetX = -(rightTanFovX + leftTanFovX) / 2;
        offsetY = -(upTanFovY + downTanFovY) / 2;
        tanFovX = (rightTanFovX - leftTanFovX) / 2;
        tanFovY = (upTanFovY - downTanFovY) / 2;
    }

    /// <summary>
    /// 모노 카메라용 tan(Fov) 및 오프셋 계산
    /// </summary>
    private void GetTanFovAndOffsetForMonoEye(out float tanFovX, out float tanFovY, out float offsetX, out float offsetY)
    {
        tanFovY = Mathf.Tan(Mathf.Deg2Rad * _camera.fieldOfView * 0.5f);
        tanFovX = tanFovY * _camera.aspect;
        offsetX = 0f;
        offsetY = 0f;
    }

    /// <summary>
    /// 스케일·오프셋이 화면 밖으로 나가는지 테스트
    /// </summary>
    private static bool VisibilityTest(float scaleX, float scaleY, float offsetX, float offsetY)
    {
        return new Vector2((1 + Mathf.Abs(offsetX)) / scaleX, (1 + Mathf.Abs(offsetY)) / scaleY).sqrMagnitude > 1.0f;
    }

    /// <summary>
    /// 값 재매핑 (in→out)
    /// </summary>
    private static float Remap(float val, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + (val - inMin) * (outMax - outMin) / (inMax - inMin);
    }

    /// <summary>
    /// 렌더러 On/Off
    /// </summary>
    private void EnableRenderers()
    {
        _opaqueMeshRenderer.enabled = _opaqueVignetteVisible;
        _transparentMeshRenderer.enabled = _transparentVignetteVisible;
    }

    private void DisableRenderers()
    {
        _opaqueMeshRenderer.enabled = false;
        _transparentMeshRenderer.enabled = false;
    }

    /// <summary>
    /// 렌더링 파이프라인 이벤트 핸들러
    /// </summary>
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == _camera)
            EnableRenderers();
        else
            DisableRenderers();
    }

    /// <summary>
    /// Vignette 영역 스케일·오프셋 계산 (눈별 전처리)
    /// </summary>
    private void CalculateVignetteScaleAndOffset()
    {
        if (_camera == null) return; // 카메라 Null 체크

        // Inner/Middle FoV의 tan 값 계산
        // vignetteFieldOfView 대신 _currentVignetteFieldOfView 사용
        var tanInnerFovY = Mathf.Tan(_currentVignetteFieldOfView * Mathf.Deg2Rad * 0.5f);
        var tanInnerFovX = tanInnerFovY * vignetteAspectRatio;
        // vignetteFieldOfView 대신 _currentVignetteFieldOfView 사용
        //var tanMiddleFovX = Mathf.Tan((_currentVignetteFieldOfView + vignetteFalloffDegrees) * Mathf.Deg2Rad * 0.5f);
        //var tanMiddleFovY = tanMiddleFovX * vignetteAspectRatio;

        var tanMiddleFovY = Mathf.Tan((_currentVignetteFieldOfView + vignetteFalloffDegrees) * Mathf.Deg2Rad * 0.5f);
        var tanMiddleFovX = tanMiddleFovY * vignetteAspectRatio;

        for (int i = 0; i < 2; i++)
        {
            // 눈별 뷰포트 비율/오프셋 구하기
            float tanFovX, tanFovY;
            if (_camera.stereoEnabled)
                GetTanFovAndOffsetForStereoEye((Camera.StereoscopicEye)i, out tanFovX, out tanFovY, out _offsetX[i], out _offsetY[i]);
            else
                GetTanFovAndOffsetForMonoEye(out tanFovX, out tanFovY, out _offsetX[i], out _offsetY[i]);

            // tanFovX, tanFovY가 0이 되는 경우 방지 (매우 작은 값으로 클램핑)
            if (Mathf.Approximately(tanFovX, 0)) tanFovX = 0.0001f;
            if (Mathf.Approximately(tanFovY, 0)) tanFovY = 0.0001f;

            // 경계 스케일
            float borderScale = new Vector2((1 + Mathf.Abs(_offsetX[i])) / vignetteAspectRatio, 1 + Mathf.Abs(_offsetY[i])).magnitude * 1.01f;

            // 스케일 계산
            _innerScaleX[i] = tanInnerFovX / tanFovX;
            _innerScaleY[i] = tanInnerFovY / tanFovY;
            _middleScaleX[i] = tanMiddleFovX / tanFovX;
            _middleScaleY[i] = tanMiddleFovY / tanFovY; // 원본 코드에서는 tanMiddleFovX * vignetteAspectRatio / tanFovY 였으나, tanMiddleFovY를 직접 사용하도록 변경된 것으로 보임. 일관성을 위해 tanMiddleFovY 사용.
            _outerScaleX[i] = borderScale * vignetteAspectRatio;
            _outerScaleY[i] = borderScale;

            // 최대 커버리지 계산
            // _innerScaleX/Y가 0이 되는 경우 방지
            float safeInnerScaleX = Mathf.Max(Mathf.Abs(_innerScaleX[i]), 0.0001f);
            float safeInnerScaleY = Mathf.Max(Mathf.Abs(_innerScaleY[i]), 0.0001f);

            _maxVignetteRange[i] = new Vector2((1 + Mathf.Abs(_offsetX[i])) / safeInnerScaleX, (1 + Mathf.Abs(_offsetY[i])) / safeInnerScaleY).magnitude;
        }
    }

    /// <summary>
    /// 외부 호출용: 파라미터 변경 후 즉시 업데이트
    /// </summary>
    public void Refresh()
    {
        CalculateVignetteScaleAndOffset();
        SetVignetteMaterial();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_camera == null)
            return;
        BuildMaterials();
        Refresh();
    }
#endif
}