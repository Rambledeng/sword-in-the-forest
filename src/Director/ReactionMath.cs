using System;

namespace SwordInTheForest.Director
{
    /// <summary>Normal CDF and log-normal density used by the reaction model.</summary>
    public static class ReactionMath
    {
        private const double InvSqrt2Pi = 0.39894228040143267794;

        /// <summary>Standard normal CDF (Abramowitz–Stegun 7.1.26 erf, |error| &lt; 1.5e-7).</summary>
        public static double Phi(double z)
        {
            var x = Math.Abs(z) / 1.4142135623730951;
            var t = 1.0 / (1.0 + 0.3275911 * x);
            var y = 1.0 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Math.Exp(-x * x);
            return z >= 0 ? 0.5 * (1.0 + y) : 0.5 * (1.0 - y);
        }

        /// <summary>Density of a log-normal with log-median <paramref name="m"/> and log-spread <paramref name="s"/> at x &gt; 0.</summary>
        public static double LogNormalPdf(double x, double m, double s)
        {
            if (x <= 0) return 0;
            var z = (Math.Log(x) - m) / s;
            return InvSqrt2Pi / (s * x) * Math.Exp(-0.5 * z * z);
        }

        /// <summary>P(success | lead) = (1 - λ) Φ((ln(L - d) - m) / s).</summary>
        public static double SuccessProbability(double leadMs, double inputDelayMs, double m, double s, double lapse)
        {
            var x = leadMs - inputDelayMs;
            if (x <= 0) return 0;
            return (1 - lapse) * Phi((Math.Log(x) - m) / s);
        }
    }
}
