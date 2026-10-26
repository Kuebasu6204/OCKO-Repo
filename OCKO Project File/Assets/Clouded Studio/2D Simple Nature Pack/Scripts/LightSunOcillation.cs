using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CloudedStudio
{
    public class LightSunOcillation : MonoBehaviour
    {
        private Light2D light2D;

        [SerializeField] private float intensityUpdateSpeed = 0.5f;
        [SerializeField] private float minIntensity = 0.5f;
        [SerializeField] private float maxIntensity = 1f;

        private Vector2 initialPosition;

        [SerializeField] private float maxDisplacement = 1f;
        [SerializeField] private float displacementSpeed = 0.5f;
        [SerializeField] private float displacementAcceleration = 0.5f;
        [SerializeField] private float timeIntervalForChangeTargetDisplacement = 5f;

        private float targetIntensity;
        private Vector2 targetPosition;
        private float timeSinceLastDisplacementChange;
        private float currentDisplacementSpeed;

        private void Awake()
        {
            light2D = GetComponent<Light2D>();
            SetRandomTargetIntensity();
        }

        private void Start()
        {
            initialPosition = transform.position;
            SetRandomTargetPosition();
            currentDisplacementSpeed = 0f;
        }

        void Update()
        {
            // Update light intensity
            light2D.intensity = Mathf.MoveTowards(light2D.intensity, targetIntensity, intensityUpdateSpeed * Time.deltaTime);
            if (Mathf.Approximately(light2D.intensity, targetIntensity))
            {
                SetRandomTargetIntensity();
            }

            // Update position
            timeSinceLastDisplacementChange += Time.deltaTime;
            if (timeSinceLastDisplacementChange >= timeIntervalForChangeTargetDisplacement || (Vector2)transform.position == targetPosition)
            {
                SetRandomTargetPosition();
                timeSinceLastDisplacementChange = 0f;
                currentDisplacementSpeed = 0f; // Reset speed for smooth acceleration
            }

            // Smooth acceleration
            currentDisplacementSpeed = Mathf.MoveTowards(currentDisplacementSpeed, displacementSpeed, displacementAcceleration * Time.deltaTime);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, currentDisplacementSpeed * Time.deltaTime);
        }

        private void SetRandomTargetIntensity()
        {
            targetIntensity = Random.Range(minIntensity, maxIntensity);
        }

        private void SetRandomTargetPosition()
        {
            targetPosition = initialPosition + new Vector2(Random.Range(-maxDisplacement, maxDisplacement), 0);
        }
    }
}

