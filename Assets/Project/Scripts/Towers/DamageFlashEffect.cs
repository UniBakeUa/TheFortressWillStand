using System;
using System.Collections;
using UnityEngine;

namespace Towers
{
    public static class DamageFlashEffect
    {
        /// <summary>
        /// Starts the flash+shake effect, restarting it if one is already running/cooling down so every hit stays visible.
        /// </summary>
        public static Coroutine Play(
            MonoBehaviour host,
            IDamageFlashTarget target,
            Coroutine currentRoutine,
            Color flashColor,
            float durationSeconds,
            float cooldownSeconds,
            float shakeOffset,
            Action onComplete)
        {
            if (currentRoutine != null)
            {
                host.StopCoroutine(currentRoutine);
                return host.StartCoroutine(RoutineOverride(target, flashColor, durationSeconds, cooldownSeconds, shakeOffset, onComplete));
            }
            else
            {

                return host.StartCoroutine(Routine(target, flashColor, durationSeconds, cooldownSeconds, shakeOffset, onComplete));
            }
        }

        private static IEnumerator Routine(
            IDamageFlashTarget target,
            Color flashColor,
            float durationSeconds,
            float cooldownSeconds,
            float shakeOffset,
            Action onComplete)
        {
            target.SetFlashColor(flashColor);
            target.Shake(UnityEngine.Random.insideUnitCircle * shakeOffset);

            yield return new WaitForSeconds(durationSeconds);

            target.ResetPosition();
            target.ResetColor();

            yield return new WaitForSeconds(cooldownSeconds);

            onComplete?.Invoke();
        }

        private static IEnumerator RoutineOverride(
            IDamageFlashTarget target,
            Color flashColor,
            float durationSeconds,
            float cooldownSeconds,
            float shakeOffset,
            Action onComplete)
        {
            target.ResetPosition();
            target.ResetColor();

            yield return new WaitForSeconds(durationSeconds/3f);

            target.SetFlashColor(flashColor);
            target.Shake(UnityEngine.Random.insideUnitCircle * shakeOffset);

            yield return new WaitForSeconds(durationSeconds);

            target.ResetPosition();
            target.ResetColor();

            yield return new WaitForSeconds(cooldownSeconds);

            onComplete?.Invoke();
        }
    }
}
