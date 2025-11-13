using TMPro;
using UnityEngine;
using System.Collections;

public class TextManager : MonoBehaviour
{
    public static TextManager instance { get; private set; }

    [Header("UiStateText")]
    public  TextMeshProUGUI UIText;

    private void Awake()
    {
        instance = this;
        UIText.gameObject.SetActive(false);
    }

    public static void ShowDebug(string t)
    {
        instance.showText(t);
    }

    public void showText(string t)
    {
        UIText.text = t;
        UIText.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HideText(2f));
    }

    private IEnumerator HideText(float delay)
    {
        yield return new WaitForSeconds(delay);
        UIText.gameObject.SetActive(false);
    }
}
