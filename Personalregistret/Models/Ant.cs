// Models/Ant.cs
namespace Personalregister.Models
{
    // INHERITANCE: Ant *är* en Employee
    public class Ant : Employee
    {
        public bool WorksNightShift { get; set; } // Specifikt för myror

        // Tom konstruktor för EF Core
        private Ant() : base("Default Ant") { }

        public Ant(string name, bool worksNightShift) : base(name)
        {
            WorksNightShift = worksNightShift;
        }

        public override double CalculateTaxRate()
        {
            // Läs skattesatser från konfiguration (AppConfig.Current)
            try
            {
                var cfg = AppConfig.Current;
                var key = WorksNightShift ? "NIGHT" : "DAY";
                if (cfg.TaxRates != null && cfg.TaxRates.TryGetValue(key, out var rate))
                {
                    return rate;
                }
            }
            catch
            {
                // fall tillbaka till hårdkodade värden
            }
            return WorksNightShift ? 0.45 : 0.30;
        }

        public override string GetDetails()
        {
            string status = IsActive() ? "Aktiv" : "Inaktiv (avliden)"; //
            string shift = WorksNightShift ? "Nattskift" : "Dagskift";
            return $"[MYRA] ID: {Id}, Namn: {Name}, Skift: {shift}, Status: {status}, Skatt: {CalculateTaxRate() * 100}%";
        }
    }
}