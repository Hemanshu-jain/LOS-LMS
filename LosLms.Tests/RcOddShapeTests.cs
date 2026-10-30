using System.Text.Json;
using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

/// <summary>A stored RC answer in an unexpected shape must never throw while the Loan &amp; Security page renders.</summary>
public sealed class RcOddShapeTests
{
    private static readonly RcInputs Inputs = new("MH12AB1234", "CH12345", "EN12345", "HDFC ERGO", "Raj Kumar", "Acme Finance", new DateOnly(2026, 10, 30));

    [Theory]
    [InlineData("""{"vehicle_details":null,"user_details":"x","finance_details":[],"permit_details":5}""")]
    [InlineData("""{"vehicle_details":{"rc_status":null,"challan_details":"none","fit_up_to":12,"masked_name":"yes"},"user_details":{"owner_name":7},"finance_details":{"financed":"maybe","insurance_upto":"not a date"},"permit_details":{"blacklist_status":{}}}""")]
    [InlineData("""{"vehicle_details":{"rc_number":"mh12ab1234","challan_details":{"a":1}},"finance_details":null}""")]
    [InlineData("""{"vehicle_details":{},"user_details":{},"finance_details":{},"permit_details":{}}""")]
    [InlineData("""{"vehicle_details":{"challan_details":[{"x":1}],"vehicle_chassis_number":"","vehicle_engine_number":"  "}}""")]
    public void Evaluate_NeverThrows_OnOddShapes(string json)
    {
        using var doc = JsonDocument.Parse(json);

        var rows = RcChecks.Evaluate(doc.RootElement, Inputs);

        Assert.NotEmpty(rows);
    }
}
