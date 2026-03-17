// ============================================================
// F1 Career Manager — NameGenerator.cs
// Generador de nombres por nacionalidad
// ============================================================
// Sin dependencias externas (no usa EventBus)
// Lee datos de arrays internos de nombres
// ============================================================

using System;
using System.Collections.Generic;

namespace F1CareerManager.Regen
{
    /// <summary>
    /// Resultado de generación de nombre
    /// </summary>
    public class GeneratedName
    {
        public string FirstName;
        public string LastName;
        public string FullName;
        public string Nationality;
        public string CountryCode;
        public string FlagEmoji;
    }

    /// <summary>
    /// Genera nombres completos aleatorios por nacionalidad,
    /// ponderados según tradición en F1.
    /// Sin repetir nombres ya existentes en el juego.
    /// </summary>
    public class NameGenerator
    {
        // ── Datos ────────────────────────────────────────────
        private Random _rng;
        private HashSet<string> _usedNames;

        // ── Pesos por nacionalidad (suman ~100) ──────────────
        // UK 18%, Alemania 12%, Francia 10%, Italia 9%,
        // Brasil 8%, España 7%, Holanda 5%, Australia 5%, Resto 26%
        private static readonly NationalityWeight[] NATIONALITY_WEIGHTS = new NationalityWeight[]
        {
            new NationalityWeight("British", "GB", "🇬🇧", 18),
            new NationalityWeight("German", "DE", "🇩🇪", 12),
            new NationalityWeight("French", "FR", "🇫🇷", 10),
            new NationalityWeight("Italian", "IT", "🇮🇹", 9),
            new NationalityWeight("Brazilian", "BR", "🇧🇷", 8),
            new NationalityWeight("Spanish", "ES", "🇪🇸", 7),
            new NationalityWeight("Dutch", "NL", "🇳🇱", 5),
            new NationalityWeight("Australian", "AU", "🇦🇺", 5),
            new NationalityWeight("Japanese", "JP", "🇯🇵", 4),
            new NationalityWeight("Mexican", "MX", "🇲🇽", 3),
            new NationalityWeight("American", "US", "🇺🇸", 4),
            new NationalityWeight("Canadian", "CA", "🇨🇦", 3),
            new NationalityWeight("Finnish", "FI", "🇫🇮", 3),
            new NationalityWeight("Danish", "DK", "🇩🇰", 2),
            new NationalityWeight("Thai", "TH", "🇹🇭", 2),
            new NationalityWeight("Argentine", "AR", "🇦🇷", 2),
            new NationalityWeight("Chinese", "CN", "🇨🇳", 2),
            new NationalityWeight("Indian", "IN", "🇮🇳", 1),
        };

        // ══════════════════════════════════════════════════════
        // NOMBRES POR NACIONALIDAD
        // ══════════════════════════════════════════════════════

        private static readonly Dictionary<string, string[]> FIRST_NAMES =
            new Dictionary<string, string[]>
        {
            { "British", new[] { "Oliver", "James", "George", "William", "Harry",
                "Liam", "Charlie", "Jack", "Thomas", "Alexander", "Edward", "Henry",
                "Samuel", "Benjamin", "Daniel", "Joseph", "Archie", "Freddie" } },
            { "German", new[] { "Lukas", "Felix", "Niklas", "Maximilian", "Leon",
                "Jonas", "Finn", "Tim", "Paul", "Moritz", "Sebastian", "Florian",
                "David", "Hans", "Klaus", "Erik", "Stefan", "Tobias" } },
            { "French", new[] { "Lucas", "Hugo", "Louis", "Gabriel", "Raphaël",
                "Arthur", "Jules", "Ethan", "Léo", "Théo", "Antoine", "Mathieu",
                "Victor", "Pierre", "Romain", "Baptiste", "Adrien", "Maxime" } },
            { "Italian", new[] { "Leonardo", "Francesco", "Lorenzo", "Alessandro",
                "Andrea", "Matteo", "Gabriele", "Riccardo", "Davide", "Marco",
                "Luca", "Giuseppe", "Federico", "Antonio", "Nicola", "Filippo" } },
            { "Brazilian", new[] { "Pedro", "Miguel", "Arthur", "Bernardo", "Heitor",
                "Rafael", "Davi", "Lucas", "Matheus", "Gabriel", "Caio", "Bruno",
                "Thiago", "Gustavo", "Felipe", "Henrique", "Vinícius", "Enzo" } },
            { "Spanish", new[] { "Pablo", "Hugo", "Martín", "Lucas", "Álvaro",
                "Adrián", "Mateo", "Daniel", "Alejandro", "Manuel", "Javier",
                "Carlos", "Diego", "Sergio", "Raúl", "Fernando", "Iñigo" } },
            { "Dutch", new[] { "Daan", "Sem", "Lucas", "Levi", "Finn",
                "Jesse", "Lars", "Bram", "Max", "Thijs", "Ruben", "Joris",
                "Stijn", "Niek", "Cas", "Mees", "Pieter", "Wouter" } },
            { "Australian", new[] { "Jack", "Oliver", "William", "Noah", "Leo",
                "Charlie", "Thomas", "Henry", "James", "Oscar", "Lachlan", "Ethan",
                "Cooper", "Mason", "Riley", "Angus", "Mitchell", "Dylan" } },
            { "Japanese", new[] { "Yuki", "Takumi", "Riku", "Haruto", "Sota",
                "Kaito", "Ren", "Hiroto", "Minato", "Takeshi", "Kenji", "Akira",
                "Daiki", "Ryota", "Shun", "Naoki", "Kenta", "Kazuki" } },
            { "Mexican", new[] { "Santiago", "Mateo", "Leonardo", "Emiliano",
                "Diego", "Miguel", "Daniel", "Alejandro", "Pablo", "Sebastián",
                "Andrés", "Carlos", "Ricardo", "Fernando", "Eduardo" } },
            { "American", new[] { "Liam", "Noah", "Oliver", "James", "Elijah",
                "Mason", "Logan", "Alexander", "Ethan", "Daniel", "Jackson",
                "Tyler", "Ryan", "Brandon", "Chase", "Colton", "Blake" } },
            { "Canadian", new[] { "Liam", "Noah", "Benjamin", "Ethan",
                "James", "Alexander", "William", "Lucas", "Logan", "Owen",
                "Nathan", "Jacob", "Samuel", "Gabriel", "Ryan", "Tyler" } },
            { "Finnish", new[] { "Onni", "Elias", "Leo", "Eino", "Väinö",
                "Oliver", "Noel", "Aaro", "Veeti", "Valtteri", "Kimi",
                "Mika", "Heikki", "Jari", "Lauri", "Rasmus", "Aleksi" } },
            { "Danish", new[] { "William", "Noah", "Oscar", "Lucas",
                "Victor", "Emil", "Magnus", "Mikkel", "Frederik", "Christian",
                "Andreas", "Mathias", "Kevin", "Rasmus", "Anders" } },
            { "Thai", new[] { "Thanawat", "Alexander", "Nattapong", "Kittipat",
                "Sarawut", "Piyawat", "Chayanon", "Kritsada", "Natthaphon",
                "Phakhin", "Sarut", "Ekkachai", "Kasem", "Somchai" } },
            { "Argentine", new[] { "Santiago", "Bautista", "Mateo", "Benicio",
                "Valentín", "Ciro", "Lautaro", "Felipe", "Thiago", "Joaquín",
                "Tomás", "Nicolás", "Francisco", "Agustín", "Emiliano" } },
            { "Chinese", new[] { "Wei", "Hao", "Jiao", "Ming", "Yichen",
                "Zhenyu", "Guanyu", "Xingyu", "Junhao", "Haoran",
                "Tianyu", "Yifan", "Zihan", "Bowen", "Chenxi" } },
            { "Indian", new[] { "Arjun", "Aarav", "Vihaan", "Aditya", "Krishna",
                "Sai", "Rohan", "Raj", "Vivaan", "Ishaan",
                "Dhruv", "Kabir", "Reyansh", "Karthik", "Jehan" } },
        };

        private static readonly Dictionary<string, string[]> LAST_NAMES =
            new Dictionary<string, string[]>
        {
            { "British", new[] { "Smith", "Johnson", "Williams", "Brown", "Jones",
                "Wilson", "Taylor", "Davies", "Evans", "Thomas", "Roberts",
                "Walker", "Wright", "Thompson", "White", "Hall", "Clarke",
                "Mitchell", "King", "Harris", "Green", "Wood" } },
            { "German", new[] { "Müller", "Schmidt", "Schneider", "Fischer", "Weber",
                "Meyer", "Wagner", "Becker", "Schulz", "Hoffmann", "Koch",
                "Richter", "Klein", "Braun", "Hartmann", "Werner", "Kraus" } },
            { "French", new[] { "Martin", "Bernard", "Dubois", "Thomas", "Robert",
                "Richard", "Petit", "Durand", "Leroy", "Moreau", "Simon",
                "Laurent", "Lefebvre", "Michel", "Garcia", "David", "Gasly" } },
            { "Italian", new[] { "Rossi", "Russo", "Ferrari", "Esposito", "Bianchi",
                "Romano", "Colombo", "Ricci", "Marino", "Greco", "Bruno",
                "Gallo", "Conti", "De Luca", "Mancini", "Lombardi", "Moretti" } },
            { "Brazilian", new[] { "Silva", "Santos", "Oliveira", "Souza", "Pereira",
                "Costa", "Rodrigues", "Almeida", "Nascimento", "Lima", "Araújo",
                "Fernandes", "Barrichello", "Fittipaldi", "Piquet", "Senna" } },
            { "Spanish", new[] { "García", "Rodríguez", "Martínez", "López", "González",
                "Hernández", "Pérez", "Sánchez", "Romero", "Torres", "Álvarez",
                "Ruiz", "Díaz", "Moreno", "Muñoz", "Iglesias", "De la Rosa" } },
            { "Dutch", new[] { "De Jong", "Jansen", "De Vries", "Van den Berg",
                "Van Dijk", "Bakker", "Visser", "Smit", "Meijer", "De Boer",
                "Mulder", "De Groot", "Bos", "Vos", "Peters", "Hendriks" } },
            { "Australian", new[] { "Smith", "Jones", "Williams", "Brown", "Wilson",
                "Taylor", "Johnson", "White", "Thompson", "Martin", "Anderson",
                "Walker", "Harris", "Clark", "Robinson", "Campbell", "Webber" } },
            { "Japanese", new[] { "Sato", "Suzuki", "Takahashi", "Tanaka", "Watanabe",
                "Ito", "Yamamoto", "Nakamura", "Kobayashi", "Yoshida", "Kato",
                "Matsuda", "Tsunoda", "Kamui", "Honda", "Miyamoto" } },
            { "Mexican", new[] { "Hernández", "García", "Martínez", "López", "González",
                "Rodríguez", "Pérez", "Sánchez", "Ramírez", "Torres", "Flores",
                "Rivera", "Cruz", "Morales", "Reyes", "Gutiérrez" } },
            { "American", new[] { "Smith", "Johnson", "Williams", "Brown", "Jones",
                "Miller", "Davis", "Garcia", "Wilson", "Anderson", "Thomas",
                "Taylor", "Moore", "Jackson", "Thompson", "Scott", "Andretti" } },
            { "Canadian", new[] { "Smith", "Brown", "Tremblay", "Martin", "Roy",
                "Wilson", "Gagnon", "Johnson", "Macdonald", "Taylor", "Côté",
                "Bouchard", "Gauthier", "Morin", "Lavoie", "Villeneuve" } },
            { "Finnish", new[] { "Korhonen", "Virtanen", "Mäkinen", "Nieminen",
                "Mäkelä", "Hämäläinen", "Laine", "Heikkinen", "Koskinen",
                "Häkkinen", "Räikkönen", "Bottas", "Kovalainen" } },
            { "Danish", new[] { "Jensen", "Nielsen", "Hansen", "Pedersen",
                "Andersen", "Christensen", "Larsen", "Sørensen", "Rasmussen",
                "Jørgensen", "Petersen", "Madsen", "Kristensen", "Magnussen" } },
            { "Thai", new[] { "Suthon", "Charoensuk", "Wongsawat", "Boonlert",
                "Tanasugarn", "Kanchanawat", "Srisai", "Tongchai", "Bolisut",
                "Suphanburi", "Panich", "Chanthawong", "Phromyothi" } },
            { "Argentine", new[] { "González", "Rodríguez", "Fernández", "López",
                "Martínez", "García", "Romero", "Díaz", "Álvarez", "Moreno",
                "Reutemann", "Fangio", "Peralta", "Agüero" } },
            { "Chinese", new[] { "Zhou", "Wang", "Li", "Zhang", "Liu", "Chen",
                "Yang", "Huang", "Zhao", "Wu", "Lin", "Sun",
                "Ma", "Gao", "He", "Luo" } },
            { "Indian", new[] { "Sharma", "Kumar", "Patel", "Singh", "Gupta",
                "Agarwal", "Reddy", "Mehta", "Joshi", "Das",
                "Narayan", "Chandhok", "Karthikeyan", "Narain" } },
        };

        // ══════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════

        public NameGenerator(Random rng = null)
        {
            _rng = rng ?? new Random();
            _usedNames = new HashSet<string>();
        }

        // ══════════════════════════════════════════════════════
        // GENERACIÓN
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genera un nombre completo aleatorio con nacionalidad ponderada.
        /// No repite nombres ya generados.
        /// </summary>
        public GeneratedName GenerateRandomName()
        {
            // Seleccionar nacionalidad por peso
            var nationality = SelectWeightedNationality();
            return GenerateNameForNationality(nationality);
        }

        /// <summary>
        /// Genera un nombre para una nacionalidad específica
        /// </summary>
        public GeneratedName GenerateNameForNationality(NationalityWeight nationality)
        {
            int maxAttempts = 30;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                string firstName = GetRandomFirst(nationality.Name);
                string lastName = GetRandomLast(nationality.Name);
                string fullName = $"{firstName} {lastName}";

                if (!_usedNames.Contains(fullName))
                {
                    _usedNames.Add(fullName);
                    return new GeneratedName
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        FullName = fullName,
                        Nationality = nationality.Name,
                        CountryCode = nationality.Code,
                        FlagEmoji = nationality.Flag
                    };
                }
            }

            // Si no se pudo generar único después de intentos, forzar con sufijo
            string fnFallback = GetRandomFirst(nationality.Name);
            string lnFallback = GetRandomLast(nationality.Name);
            string suffix = _rng.Next(1, 99).ToString();
            string fallbackFull = $"{fnFallback} {lnFallback}";

            // Usar un apellido ligeramente modificado para unicidad
            if (_usedNames.Contains(fallbackFull))
                lnFallback = lnFallback + " Jr.";

            _usedNames.Add($"{fnFallback} {lnFallback}");
            return new GeneratedName
            {
                FirstName = fnFallback,
                LastName = lnFallback,
                FullName = $"{fnFallback} {lnFallback}",
                Nationality = nationality.Name,
                CountryCode = nationality.Code,
                FlagEmoji = nationality.Flag
            };
        }

        /// <summary>
        /// Registra un nombre como ya usado (para pilotos reales existentes)
        /// </summary>
        public void RegisterExistingName(string fullName)
        {
            _usedNames.Add(fullName);
        }

        // ══════════════════════════════════════════════════════
        // SELECCIÓN PONDERADA
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Selecciona una nacionalidad basada en los pesos definidos
        /// </summary>
        private NationalityWeight SelectWeightedNationality()
        {
            int totalWeight = 0;
            foreach (var n in NATIONALITY_WEIGHTS)
                totalWeight += n.Weight;

            int roll = _rng.Next(0, totalWeight);
            int cumulative = 0;

            foreach (var n in NATIONALITY_WEIGHTS)
            {
                cumulative += n.Weight;
                if (roll < cumulative)
                    return n;
            }

            return NATIONALITY_WEIGHTS[0]; // Fallback
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        private string GetRandomFirst(string nationality)
        {
            if (FIRST_NAMES.ContainsKey(nationality))
            {
                var names = FIRST_NAMES[nationality];
                return names[_rng.Next(names.Length)];
            }
            // Fallback a British
            var fallback = FIRST_NAMES["British"];
            return fallback[_rng.Next(fallback.Length)];
        }

        private string GetRandomLast(string nationality)
        {
            if (LAST_NAMES.ContainsKey(nationality))
            {
                var names = LAST_NAMES[nationality];
                return names[_rng.Next(names.Length)];
            }
            var fallback = LAST_NAMES["British"];
            return fallback[_rng.Next(fallback.Length)];
        }
    }

    /// <summary>
    /// Peso de una nacionalidad para la generación
    /// </summary>
    public class NationalityWeight
    {
        public string Name;    // "British"
        public string Code;    // "GB"
        public string Flag;    // "🇬🇧"
        public int Weight;     // 18

        public NationalityWeight(string name, string code, string flag, int weight)
        {
            Name = name;
            Code = code;
            Flag = flag;
            Weight = weight;
        }
    }
}
