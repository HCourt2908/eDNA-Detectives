using System;
using UnityEngine;

namespace EDNA.Investigation
{
    // Animation lifetime belongs to the rendered scene. Its clock belongs to the
    // presenter, so resizing/rebuilding the UI resumes rather than restarts a run.
    public sealed class InvestigationScenarioPlayback : MonoBehaviour
    {
        private double started;
        private float duration;
        private Action<float> sample;
        private Action complete;
        private bool finished;
        private Func<double> clock;
        public void Configure(double startTime, float seconds, Action<float> frame, Action done, Func<double> currentTime = null)
        {
            clock = currentTime; started = startTime; duration = Mathf.Max(.01f, seconds); sample = frame; complete = done;
            sample?.Invoke(Mathf.Clamp01((float)((clock == null ? Time.unscaledTimeAsDouble : clock()) - started) / duration));
        }
        private void Update()
        {
            if (finished || sample == null) return;
            float t = Mathf.Clamp01((float)((clock == null ? Time.unscaledTimeAsDouble : clock()) - started) / duration);
            sample(t);
            if (t < 1f) return;
            finished = true; complete?.Invoke();
        }
    }
}
