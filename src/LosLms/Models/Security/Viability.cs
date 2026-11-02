using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// Monthly income and expense declared for an application. One row per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// Only the six declared amounts are stored. Total income, total expense, free cash flow, proposed
/// EMI and FOIR are all derived at render time and deliberately not persisted — storing a computed
/// figure alongside its inputs invites the two drifting apart.
/// </remarks>
public class Viability
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    public decimal IncomeFreight { get; set; }
    public decimal IncomeSalary { get; set; }
    public decimal IncomeOther { get; set; }

    public decimal ExpenseHousehold { get; set; }
    public decimal ExpenseFuelDriver { get; set; }
    public decimal ExpenseExistingEmi { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}
