using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Health target;
    [SerializeField] private Image fillImage;
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private Color lowColor = Color.red;
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private float lowThreshold = 0.3f;

    private float displayedFill = 1f;

    private void Start()
    {
        if (target == null)
            target = GetComponentInParent<Health>();
    }

    private void LateUpdate()
    {
        if (target == null || fillImage == null) return;

        float ratio = target.MaxHealth > 0f
            ? target.CurrentHealth / target.MaxHealth
            : 0f;

        displayedFill = Mathf.Lerp(displayedFill, ratio, Time.deltaTime * smoothSpeed);
        fillImage.fillAmount = displayedFill;
        fillImage.color = Color.Lerp(lowColor, fullColor, displayedFill);
    }
}
