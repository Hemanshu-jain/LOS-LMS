namespace LosLms.Services;

/// <summary>A dropdown with exactly one real option should not make the officer pick it: blank becomes that option.</summary>
public static class OnlyChoice
{
    public static string? IfBlank(string? current, IReadOnlyList<string> options) =>
        string.IsNullOrWhiteSpace(current) && options.Count == 1 ? options[0] : current;
}
