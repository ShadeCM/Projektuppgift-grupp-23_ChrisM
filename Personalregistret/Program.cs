// Program.cs
using Personalregister.Data;
using Personalregister.Models;

namespace Personalregister
{
    class Program
    {
        // Vi använder Dependency Injection (från SOLID)
        // Programmet beror på en IEmployeeRepository, inte en specifik databas.
        private static readonly IEmployeeRepository _repository = new EmployeeRepository("personal.db");
        static void Main(string[] args)
        {
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
    }
}