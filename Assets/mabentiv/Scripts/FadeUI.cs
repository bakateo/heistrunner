using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeUI : MonoBehaviour
{

    [SerializeField] Animator animator;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] float endValue;
    [SerializeField] float duration = 1f;

    private void Update()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && !animator.IsInTransition(0)) StartCoroutine(FadeIn(canvasGroup, endValue, duration));
    }


    public void StartFadeOut()
    {
        StartCoroutine(FadeOut());
    }

    public IEnumerator FadeIn(CanvasGroup cg, float endValue, float duration)
        {
            float elapsedTime = 0;
            float startValue = canvasGroup.alpha;
            while(elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float newAlpha = Mathf.Lerp(startValue, endValue, elapsedTime / duration);
                canvasGroup.alpha = newAlpha;
                yield return null;
            }
    }

    public IEnumerator FadeOut()
    {
        float eV = 0f;
        while (canvasGroup.alpha > eV)
        {
            float newAlpha = Time.deltaTime / 2;
            canvasGroup.alpha = newAlpha;
            yield return null;
        }
    }
}
