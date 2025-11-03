using System.Collections.Concurrent;
using System.Text.Json;
using Personalregister.Models;

namespace Personalregister.Data
{
    // Simple in-memory implementation of IEmployeeRepository.
    // Uses Dictionary-backed storage and basic indexes for name and shift.
    public class InMemoryEmployeeRepository : IEmployeeRepository
    {
        private readonly Dictionary<int, Employee> _byId = new();
        private readonly Dictionary<string, HashSet<int>> _nameIndex = new(); // lowercase name -> ids
        private readonly HashSet<int> _nightShift = new();
        private int _nextId = 1;
        private readonly int _searchLimit;

        public InMemoryEmployeeRepository(AppConfig? config = null)
        {
            _searchLimit = config?.SearchLimit ?? 200;
        }

        public void AddEmployee(Employee employee)
        {
            if (employee.Id == 0)
            {
                employee.Id = _nextId++;
            }

            _byId[employee.Id] = employee;
            IndexEmployee(employee);
        }

        public void DeleteEmployee(int id)
        {
            if (_byId.Remove(id, out var emp))
            {
                RemoveIndexes(emp);
            }
        }

        public IEnumerable<Employee> GetAllEmployees()
        {
            // Return a copy to avoid external mutation issues and set LastReadTime on each
            var now = DateTime.UtcNow;
            return _byId.Values.Select(e => { e.LastReadTime = now; return e; }).ToList();
        }

        public Employee? GetEmployeeById(int id)
        {
            if (_byId.TryGetValue(id, out var emp))
            {
                emp.LastReadTime = DateTime.UtcNow;
                return emp;
            }
            return null;
        }

        public DateTime GetLastReadTime()
        {
            return DateTime.UtcNow; // For in-memory repository we return now
        }

        public IEnumerable<Employee> SearchEmployees(string searchTerm)
        {
            // Support searchTerm parsing for optional filters, e.g. "shift:night", "status:inactive"
            var term = (searchTerm ?? string.Empty).Trim();
            if (int.TryParse(term, out var id))
            {
                var emp = GetEmployeeById(id);
                return emp != null ? new[] { emp } : Array.Empty<Employee>();
            }

            bool filterNight = false;
            bool? filterActive = null;

            // simple inline filters
            if (term.Contains("shift:night", StringComparison.OrdinalIgnoreCase)) filterNight = true;
            if (term.Contains("shift:day", StringComparison.OrdinalIgnoreCase)) filterNight = false;
            if (term.Contains("status:active", StringComparison.OrdinalIgnoreCase)) filterActive = true;
            if (term.Contains("status:inactive", StringComparison.OrdinalIgnoreCase) || term.Contains("status:avliden", StringComparison.OrdinalIgnoreCase)) filterActive = false;

            // Remove known filters from term
            var cleaned = term
                .Replace("shift:night", "", StringComparison.OrdinalIgnoreCase)
                .Replace("shift:day", "", StringComparison.OrdinalIgnoreCase)
                .Replace("status:active", "", StringComparison.OrdinalIgnoreCase)
                .Replace("status:inactive", "", StringComparison.OrdinalIgnoreCase)
                .Replace("status:avliden", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            var results = new List<Employee>();

            if (string.IsNullOrEmpty(cleaned))
            {
                // if only filters provided, apply them across all
                foreach (var e in _byId.Values)
                {
                    if (MatchesFilters(e, filterNight, filterActive))
                    {
                        e.LastReadTime = DateTime.UtcNow;
                        results.Add(e);
                        if (results.Count >= _searchLimit) break;
                    }
                }
                return results;
            }

            var lowered = cleaned.ToLowerInvariant();
            foreach (var e in _byId.Values)
            {
                if ((e.Name?.ToLowerInvariant().Contains(lowered) ?? false) && MatchesFilters(e, filterNight, filterActive))
                {
                    e.LastReadTime = DateTime.UtcNow;
                    results.Add(e);
                    if (results.Count >= _searchLimit) break;
                }
            }

            return results;
        }

        public void UpdateEmployee(Employee employee)
        {
            if (_byId.ContainsKey(employee.Id))
            {
                RemoveIndexes(_byId[employee.Id]);
                _byId[employee.Id] = employee;
                IndexEmployee(employee);
            }
            else
            {
                AddEmployee(employee);
            }
        }

        // --- indexing helpers ---
        private void IndexEmployee(Employee e)
        {
            var nameKey = (e.Name ?? string.Empty).ToLowerInvariant();
            if (!_nameIndex.TryGetValue(nameKey, out var set))
            {
                set = new HashSet<int>();
                _nameIndex[nameKey] = set;
            }
            set.Add(e.Id);

            if (e is Ant ant && ant.WorksNightShift)
            {
                _nightShift.Add(e.Id);
            }
            else
            {
                _nightShift.Remove(e.Id);
            }
        }

        private void RemoveIndexes(Employee e)
        {
            var nameKey = (e.Name ?? string.Empty).ToLowerInvariant();
            if (_nameIndex.TryGetValue(nameKey, out var set))
            {
                set.Remove(e.Id);
                if (set.Count == 0) _nameIndex.Remove(nameKey);
            }
            _nightShift.Remove(e.Id);
        }

        private bool MatchesFilters(Employee e, bool filterNight, bool? filterActive)
        {
            if (filterActive.HasValue)
            {
                if (e.IsActive() != filterActive.Value) return false;
            }

            if (filterNight)
            {
                if (e is Ant a)
                {
                    if (!a.WorksNightShift) return false;
                }
                else return false;
            }

            return true;
        }
    }
}
