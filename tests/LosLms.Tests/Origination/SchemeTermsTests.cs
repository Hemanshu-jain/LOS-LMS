using LosLms.Models;
using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

public sealed class SchemeTermsTests
{
    private static SchemeTerms Gold => new() { Scheme = "Gold", Amount = 300_000m, Tenure = 24, Roi = 14m, ProcessingFee = 3_000m, AdvanceEmi = 12_000m };
    private static SchemeTerms Silver => new() { Scheme = "Silver", Amount = 200_000m, Tenure = 36, Roi = 16m, ProcessingFee = 2_500m, AdvanceEmi = 8_000m };

    [Fact]
    public void ABlankFile_StartsFromTheScheme()
    {
        var app = new Application();
        SchemeTermsService.Apply(app, null, Gold);

        Assert.Equal(300_000m, app.LoanAmount);
        Assert.Equal(24, app.Tenure);
        Assert.Equal(14m, app.Roi);
        Assert.Equal(3_000m, app.ProcessingFee);
        Assert.Equal(12_000m, app.AdvanceEmi);
    }

    [Fact]
    public void WhatTheOfficerChanged_IsKept_WhenTheSchemeChanges_ButUntouchedFiguresFollow()
    {
        var app = new Application();
        SchemeTermsService.Apply(app, null, Gold);
        app.Roi = 15.5m;            // the officer negotiated a different rate
        app.ProcessingFee = 4_000m; // and a different fee

        SchemeTermsService.Apply(app, Gold, Silver);

        Assert.Equal(15.5m, app.Roi);
        Assert.Equal(4_000m, app.ProcessingFee);
        Assert.Equal(200_000m, app.LoanAmount); // untouched, so it follows the new scheme
        Assert.Equal(36, app.Tenure);
        Assert.Equal(8_000m, app.AdvanceEmi);
    }

    [Fact]
    public void ASchemeWithoutTerms_ChangesNothing_AndASchemeThatSetsOnlySomeFiguresLeavesTheRest()
    {
        var app = new Application { LoanAmount = 123m, Tenure = 12 };
        SchemeTermsService.Apply(app, null, null);
        SchemeTermsService.Apply(app, null, new SchemeTerms { Roi = 11m });

        Assert.Equal(123m, app.LoanAmount);
        Assert.Equal(12, app.Tenure);
        Assert.Equal(11m, app.Roi);
        Assert.Null(app.ProcessingFee);
    }
}
