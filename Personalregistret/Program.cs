// Program.cs
using Personalregister.Data;
using Personalregister.Models;

namespace Personalregister
{
    class Program
    {
    // Vi använder Dependency Injection (från SOLID)
    // Programmet beror på en IEmployeeRepository, inte en specifik databas.
    private static IEmployeeRepository _repository = null!;
        static void Main(string[] args)
        {
            // Ladda konfiguration
            var cfg = AppConfig.Load("config.json");
            if (cfg.UseInMemory)
            {
                _repository = new Data.InMemoryEmployeeRepository(cfg);
            }
            else
            {
                _repository = new Data.EmployeeRepository("personal.db");
            }

            // Enkel konsoll-UI: visa header och meny på ett snyggt sätt.
            bool running = true;
            while (running)
            {
                var choice = ShowMenu();
                switch (choice)
                {
                    case "1":
                        AddEmployee();
                        break;
                    case "2":
                        SearchEmployee();
                        break;
                    case "3":
                        UpdateEmployee();
                        break;
                    case "4":
                        RemoveEmployee();
                        break;
                    case "5":
                        ListAllEmployees();
                        break;
                    case "6":
                        AddBee(); // Demonstrerar framtida utbyggnad
                        break;
                    case "8":
                        SaveSnapshot();
                        break;
                    case "9":
                        LoadSnapshot();
                        break;
                    case "10":
                        Benchmark();
                        break;
                    case "7":
                        running = false;
                        break;
                    default:
                        WriteError("Ogiltigt val. Försök igen.");
                        Pause();
                        break;
                }
            }
        }

        // --- UI Helpers ---
        private static void PrintHeader()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Personalregister för Arbetsmyror (och framtida Bin!)");
            Console.ResetColor();
            Console.WriteLine($"Register inläst. Senaste kända klockslag: {_repository.GetLastReadTime()}");
            Console.WriteLine(new string('-', 50));
        }

        private static string ShowMenu()
        {
            PrintHeader();
            Console.WriteLine();
            Console.WriteLine("1. Lägg till ny personal (Myra)");
            Console.WriteLine("2. Sök personal");
            Console.WriteLine("3. Uppdatera personal");
            Console.WriteLine("4. Ta bort personal");
            Console.WriteLine("5. Visa all personal");
            Console.WriteLine("6. (Framtid) Lägg till Arbetsbi");
            Console.WriteLine("8. Spara snapshot (JSONL)");
            Console.WriteLine("9. Läs snapshot (JSONL)");
            Console.WriteLine("10. Benchmark (generera testdata)");
            Console.WriteLine("7. Avsluta");
            Console.WriteLine();
            Console.Write("Val: ");
            return Console.ReadLine() ?? string.Empty;
        }

        private static void Pause(string message = "Tryck Enter för att fortsätta...")
        {
            Console.Write(message);
            Console.ReadLine();
        }

        private static bool Confirm(string prompt)
        {
            Console.Write(prompt + " ");
            var ans = (Console.ReadLine() ?? string.Empty).Trim().ToLower();
            return ans == "j" || ans == "y";
        }

        private static void WriteSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void AddEmployee()
        {
            try
            {
                Console.Write("Namn: ");
                string name = Console.ReadLine() ?? "";

                Console.Write("Arbetar nattskift (j/n): ");
                bool isNightShift = (Console.ReadLine() ?? string.Empty).Trim().ToLower() == "j";

                // Här skapar vi en Myra. Detta uppfyller kravet om olika skattesatser
                // och framtida expansion.
                Ant newAnt = new Ant(name, isNightShift);

                _repository.AddEmployee(newAnt);
                WriteSuccess($"Tillagd: {newAnt.Name} (ID: {newAnt.Id}).");
                Pause();
            }
            catch (Exception ex)
            {
                WriteError($"Fel: {ex.Message}");
                Pause();
            }
        }

        private static void SearchEmployee()
        {
            Console.Write("Ange namn eller ID att söka efter: ");
            string searchTerm = Console.ReadLine() ?? "";

            var employees = _repository.SearchEmployees(searchTerm);

            if (!employees.Any())
            {
                WriteError("Ingen personal hittades.");
                Pause();
                return;
            }

            Console.WriteLine("Hittade följande:");
            foreach (var emp in employees)
            {
                Console.WriteLine(emp.GetDetails());
            }
            Pause();
        }

        private static void UpdateEmployee()
        {
            Console.Write("Ange ID på personal att uppdatera: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                WriteError("Ogiltigt ID.");
                Pause();
                return;
            }

            var emp = _repository.GetEmployeeById(id);
            if (emp == null)
            {
                WriteError("Personal hittades inte.");
                Pause();
                return;
            }

            Console.WriteLine($"Uppdaterar: {emp.GetDetails()}");
            Console.Write($"Nytt namn (lämna tomt för att behålla '{emp.Name}'): ");
            string name = Console.ReadLine() ?? "";
            if (!string.IsNullOrEmpty(name))
            {
                emp.Name = name;
            }

            // Specifik logik för Myror
            if (emp is Ant ant)
            {
                Console.Write($"Arbetar nattskift (j/n) (nuvarande: {ant.WorksNightShift}): ");
                string nightShift = Console.ReadLine()?.ToLower() ?? "";
                if (nightShift == "j")
                {
                    ant.WorksNightShift = true;
                }
                else if (nightShift == "n")
                {
                    ant.WorksNightShift = false;
                }
            }

            _repository.UpdateEmployee(emp);
            WriteSuccess("Personal uppdaterad.");
            Console.WriteLine($"Nuvarande: {emp.GetDetails()}");
            Pause();
        }

        private static void RemoveEmployee()
        {
            Console.Write("Ange ID på personal att ta bort: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                WriteError("Ogiltigt ID.");
                Pause();
                return;
            }

            var emp = _repository.GetEmployeeById(id);
            if (emp == null)
            {
                WriteError("Personal hittades inte.");
                Pause();
                return;
            }

            Console.WriteLine($"Följande kommer att tas bort: {emp.GetDetails()}");
            if (!Confirm("Bekräfta borttagning? (j/n):"))
            {
                WriteError("Borttagning avbröts.");
                Pause();
                return;
            }

            _repository.DeleteEmployee(id);
            WriteSuccess("Personal borttagen.");
            Pause();
        }

        private static void ListAllEmployees()
        {
            // Vi hämtar även "döda" myror för att visa att vi hanterar livscykeln
            var employees = _repository.GetAllEmployees();

            if (!employees.Any())
            {
                WriteError("Registret är tomt.");
                Pause();
                return;
            }

            Console.WriteLine("\n--- All Personal ---");
            foreach (var emp in employees)
            {
                Console.WriteLine(emp.GetDetails());
            }
            Console.WriteLine("--- Slut på listan ---");
            Pause();
        }

        private static void AddBee()
        {
            Console.WriteLine("\n--- Framtida funktion: Lägg till Arbetsbi ---");
            Console.Write("Namn: ");
            string name = Console.ReadLine() ?? "";
            Console.Write("Antal vingar: ");
            int.TryParse(Console.ReadLine(), out int wings);

            Bee newBee = new Bee(name, wings);
            _repository.AddEmployee(newBee);
            WriteSuccess($"Tillagd: {newBee.Name} (ID: {newBee.Id}).");
            Console.WriteLine("Detta visar hur vi enkelt kan bygga ut systemet! (Polymorphism/Open-Closed Principle)");
            Pause();
        }

        private static void SaveSnapshot()
        {
            if (_repository is Data.InMemoryEmployeeRepository mem)
            {
                Console.Write("Skriv sökväg att spara snapshot till (enter för default 'snapshot.jsonl'): ");
                var path = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(path)) path = AppConfig.Current.SnapshotPath;
                mem.SaveSnapshot(path);
                WriteSuccess($"Snapshot sparad till {path}");
            }
            else
            {
                WriteError("Snapshot stöds endast för in-memory repository.");
            }
            Pause();
        }

        private static void LoadSnapshot()
        {
            if (_repository is Data.InMemoryEmployeeRepository mem)
            {
                Console.Write("Sökväg till snapshot att läsa (enter för default 'snapshot.jsonl'): ");
                var path = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(path)) path = AppConfig.Current.SnapshotPath;
                mem.LoadSnapshot(path);
                WriteSuccess($"Snapshot inläst från {path}");
            }
            else
            {
                WriteError("Snapshot stöds endast för in-memory repository.");
            }
            Pause();
        }

        private static void Benchmark()
        {
            if (!(_repository is Data.InMemoryEmployeeRepository mem))
            {
                WriteError("Benchmark körs endast för in-memory repository.");
                Pause();
                return;
            }

            Console.Write("Antal testmyror att skapa (t.ex. 500000): ");
            if (!int.TryParse(Console.ReadLine(), out int n) || n <= 0)
            {
                WriteError("Ogiltigt tal.");
                Pause();
                return;
            }

            Console.WriteLine($"Skapar {n} testmyror...");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < n; i++)
            {
                var a = new Ant($"TestMyra_{i}", i % 2 == 0);
                mem.AddEmployee(a);
            }
            sw.Stop();
            WriteSuccess($"Infogade {n} testmyror på {sw.Elapsed.TotalSeconds:F2}s");

            // Kör en sökningstest
            sw.Restart();
            // Sök efter testmyror som vi just skapade (namn börjar med "TestMyra_")
            var found = mem.SearchEmployees("TestMyra_").ToList();
            sw.Stop();
            Console.WriteLine($"Sökning exekverades på {sw.Elapsed.TotalMilliseconds:F1}ms och hittade {found.Count} testmyror.");
            Pause();
        }
    }
}