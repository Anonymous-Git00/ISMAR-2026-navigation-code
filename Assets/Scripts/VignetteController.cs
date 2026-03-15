using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class VignetteController : MonoBehaviour
{
    [SerializeField]
    Vignette vignette;
    [SerializeField]
    OVRPassthroughLayer OVRpassthroughlayer;
    [SerializeField]
    KATXRWalker katXRWalker;

    public bool isKATVR = false;


    private Coroutine increaseFOVCoroutine = null;
    private Coroutine decreaseFOVCoroutine = null;
    private Coroutine increaseOPRSCoroutine = null;
    private Coroutine decreaseOPRSCoroutine = null;

    public void Start()
    {
        if (isKATVR)
        {
            katXRWalker.enabled = true; 
        }
    }

    public void ToggleIncreaseFOV()
    {
        if (increaseFOVCoroutine == null)
        {
            if (decreaseFOVCoroutine != null) 
            {
                StopCoroutine(decreaseFOVCoroutine);
            }

            increaseFOVCoroutine = StartCoroutine(IncreaseFOV());
        } 
        else
        {
            StopCoroutine(increaseFOVCoroutine);
            increaseFOVCoroutine = null;
        }
    }

    public void ToggleDecreaseFOV()
    {
        if (decreaseFOVCoroutine == null) 
        {
            if (increaseFOVCoroutine != null)
            {
                StopCoroutine(increaseFOVCoroutine);
            }

            decreaseFOVCoroutine = StartCoroutine(DecreaseFOV());
        }
        else
        {
            StopCoroutine(decreaseFOVCoroutine);
            decreaseFOVCoroutine = null;
        }
    }

    public void ToggleIncreasePRV()
    {
        if (increaseOPRSCoroutine == null)
        {
            if (decreaseOPRSCoroutine != null)
            {
                StopCoroutine(decreaseOPRSCoroutine);
            }

            increaseOPRSCoroutine = StartCoroutine(IncreaseOPRS());
        }
        else
        {
            StopCoroutine(increaseOPRSCoroutine);
            increaseOPRSCoroutine = null;
        }
    }

    public void ToggleDecreasePRV()
    {
        if (decreaseOPRSCoroutine == null)
        {
            if (increaseOPRSCoroutine != null)
            {
                StopCoroutine(increaseOPRSCoroutine);
            }

            decreaseOPRSCoroutine = StartCoroutine(DecreaseOPRS());
        }
        else
        {
            StopCoroutine(decreaseOPRSCoroutine);
            decreaseOPRSCoroutine = null;
        }
    }


    public IEnumerator IncreaseFOV()
    {
        while (true)
        {
            vignette.forceVignetteValue += 2.5f;

            yield return new WaitForSeconds(1.0f);
        }
    }

    public IEnumerator DecreaseFOV()
    {
        while (true)
        {
            vignette.forceVignetteValue -= 2.5f;

            yield return new WaitForSeconds(1.0f);
        }
    }

    public IEnumerator IncreaseOPRS()
    {
        while (true)
        {
            //OVRpassthroughlayer.textureOpacity += 0.01f;
            vignette.forceAlphaValue += 0.01f;

            yield return new WaitForSeconds(1.0f);
        }
    }

    public IEnumerator DecreaseOPRS()
    {
        while (true)
        {
            //OVRpassthroughlayer.textureOpacity -= 0.01f;
            vignette.forceAlphaValue -= 0.01f;

            yield return new WaitForSeconds(1.0f);
        }
    }
}
