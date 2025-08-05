using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIFadeInOutAnimation : MonoBehaviour
{
    public GraphicRaycaster graphicRaycaster;
    public Image animationImage;
    public float animationSpeed = 1.0f;
    Color baseColor;

    WaitForSeconds waitForSeconds = new WaitForSeconds(0.1f);
    WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

    void Awake()
    {
        Init();
    }

    void Init()
    {
        graphicRaycaster.enabled = true;
        animationImage.enabled = false;
        baseColor = animationImage.color;
    }

    public IEnumerator FadeIn()
    {
        float alphaValue = 1.0f;
        graphicRaycaster.enabled = false;
        animationImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alphaValue);
        animationImage.enabled = true;

        yield return waitForSeconds;

        while (alphaValue > 0)
        {
            yield return waitForEndOfFrame;
            alphaValue -= Time.deltaTime * animationSpeed;
            animationImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alphaValue);
        }
        graphicRaycaster.enabled = true;
        animationImage.enabled = false;
    }

    public IEnumerator FadeOut()
    {
        float alphaValue = 0.0f;
        graphicRaycaster.enabled = true;
        animationImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alphaValue);
        animationImage.enabled = true;

        yield return waitForSeconds;

        while (1.0f > alphaValue)
        {
            yield return waitForEndOfFrame;
            alphaValue += Time.deltaTime * animationSpeed;
            animationImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alphaValue);
        }
    }
}