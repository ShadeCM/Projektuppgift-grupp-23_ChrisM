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
        private readonly Dictionary<string, HashSet<int>> _nameIndex = new(); // lowercase full name -> ids
        private readonly Dictionary<string, HashSet<int>> _prefixIndex = new(); // name prefixes -> ids
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

            // Try prefix index first for fast matches
            if (_prefixIndex.TryGetValue(lowered, out var ids))
            {
                foreach (var eid in ids)
                {
                    if (_byId.TryGetValue(eid, out var e) && MatchesFilters(e, filterNight, filterActive))
                    {
                        e.LastReadTime = DateTime.UtcNow;
                        results.Add(e);
                        if (results.Count >= _searchLimit) break;
                    }
                }
                return results;
            }

            // Fallback to substring scan
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

            // index prefixes for the name (for fast prefix search)
            for (int len = 1; len <= Math.Min(30, nameKey.Length); len++)
            {
                var prefix = nameKey.Substring(0, len);
                if (!_prefixIndex.TryGetValue(prefix, out var pset))
                {
                    pset = new HashSet<int>();
                    _prefixIndex[prefix] = pset;
                }
                pset.Add(e.Id);
            }

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

            // remove from prefix index
            for (int len = 1; len <= Math.Min(30, nameKey.Length); len++)
            {
                var prefix = nameKey.Substring(0, len);
                if (_prefixIndex.TryGetValue(prefix, out var pset))
                {
                    pset.Remove(e.Id);
                    if (pset.Count == 0) _prefixIndex.Remove(prefix);
                }
            }
        }

        // --- snapshot helpers (JSONL) ---
        public void SaveSnapshot(string path)
        {
            var options = new JsonSerializerOptions { WriteIndented = false };
            using var sw = new StreamWriter(path, false);
            foreach (var e in _byId.Values)
            {
                var wrapper = new { Type = e.GetType().Name, Data = e };
                sw.WriteLine(JsonSerializer.Serialize(wrapper, options));
            }
        }

        public void LoadSnapshot(string path)
        {
            if (!File.Exists(path)) return;
            int maxId = _nextId - 1;
            var options = new JsonSerializerOptions();
            foreach (var line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    var type = root.GetProperty("Type").GetString();
                    var data = root.GetProperty("Data").GetRawText();
                    Employee? emp = null;
                    if (type == nameof(Ant)) emp = JsonSerializer.Deserialize<Ant>(data, options);
                    else if (type == nameof(Bee)) emp = JsonSerializer.Deserialize<Bee>(data, options);
                    if (emp != null)
                    {
                        emp.LastReadTime = DateTime.UtcNow; // requirement: set to current time per object
                        _byId[emp.Id] = emp;
                        IndexEmployee(emp);
                        if (emp.Id > maxId) maxId = emp.Id;
                    }
                }
                catch { /* ignore malformed lines */ }
            }
            _nextId = maxId + 1;
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
