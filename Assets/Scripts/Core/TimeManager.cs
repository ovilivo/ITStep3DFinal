using System.Collections;
using UnityEngine;

/// <summary>
/// Controls time scale globally. Use for slow-motion abilities and pausing.
/// Access via TimeManager.Instance.
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetTimeScale(float scale) => Time.timeScale = scale;

    public void ResetTimeScale() => Time.timeScale = 1f;

    /// <summary>Slow time to <paramref name="scale"/> for <paramref name="duration"/> real seconds, then restore.</summary>
    public void SlowTime(float scale, float duration) => StartCoroutine(SlowTimeCoroutine(scale, duration));

    private IEnumerator SlowTimeCoroutine(float scale, float duration)
    {
        SetTimeScale(scale);
        yield return new WaitForSecondsRealtime(duration);
        ResetTimeScale();
    }
}
