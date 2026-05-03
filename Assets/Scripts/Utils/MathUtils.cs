using UnityEngine;

namespace Utils
{
    public abstract class MathUtils
    {
        public static Vector3 ClampVector3(
            Vector3 v,
            Vector3 min,
            Vector3 max)
        {
            return new Vector3(
                Mathf.Clamp(v.x, min.x, max.x),
                Mathf.Clamp(v.y, min.y, max.y),
                Mathf.Clamp(v.z, min.z, max.z)
            );
        }
    }
}
