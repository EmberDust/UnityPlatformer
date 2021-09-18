using System;
using System.Collections;
using UnityEngine;

public static class Utils
{
    public static IEnumerator DoAfterAFrame(Action functionToCall)
    {
        yield return null;
        functionToCall.Invoke();
    }

    public static IEnumerator DoAfterDelay(Action functionToCall, float delay)
    {
        yield return new WaitForSeconds(delay);
        functionToCall.Invoke();
    }

    public static void EmitParticleBurstAtPosition(ParticleSystem particleSystem, Vector2 position, bool overrideBurstSize = false, int burstSize = 30)
    {
        // If particle system has bursts - take the first burst values
        // Yes, this is a crutch
        if (particleSystem.emission.burstCount > 0 && !overrideBurstSize)
        {
            burstSize = (int)particleSystem.emission.GetBurst(0).count.constantMax;
        }

        var emitParams = new ParticleSystem.EmitParams();
        emitParams.position = position;

        particleSystem.Emit(emitParams, burstSize);
    }
}
