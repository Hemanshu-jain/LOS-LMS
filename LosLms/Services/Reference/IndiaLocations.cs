namespace LosLms.Services.Reference;

/// <summary>
/// India's states/UTs and their major cities, used to drive the linked State ↔ City dropdowns on the
/// Customer Details form: pick a state and only its cities show; pick a city and its state auto-fills.
/// </summary>
/// <remarks>
/// Not an exhaustive gazetteer — it lists the cities a lender is realistically going to see, which
/// keeps the dropdowns usable. A PIN-code lookup or manual value that is not in a list is still kept
/// and shown (see the form's option helpers), so nothing a user enters is lost.
/// </remarks>
public static class IndiaLocations
{
    public static readonly IReadOnlyDictionary<string, string[]> StateCities =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Andhra Pradesh"] = new[] { "Visakhapatnam", "Vijayawada", "Guntur", "Nellore", "Kurnool", "Rajahmundry", "Tirupati", "Kadapa", "Anantapur", "Kakinada", "Eluru", "Ongole" },
            ["Arunachal Pradesh"] = new[] { "Itanagar", "Naharlagun", "Pasighat", "Tawang" },
            ["Assam"] = new[] { "Guwahati", "Silchar", "Dibrugarh", "Jorhat", "Nagaon", "Tinsukia", "Tezpur" },
            ["Bihar"] = new[] { "Patna", "Gaya", "Bhagalpur", "Muzaffarpur", "Purnia", "Darbhanga", "Bihar Sharif", "Ara", "Begusarai" },
            ["Chhattisgarh"] = new[] { "Raipur", "Bhilai", "Bilaspur", "Korba", "Durg", "Rajnandgaon", "Raigarh" },
            ["Goa"] = new[] { "Panaji", "Margao", "Vasco da Gama", "Mapusa", "Ponda" },
            ["Gujarat"] = new[] { "Ahmedabad", "Surat", "Vadodara", "Rajkot", "Bhavnagar", "Jamnagar", "Gandhinagar", "Junagadh", "Anand", "Nadiad" },
            ["Haryana"] = new[] { "Gurugram", "Faridabad", "Panipat", "Ambala", "Yamunanagar", "Rohtak", "Hisar", "Karnal", "Sonipat", "Panchkula" },
            ["Himachal Pradesh"] = new[] { "Shimla", "Solan", "Dharamshala", "Mandi", "Kullu", "Bilaspur" },
            ["Jharkhand"] = new[] { "Ranchi", "Jamshedpur", "Dhanbad", "Bokaro Steel City", "Deoghar", "Hazaribagh" },
            ["Karnataka"] = new[] { "Bengaluru", "Mysuru", "Hubballi", "Mangaluru", "Belagavi", "Kalaburagi", "Davanagere", "Ballari", "Vijayapura", "Shivamogga", "Tumakuru" },
            ["Kerala"] = new[] { "Thiruvananthapuram", "Kochi", "Kozhikode", "Thrissur", "Kollam", "Kannur", "Alappuzha", "Palakkad", "Malappuram" },
            ["Madhya Pradesh"] = new[] { "Indore", "Bhopal", "Jabalpur", "Gwalior", "Ujjain", "Sagar", "Ratlam", "Satna", "Rewa", "Dewas" },
            ["Maharashtra"] = new[] { "Mumbai", "Pune", "Nagpur", "Nashik", "Aurangabad", "Solapur", "Thane", "Kolhapur", "Amravati", "Nanded", "Sangli", "Jalgaon", "Akola", "Latur" },
            ["Manipur"] = new[] { "Imphal", "Thoubal", "Kakching" },
            ["Meghalaya"] = new[] { "Shillong", "Tura", "Jowai" },
            ["Mizoram"] = new[] { "Aizawl", "Lunglei", "Champhai" },
            ["Nagaland"] = new[] { "Kohima", "Dimapur", "Mokokchung" },
            ["Odisha"] = new[] { "Bhubaneswar", "Cuttack", "Rourkela", "Berhampur", "Sambalpur", "Puri", "Balasore" },
            ["Punjab"] = new[] { "Ludhiana", "Amritsar", "Jalandhar", "Patiala", "Bathinda", "Mohali", "Hoshiarpur", "Pathankot" },
            ["Rajasthan"] = new[] { "Jaipur", "Jodhpur", "Kota", "Bikaner", "Ajmer", "Udaipur", "Bhilwara", "Alwar", "Sikar", "Sri Ganganagar" },
            ["Sikkim"] = new[] { "Gangtok", "Namchi", "Gyalshing" },
            ["Tamil Nadu"] = new[] { "Chennai", "Coimbatore", "Madurai", "Tiruchirappalli", "Salem", "Tirunelveli", "Erode", "Vellore", "Thoothukudi", "Tiruppur", "Thanjavur" },
            ["Telangana"] = new[] { "Hyderabad", "Warangal", "Nizamabad", "Karimnagar", "Khammam", "Ramagundam", "Mahbubnagar", "Secunderabad", "Nalgonda", "Siddipet" },
            ["Tripura"] = new[] { "Agartala", "Udaipur", "Dharmanagar" },
            ["Uttar Pradesh"] = new[] { "Lucknow", "Kanpur", "Ghaziabad", "Agra", "Varanasi", "Meerut", "Prayagraj", "Noida", "Bareilly", "Aligarh", "Moradabad", "Gorakhpur", "Saharanpur", "Jhansi" },
            ["Uttarakhand"] = new[] { "Dehradun", "Haridwar", "Roorkee", "Haldwani", "Rudrapur", "Kashipur", "Rishikesh" },
            ["West Bengal"] = new[] { "Kolkata", "Asansol", "Siliguri", "Durgapur", "Howrah", "Bardhaman", "Malda", "Kharagpur" },
            ["Andaman and Nicobar Islands"] = new[] { "Port Blair" },
            ["Chandigarh"] = new[] { "Chandigarh" },
            ["Dadra and Nagar Haveli and Daman and Diu"] = new[] { "Silvassa", "Daman", "Diu" },
            ["Delhi"] = new[] { "New Delhi", "Delhi", "Dwarka", "Rohini", "Pitampura" },
            ["Jammu and Kashmir"] = new[] { "Srinagar", "Jammu", "Anantnag", "Baramulla", "Udhampur" },
            ["Ladakh"] = new[] { "Leh", "Kargil" },
            ["Lakshadweep"] = new[] { "Kavaratti" },
            ["Puducherry"] = new[] { "Puducherry", "Karaikal", "Yanam", "Mahe" },
        };

    /// <summary>All state/UT names, alphabetical — the fixed State dropdown list.</summary>
    public static readonly string[] States = StateCities.Keys.OrderBy(s => s).ToArray();

    /// <summary>Every city across all states, distinct and alphabetical — shown when no state is chosen yet.</summary>
    public static readonly string[] AllCities = StateCities.Values
        .SelectMany(c => c)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(c => c)
        .ToArray();

    /// <summary>The cities for a state, or an empty list if the state is unknown/blank.</summary>
    public static string[] CitiesFor(string? state) =>
        !string.IsNullOrWhiteSpace(state) && StateCities.TryGetValue(state, out var cities)
            ? cities
            : Array.Empty<string>();

    /// <summary>The state a city belongs to (first match), or null if the city is not in the dataset.</summary>
    public static string? StateForCity(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        foreach (var (state, cities) in StateCities)
        {
            if (cities.Contains(city, StringComparer.OrdinalIgnoreCase))
            {
                return state;
            }
        }

        return null;
    }
}
