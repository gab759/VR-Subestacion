using UnityEngine;

namespace VRSubestacion.Simulation
{
    public static class LeverAngleMath
    {
        private const float MinAngle = 0.0001f;

        /// <summary>
        /// Ángulo con signo (grados, -180..180) de <paramref name="current"/> respecto a <paramref name="rest"/>
        /// alrededor de <paramref name="localAxis"/>, ambos en el mismo espacio local.
        /// </summary>
        public static float SignedAngle(Quaternion rest, Quaternion current, Vector3 localAxis)
        {
            Quaternion delta = Quaternion.Inverse(rest) * current;
            delta.ToAngleAxis(out float angle, out Vector3 axis);

            if (angle < MinAngle || localAxis.sqrMagnitude < MinAngle)
                return 0f;

            if (angle > 180f)
                angle -= 360f;

            return angle * Vector3.Dot(axis.normalized, localAxis.normalized);
        }

        /// <summary>
        /// Histéresis: pasa a ON al superar <paramref name="onThreshold"/> y vuelve a OFF bajo <paramref name="offThreshold"/>.
        /// </summary>
        public static bool EvaluateToggle(bool currentOn, float angle, float onThreshold, float offThreshold)
        {
            if (!currentOn && angle >= onThreshold)
                return true;

            if (currentOn && angle <= offThreshold)
                return false;

            return currentOn;
        }
    }
}
