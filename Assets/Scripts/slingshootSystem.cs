using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Unity.Cinemachine;

public class slingshootSystem : MonoBehaviour
{
    [Header("Bow")]
    public Transform bowModel;
    private Vector3 bowOriginalPos, bowOriginalRot;
    public Transform bowZoomTransform;

    [Header("Arrow")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnOrigin;
    public Transform arrowModel;
    private Vector3 arrowOriginalPos;

    [Header("Parameters")]
    public Vector3 arrowImpulse;
    public float timeToShoot;
    public float shootWait;
    public bool canShoot;
    public bool shootRest = false;
    public bool isAiming = false;

    [Header("Hold to Shoot")]
    public float holdTimeToShoot = 1.0f;
    public float miniShotHold = 0.5f;
    private float mouseDownTime;
    private bool isHolding = false;
    private bool isInMiniHold = false;

    [Header("Zoom")]
    public float zoomInDuration;
    public float zoomOutDuration;
    private float camOriginalFov;
    public float camZoomFov;
    private Vector3 camOriginalPos;
    public Vector3 camZoomOffset;

    [Header("Particles")]
    public ParticleSystem[] prepareParticles;
    public ParticleSystem[] aimParticles;
    public GameObject circleParticlePrefab;

    [Header("Canvas")]
    public RectTransform reticle;
    public CanvasGroup reticleCanvas;
    public Image centerCircle;
    private Vector2 originalImage;

    [Header("Fast Shot Settings")]
    public float fastShotThreshold = 0.2f;
    private bool pendingFastShot = false;
    private bool readyToShoot = false;

    [Header("Cinemachine Camera Shake")]
    public CinemachineCamera cinemachineCamera;
    private CinemachineBasicMultiChannelPerlin camNoise;
    public float maxShakeAmplitude = 2f;
    public float maxShakeFrequency = 2f;
    public float shakeBuildUpSpeed = 1.5f;
    private float currentShake = 0f;
    private bool isShaking = false;

    PlayerSunlightEnergy sunenergy;


    private void Start()
    {
        camOriginalPos = Camera.main.transform.localPosition;
        camOriginalFov = Camera.main.fieldOfView;
        bowOriginalPos = bowModel.localPosition;
        bowOriginalRot = bowModel.localEulerAngles;
        arrowOriginalPos = arrowModel.localPosition;

        if (cinemachineCamera != null)
        {
            camNoise = cinemachineCamera.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
        }

        originalImage = reticle.sizeDelta;
        ShowReticle(false, 0);

        sunenergy = GetComponent<PlayerSunlightEnergy>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && sunenergy.currentSunlight > 0)
        {
            mouseDownTime = Time.time;
            pendingFastShot = true;
        }

        if (Input.GetKey(KeyCode.Mouse0) && sunenergy.currentSunlight > 0)
        {
            float holdDuration = Time.time - mouseDownTime;

            if (holdDuration > fastShotThreshold && !isAiming && !shootRest)
            {
                StartAiming();
            }

            if (isAiming && isHolding && holdDuration >= holdTimeToShoot && !isInMiniHold)
            {
                isHolding = false;
                StartCoroutine(MiniShotHoldSequence());
            }
        }

        if (Input.GetKeyUp(KeyCode.Mouse0) && sunenergy.currentSunlight > 0)
        {
            float holdDuration = Time.time - mouseDownTime;

            if (pendingFastShot && holdDuration <= fastShotThreshold && !shootRest)
            {
                StartCoroutine(FastShot());
            }
            else if (readyToShoot)
            {
                readyToShoot = false;
                StopCoroutine(ShootSequence());
                StartCoroutine(ShootSequence());
            }
            else if (isHolding || isInMiniHold)
            {
                CancelShot();
            }

            pendingFastShot = false;
        }

        if (isShaking && camNoise != null)
        {
            currentShake += Time.deltaTime * shakeBuildUpSpeed;
            camNoise.AmplitudeGain = Mathf.Min(currentShake, maxShakeAmplitude);
            camNoise.FrequencyGain = Mathf.Min(currentShake * 1.5f, maxShakeFrequency);
        }

    }

    private void StartAiming()
    {
        canShoot = false;
        isAiming = true;
        isHolding = true;
        isInMiniHold = false;

        StopAllCoroutines();
        StartCoroutine(PrepareSequence());

        ShowReticle(true, zoomInDuration / 2);

        arrowModel.localPosition = arrowOriginalPos;
        arrowModel.DOLocalMoveZ(arrowModel.localPosition.z - 0.1f, zoomInDuration * 2f);

        CameraZoom(camZoomFov, camOriginalPos + camZoomOffset, bowZoomTransform.localPosition, bowZoomTransform.localEulerAngles, zoomInDuration, true);

        StartChargingShake();
    }

    private void CancelShot()
    {
        isHolding = false;
        isAiming = false;
        isInMiniHold = false;
        canShoot = false;
        readyToShoot = false;
        pendingFastShot = false;

        StopAllCoroutines();
        ShowReticle(false, zoomOutDuration);
        CameraZoom(camOriginalFov, camOriginalPos, bowOriginalPos, bowOriginalRot, zoomOutDuration, false);
        arrowModel.localPosition = arrowOriginalPos;

        ResetShake();
    }

    private IEnumerator MiniShotHoldSequence()
    {
        isInMiniHold = true;
        yield return new WaitForSeconds(miniShotHold);

        if (Input.GetKey(KeyCode.Mouse0))
        {
            isInMiniHold = false;
            readyToShoot = true;
        }
        else
        {
            CancelShot();
        }
    }

    public IEnumerator PrepareSequence()
    {
        foreach (ParticleSystem part in prepareParticles)
            part.Play();

        yield return new WaitForSeconds(timeToShoot);

        canShoot = true;

        foreach (ParticleSystem part in aimParticles)
            part.Play();
    }

    public IEnumerator ShootSequence()
    {
        yield return new WaitUntil(() => canShoot == true);

        shootRest = true;
        isAiming = false;
        isHolding = false;
        isInMiniHold = false;
        canShoot = false;

        ShowReticle(false, zoomOutDuration);
        CameraZoom(camOriginalFov, camOriginalPos, bowOriginalPos, bowOriginalRot, zoomOutDuration, true);
        arrowModel.localPosition = arrowOriginalPos;

        Instantiate(circleParticlePrefab, arrowSpawnOrigin.position, Quaternion.identity);
        GetComponent<PlayerSunlightEnergy>().UseSunlight(5);
        GameObject arrow = Instantiate(arrowPrefab, arrowSpawnOrigin.position, bowModel.rotation);
        arrow.GetComponent<Rigidbody>().AddForce(transform.forward * arrowImpulse.z + transform.up * arrowImpulse.y, ForceMode.Impulse);

        ShowArrow(false);

        yield return new WaitForSeconds(shootWait);
        shootRest = false;
        ResetShake();
    }

    private IEnumerator FastShot()
    {
        shootRest = true;

        Instantiate(circleParticlePrefab, arrowSpawnOrigin.position, Quaternion.identity);
        GetComponent<PlayerSunlightEnergy>().UseSunlight(3);

        GameObject arrow = Instantiate(arrowPrefab, arrowSpawnOrigin.position, bowModel.rotation);
        arrow.GetComponent<Rigidbody>().AddForce(transform.forward * arrowImpulse.z, ForceMode.Impulse);

        yield return new WaitForSeconds(shootWait);
        shootRest = false;
    }

    public void CameraZoom(float fov, Vector3 camPos, Vector3 bowPos, Vector3 bowRot, float duration, bool zoom)
    {
        Camera.main.transform.DOComplete();
        Camera.main.DOFieldOfView(fov, duration);
        Camera.main.transform.DOLocalMove(camPos, duration);
        bowModel.DOLocalRotate(bowRot, duration).SetEase(Ease.OutBack);
        bowModel.DOLocalMove(bowPos, duration).OnComplete(() => ShowArrow(zoom));
    }

    public void ShowArrow(bool state)
    {
        bowModel.GetChild(0).gameObject.SetActive(state);
    }

    public void ShowReticle(bool state, float duration)
    {
        float targetAlpha = state ? 1 : 0;
        reticleCanvas.DOFade(targetAlpha, duration);
        Vector2 size = state ? originalImage / 2 : originalImage;
        reticle.DOComplete();
        reticle.DOSizeDelta(size, duration * 4);

        if (state)
            centerCircle.DOFade(1, 0.5f).SetDelay(duration * 3);
        else
            centerCircle.DOFade(0, duration);
    }

    private void StartChargingShake()
    {
        isShaking = true;
        currentShake = 0f;
    }

    private void ResetShake()
    {
        isShaking = false;
        currentShake = 0f;

        if (camNoise != null)
        {
            camNoise.AmplitudeGain = 0f;
            camNoise.FrequencyGain = 0f;
        }
    }
}
