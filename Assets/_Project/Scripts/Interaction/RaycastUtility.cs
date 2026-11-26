using UnityEngine;

namespace CircuitLab.Interaction
{
    /// <summary>GC'siz raycast yordamchisi (oldindan ajratilgan bufer bilan).</summary>
    public static class RaycastUtility
    {
        /// <summary>
        /// <see cref="Physics.RaycastNonAlloc(Ray, RaycastHit[], float, int, QueryTriggerInteraction)"/>
        /// natijalari ichidan eng yaqinini qaytaradi (NonAlloc natijalari tartiblanmagan).
        /// </summary>
        public static bool TryGetClosestHit(Ray ray, RaycastHit[] buffer, float maxDistance, LayerMask mask, out RaycastHit closest)
        {
            int count = Physics.RaycastNonAlloc(ray, buffer, maxDistance, mask, QueryTriggerInteraction.Collide);
            closest = default;
            if (count == 0) return false;

            int best = 0;
            float bestDistance = buffer[0].distance;
            for (int i = 1; i < count; i++)
            {
                if (buffer[i].distance < bestDistance)
                {
                    bestDistance = buffer[i].distance;
                    best = i;
                }
            }
            closest = buffer[best];
            return true;
        }
    }
}
