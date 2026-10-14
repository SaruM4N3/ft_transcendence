using System.Collections;
using TMPro;
using UnityEngine;

// One-shot floating damage number: rises and fades, then destroys itself.
public class DamageNumberPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float riseDistance = 0.8f;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float randomXSpread = 0.3f;

    public void Show(float amount)
    {
        label.text = Mathf.RoundToInt(amount).ToString();
        transform.position += new Vector3(Random.Range(-randomXSpread, randomXSpread), 0f, 0f);
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector3 start = transform.position;
        Vector3 end = start + new Vector3(0f, riseDistance, 0f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            transform.position = Vector3.Lerp(start, end, p);
            canvasGroup.alpha = 1f - p;
            yield return null;
        }
        Destroy(gameObject);
    }
}
