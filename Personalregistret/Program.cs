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
            Console.WriteLine("Personalregister för Arbetsmyror (och framtida Bin!)");
            Console.WriteLine($"Register inläst. Senaste kända klockslag: {_repository.GetLastReadTime()}");
            Console.WriteLine("--------------------------------------------------");

            bool running = true;
            while (running)
            {
                Console.WriteLine("\nVälj ett alternativ:");
                Console.WriteLine("1. Lägg till ny personal (Myra)");
                Console.WriteLine("2. Sök personal");
                Console.WriteLine("3. Uppdatera personal");
                Console.WriteLine("4. Ta bort personal");
                Console.WriteLine("5. Visa all personal");
                Console.WriteLine("6. (Framtid) Lägg till Arbetsbi");
                Console.WriteLine("7. Avsluta");
                Console.Write("> ");

                switch (Console.ReadLine())
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
                        Console.WriteLine("Ogiltigt val. Försök igen.");
                        break;
                }
            }
        }

        private static void AddEmployee()
        {
            try
            {
                Console.Write("Namn: ");
                string name = Console.ReadLine() ?? "";

                Console.Write("Arbetar nattskift (j/n): ");
                bool isNightShift = Console.ReadLine()?.ToLower() == "j";

                // Här skapar vi en Myra. Detta uppfyller kravet om olika skattesatser
                // och framtida expansion.
                Ant newAnt = new Ant(name, isNightShift);

                _repository.AddEmployee(newAnt);
                Console.WriteLine($"Tillagd: {newAnt.Name} (ID: {newAnt.Id}).");
                Console.Write("Tryck Enter för att fortsätta...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fel: {ex.Message}");
                Console.Write("Tryck Enter för att fortsätta...");
                Console.ReadLine();
            }
        }

        private static void SearchEmployee()
        {
            Console.Write("Ange namn eller ID att söka efter: ");
            string searchTerm = Console.ReadLine() ?? "";

            var employees = _repository.SearchEmployees(searchTerm);

            if (!employees.Any())
            {
                Console.WriteLine("Ingen personal hittades.");
                return;
            }

            Console.WriteLine("Hittade följande:");
            foreach (var emp in employees)
            {
                Console.WriteLine(emp.GetDetails());
            }
            Console.Write("Tryck Enter för att fortsätta...");
            Console.ReadLine();
        }

        private static void UpdateEmployee()
        {
            Console.Write("Ange ID på personal att uppdatera: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Ogiltigt ID.");
                return;
            }

            var emp = _repository.GetEmployeeById(id);
            if (emp == null)
            {
                Console.WriteLine("Personal hittades inte.");
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
            Console.WriteLine("Personal uppdaterad.");
            Console.WriteLine($"Nuvarande: {emp.GetDetails()}");
            Console.Write("Tryck Enter för att fortsätta...");
            Console.ReadLine();
        }

        private static void RemoveEmployee()
        {
            Console.Write("Ange ID på personal att ta bort: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Ogiltigt ID.");
                Console.Write("Tryck Enter för att fortsätta...");
                Console.ReadLine();
                return;
            }

            var emp = _repository.GetEmployeeById(id);
            if (emp == null)
            {
                Console.WriteLine("Personal hittades inte.");
                Console.Write("Tryck Enter för att fortsätta...");
                Console.ReadLine();
                return;
            }

            Console.WriteLine($"Följande kommer att tas bort: {emp.GetDetails()}");
            Console.Write("Bekräfta borttagning? (j/n): ");
            var confirm = Console.ReadLine()?.ToLower() ?? "";
            if (confirm != "j")
            {
                Console.WriteLine("Borttagning avbröts.");
                Console.Write("Tryck Enter för att fortsätta...");
                Console.ReadLine();
                return;
            }

            _repository.DeleteEmployee(id);
            Console.WriteLine("Personal borttagen.");
            Console.Write("Tryck Enter för att fortsätta...");
            Console.ReadLine();
        }

        private static void ListAllEmployees()
        {
            // Vi hämtar även "döda" myror för att visa att vi hanterar livscykeln
            var employees = _repository.GetAllEmployees();

            if (!employees.Any())
            {
                Console.WriteLine("Registret är tomt.");
                return;
            }

            Console.WriteLine("\n--- All Personal ---");
            foreach (var emp in employees)
            {
                Console.WriteLine(emp.GetDetails());
            }
            Console.WriteLine("--- Slut på listan ---");
            Console.Write("Tryck Enter för att fortsätta...");
            Console.ReadLine();
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
            Console.WriteLine($"Tillagd: {newBee.Name} (ID: {newBee.Id}).");
            Console.WriteLine("Detta visar hur vi enkelt kan bygga ut systemet! (Polymorphism/Open-Closed Principle)");
            Console.Write("Tryck Enter för att fortsätta...");
            Console.ReadLine();
        }
    }
}