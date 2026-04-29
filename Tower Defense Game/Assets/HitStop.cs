using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    private static HitStop instance;
    private Coroutine routine;

    public static void Freeze(float duration)
    {
        if (instance == null)
        {
            GameObject obj = new GameObject("HitStop");
            DontDestroyOnLoad(obj);
            instance = obj.AddComponent<HitStop>();
        }
        // Don't extend an existing freeze — avoids stacking from rapid kills
        if (instance.routine == null)
            instance.routine = instance.StartCoroutine(instance.FreezeRoutine(duration));
    }

    IEnumerator FreezeRoutine(float duration)
    {
        float prev = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = prev;
        routine = null;
    }
}
