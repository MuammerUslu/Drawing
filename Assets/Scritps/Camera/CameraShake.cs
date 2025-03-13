using UnityEngine;
using System.Collections;
using System;


namespace Drawing
{
    public class CameraShake : MonoBehaviour
    {
        private Vector3 _originalPosition;
        private Coroutine _shakeCoroutine;

        public float shakeMagnitude = 0.1f;
        public float shakeDuration = 0.5f;

        private void OnEnable()
        {
            _originalPosition = transform.localPosition;
            Constants.FailedTry += TriggerShake;
        }

        private void OnDisable()
        {
            Constants.FailedTry -= TriggerShake;
        }

        private void TriggerShake()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }

            _shakeCoroutine = StartCoroutine(ShakeCamera(shakeDuration, shakeMagnitude));
        }

        private IEnumerator ShakeCamera(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                transform.localPosition = _originalPosition + UnityEngine.Random.insideUnitSphere * magnitude;
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = _originalPosition;
        }
    }
}