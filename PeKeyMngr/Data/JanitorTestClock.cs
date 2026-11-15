using Microsoft.Extensions.Hosting;

namespace PeKeyMngr.Data
{
    /// <summary>
    /// Csak fejlesztői környezetben működő, scoped tesztóra a portásoldalhoz.
    /// Nem írja át a rendszeridőt, nem befolyásolja a foglalási üzleti logikát.
    /// </summary>
    public sealed class JanitorTestClock(IHostEnvironment environment)
    {
        private DateTime? overriddenLocalTime;

        // Éles környezetben soha nem állítható át.
        public bool IsAvailable => environment.IsDevelopment();

        // A szimulált pillanat nem ketyeg; DateTimeKind.Unspecified,
        // összhangban a Sanyi által mentett helyi foglalási időpontokkal.
        public DateTime Now => IsAvailable && overriddenLocalTime.HasValue
            ? overriddenLocalTime.Value
            : DateTime.Now;

        public bool IsOverridden => IsAvailable && overriddenLocalTime.HasValue;

        public bool TrySet(DateTime localDateTime)
        {
            if (!IsAvailable || localDateTime == default)
            {
                return false;
            }

            overriddenLocalTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
            return true;
        }

        public void Reset() => overriddenLocalTime = null;
    }
}
