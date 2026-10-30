namespace LosLms.Services;

/// <summary>
/// Customer Details auto-saves the whole open form. A field that something else filled meanwhile (a DigiLocker result arriving from the customer's
/// phone) must not be wiped by a form that has not seen it yet: a blank form value never erases a stored one. To correct a value, type the right one.
/// </summary>
public static class FormMerge
{
    public static string? KeepIfBlank(string? form, string? stored) => string.IsNullOrWhiteSpace(form) ? stored : form;
}
