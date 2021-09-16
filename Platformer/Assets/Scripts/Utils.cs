using System;
using System.Collections;

public static class Utils
{
    public static IEnumerator DoAfterAFrame(Action functionToCall)
    {
        yield return null;
        functionToCall.Invoke();
    }
}
