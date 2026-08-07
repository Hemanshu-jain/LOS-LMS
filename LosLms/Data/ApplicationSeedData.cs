using LosLms.Models;

namespace LosLms.Data;

/// <summary>
/// The 128 applications seeded into the database via <c>HasData</c>.
/// </summary>
/// <remarks>
/// Replaces the old in-memory MockApplicationProvider. The first nine rows are the Applications
/// Dashboard spec's sample data reproduced field-for-field; the remaining 119 are generated from a
/// fixed seed so the dashboard, its pagination and its triage counts are identical to the
/// pre-database build and stable across restarts.
/// </remarks>
public static class ApplicationSeedData
{
    /// <summary>
    /// The "today" the dashboard's SLA maths is measured against.
    /// </summary>
    /// <remarks>
    /// MOCK ONLY — the seeded queue dates are generated around this fixed anchor so the SLA warning
    /// lands on a realistic subset of rows. Production must use the server date.
    /// </remarks>
    public static readonly DateOnly ReferenceToday = new(2026, 7, 13);

    private const int GeneratedCount = 119;   // 9 spec rows + 119 = 128 total

    private static readonly string[] Products = { "Commercial vehicle", "Loan against property" };
    private static readonly string[] Branches = { "Nashik West", "Nashik East", "Pune Camp", "Aurangabad", "Jalgaon" };
    // "DSA" became "DSA — Patil Motors" when Stage 2 introduced named sourcing partners. Same array
    // length, so the deterministic sequence and every other generated value are unchanged.
    private static readonly string[] Channels = { "DSA — Patil Motors", "Branch walk-in", "Digital" };
    private static readonly string[] Officers = { "R. Kulkarni", "S. Deshpande", "A. Rao" };

    private static readonly string[] FirstNames =
    {
        "Ramesh", "Sunita", "Anil", "Meena", "Vikram", "Farhan", "Prakash", "Nilima",
        "Sachin", "Kavita", "Ganesh", "Pooja", "Mahesh", "Rekha", "Sanjay", "Deepa",
        "Ajay", "Smita", "Rahul", "Vaishali", "Nitin", "Manisha", "Sunil", "Archana",
        "Yogesh", "Shalini", "Dattatray", "Jyoti", "Amol", "Trupti",
    };

    private static readonly string[] LastNames =
    {
        "Pawar", "Deshmukh", "Jadhav", "Kulkarni", "Rao", "Shaikh", "Patil", "Joshi",
        "More", "Shinde", "Bhosale", "Gaikwad", "Chavan", "Salunkhe", "Kadam", "Thorat",
        "Sawant", "Mane", "Nikam", "Waghmare", "Bhalerao", "Dhumal", "Ingle", "Khedkar",
    };

    private static readonly string[] FirmNames =
    {
        "Shree Roadlines", "Iqbal Transport LLP", "Balaji Carriers", "Sai Logistics",
        "Konkan Freight Movers", "Deccan Cargo LLP", "Ganraj Transports", "Vitthal Carriers",
        "Maratha Roadways", "Sahyadri Transport Co", "Jai Malhar Logistics", "Om Sai Carriers",
        "Panchvati Freight", "Godavari Movers", "Ajanta Roadlines", "Sinhagad Transport LLP",
    };

    /// <summary>Builds all 128 rows. Deterministic — same output on every call.</summary>
    public static List<Application> Build()
    {
        var rows = new List<Application>(9 + GeneratedCount);
        rows.AddRange(SpecRows());

        var rng = new Random(20260713);

        // Queue dates are drawn first and sorted newest-first so descending application numbers
        // line up with descending dates, the way the spec's nine rows do.
        var dates = new List<DateOnly>(GeneratedCount);
        for (var i = 0; i < GeneratedCount; i++) dates.Add(RandomQueueDate(rng));
        dates.Sort((a, b) => b.CompareTo(a));

        var idNumber = 4790;   // starts just below the spec's lowest number, LN-2026-004795
        foreach (var queueDate in dates)
        {
            // The order of these Random calls is load-bearing. It reproduces the exact dataset the
            // dashboard was built and verified against, which is why the PAN draw below is still
            // made and then discarded: PAN moved to the Parties table, but removing the call would
            // shift every subsequent value and change the triage counts.
            var product = Products[rng.Next(Products.Length)];
            var status = RandomStatus(rng);
            var branch = Branches[rng.Next(Branches.Length)];
            var customer = RandomCustomer(rng, product);
            var amount = rng.Next(75, 480) * 10_000m;
            var stage = RandomStageNumber(rng, status);
            var channel = Channels[rng.Next(Channels.Length)];
            var officer = Officers[rng.Next(Officers.Length)];
            _ = RandomPan(rng);   // sequence alignment only — see comment above

            var created = queueDate.ToDateTime(TimeOnly.MinValue);

            rows.Add(new Application
            {
                Id = $"LN-2026-{idNumber:D6}",
                CustomerType = CustomerTypeFor(product),
                Branch = branch,
                LoanProduct = product,
                Scheme = SchemeFor(product),
                LoanAmount = amount,
                Tenure = TenureFor(idNumber),
                Roi = RoiFor(idNumber),
                CurrentStage = stage,
                Status = status,
                CustomerName = customer,
                SourcingChannel = channel,
                AssignedOfficer = officer,
                CreatedAt = created,
                UpdatedAt = created,
            });

            idNumber -= rng.Next(2, 9);
        }

        return rows;
    }

    /// <summary>The nine rows from the dashboard spec, reproduced field-for-field.</summary>
    private static IEnumerable<Application> SpecRows()
    {
        // Id, product, branch, customer, amount, status, stage, queue date, channel, officer
        var spec = new (string Id, string Product, string Branch, string Customer, decimal Amount,
            string Status, int Stage, DateOnly Queued, string Channel, string Officer)[]
        {
            ("LN-2026-004871", "Commercial vehicle",    "Nashik West", "Ramesh Pawar",        1_850_000m, "In progress", 3, new(2026, 7, 12), "DSA — Patil Motors", "R. Kulkarni"),
            ("LN-2026-004868", "Loan against property", "Pune Camp",   "Sunita Deshmukh",     4_200_000m, "In progress", 6, new(2026, 7, 11), "Branch walk-in", "S. Deshpande"),
            ("LN-2026-004859", "Commercial vehicle",    "Aurangabad",  "Iqbal Transport LLP", 2_675_000m, "In progress", 4, new(2026, 7, 10), "DSA — Patil Motors", "A. Rao"),
            ("LN-2026-004844", "Loan against property", "Nashik West", "Anil Jadhav",         1_500_000m, "In progress", 7, new(2026, 7,  8), "Digital",        "R. Kulkarni"),
            ("LN-2026-004831", "Commercial vehicle",    "Jalgaon",     "Shree Roadlines",     3_120_000m, "Sanctioned",  8, new(2026, 7,  5), "DSA — Patil Motors", "S. Deshpande"),
            ("LN-2026-004820", "Loan against property", "Pune Camp",   "Meena Kulkarni",      2_240_000m, "In progress", 2, new(2026, 7,  4), "Branch walk-in", "A. Rao"),
            ("LN-2026-004811", "Commercial vehicle",    "Nashik East", "Balaji Carriers",     1_990_000m, "New",         1, new(2026, 7,  2), "Digital",        "R. Kulkarni"),
            ("LN-2026-004802", "Loan against property", "Aurangabad",  "Farhan Shaikh",       3_750_000m, "In progress", 5, new(2026, 7,  1), "DSA — Patil Motors", "S. Deshpande"),
            ("LN-2026-004795", "Commercial vehicle",    "Pune Camp",   "Vikram Rao",          1_420_000m, "Rejected",    5, new(2026, 6, 28), "Branch walk-in", "A. Rao"),
        };

        foreach (var r in spec)
        {
            var created = r.Queued.ToDateTime(TimeOnly.MinValue);
            yield return new Application
            {
                Id = r.Id,
                CustomerType = CustomerTypeFor(r.Product),
                Branch = r.Branch,
                LoanProduct = r.Product,
                Scheme = SchemeFor(r.Product),
                LoanAmount = r.Amount,
                // LN-2026-004871 is the application the Customer Details spec uses as its sample,
                // so its tenure and ROI are pinned to the values shown there.
                Tenure = r.Id == "LN-2026-004871" ? 48 : TenureFor(SequenceOf(r.Id)),
                Roi = r.Id == "LN-2026-004871" ? 13.25m : RoiFor(SequenceOf(r.Id)),
                CurrentStage = r.Stage,
                Status = r.Status,
                CustomerName = r.Customer,
                SourcingChannel = r.Channel,
                AssignedOfficer = r.Officer,
                CreatedAt = created,
                UpdatedAt = created,
            };
        }
    }

    private static int SequenceOf(string id) =>
        int.TryParse(id.AsSpan(id.LastIndexOf('-') + 1), out var n) ? n : 0;

    private static string CustomerTypeFor(string product) =>
        product == "Commercial vehicle" ? "Individual · CV" : "Individual · LAP";

    private static string SchemeFor(string product) =>
        product == "Commercial vehicle" ? "CV-STD-2026" : "LAP-STD-2026";

    // Derived from the id, not from the Random sequence, so adding these columns could not disturb
    // the generated dataset.
    private static int TenureFor(int sequence) =>
        (sequence % 4) switch { 0 => 36, 1 => 48, 2 => 60, _ => 72 };

    private static decimal RoiFor(int sequence) => 12.50m + (sequence % 9) * 0.25m;

    /// <summary>
    /// Weighted so a plausible share of the queue is still inside the SLA window. A flat spread
    /// backwards in time would put the overdue warning on nearly every row, making it meaningless.
    /// </summary>
    private static DateOnly RandomQueueDate(Random rng)
    {
        var roll = rng.Next(100);
        var daysBack =
            roll < 45 ? rng.Next(0, 6) :    // comfortably inside the SLA window
            roll < 70 ? rng.Next(6, 15) :   // just past it
            rng.Next(15, 56);               // long overdue
        return ReferenceToday.AddDays(-daysBack);
    }

    private static string RandomStatus(Random rng)
    {
        var roll = rng.Next(100);
        if (roll < 14) return "New";
        if (roll < 74) return "In progress";
        if (roll < 90) return "Sanctioned";
        return "Rejected";
    }

    private static int RandomStageNumber(Random rng, string status) => status switch
    {
        "New" => 1,
        "Sanctioned" => 8,
        "Rejected" => rng.Next(3, 6) + 1,   // rejections land mid-pipeline
        _ => rng.Next(1, 7) + 1,
    };

    private static string RandomCustomer(Random rng, string product)
    {
        // Commercial vehicle loans skew towards transport firms; property loans towards individuals.
        var isFirm = product == "Commercial vehicle" && rng.Next(100) < 45;
        return isFirm
            ? FirmNames[rng.Next(FirmNames.Length)]
            : $"{FirstNames[rng.Next(FirstNames.Length)]} {LastNames[rng.Next(LastNames.Length)]}";
    }

    private static string RandomPan(Random rng)
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var chars = new char[10];
        for (var i = 0; i < 5; i++) chars[i] = letters[rng.Next(letters.Length)];
        for (var i = 5; i < 9; i++) chars[i] = (char)('0' + rng.Next(10));
        chars[9] = letters[rng.Next(letters.Length)];
        return new string(chars);
    }
}
