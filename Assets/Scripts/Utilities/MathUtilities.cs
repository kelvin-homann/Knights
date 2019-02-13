using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtilities
{
    public static double[] SolveQuadratic(double a, double b, double c)
    {
        double x1 = 0.0, x2 = 0.0;
        double d = b * b - 4.0 * a * c;

        // two solutions
        if(d > 0)
        {
            double sqrt_d = System.Math.Sqrt(d);
            x1 = (-b + sqrt_d) / (2.0 * a);
            x2 = (-b - sqrt_d) / (2.0 * a);
            double[] result = { x1, x2 };
            return result;
        }
        // one solution
        else if(d == 0)
        {
            x1 = -b / (2.0 * a);
            double[] result = { x1 };
            return result;
        }
        // imaginary solution (not implemented yet or here)
        else
            return null;
    }
}
