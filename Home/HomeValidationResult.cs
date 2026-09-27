namespace LivingNorsemen.Home
{
    public class HomeValidationResult
    {
        public bool HasHome;
        public bool HasBed;
        public bool HasRoof;
        public bool HasFire;

        public bool IsValid
        {
            get
            {
                return HasHome &&
                       HasBed &&
                       HasRoof &&
                       HasFire;
            }
        }
    }
}
