using UnityEngine;

namespace LivingNorsemen.Home
{
    public static class HomeValidator
    {
        public static HomeValidationResult Validate(LivingNorseman villager)
        {
            HomeValidationResult result = new HomeValidationResult();

            result.HasHome = villager.HasHome;
            result.HasBed = villager.AssignedBed != null;

            // We'll implement these next
            result.HasRoof = IsSheltered(villager);
            result.HasFire = HasFire(villager);

            return result;
        }

        private static bool IsSheltered(LivingNorseman villager)
        {
            float cover;
            bool underRoof;

            Cover.GetCoverForPoint(
                villager.transform.position,
                out cover,
                out underRoof,
                0.5f
            );

            return underRoof && cover >= 0.8f;
        }

        private static bool HasFire(LivingNorseman villager)
        {
            if (!villager.HasHome)
            {
                return false;
            }

            return EffectArea.IsPointInsideArea(
                villager.HomePosition,
                EffectArea.Type.Burning,
                5f
            ) != null;
        }
    }
}
