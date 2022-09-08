using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Sampler
{
    public static double IntergerRadicalInverse(int baseNumber, int i)
    {

        int inverse;
        int numPoints = 1;
        for(inverse = 0; i > 0; i /= baseNumber)
        {
            inverse = inverse * baseNumber + (i % baseNumber);
            numPoints = numPoints * baseNumber;
        }

        //flip the generated digis into right
        return inverse / (double)numPoints;

    }
    
    public static IEnumerable<Vector2> HaltonSequence(int baseX, int baseY)
    {
        for (int  n = 0; ; n++)
            yield return new Vector2((float)IntergerRadicalInverse(baseX, n), (float)IntergerRadicalInverse(baseY, n));
    }
}
