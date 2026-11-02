using LosLms.Models;
using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

/// <summary>Alternate mobile is optional; the father / spouse name from the Aadhaar address; an auto-save never blanks a filled value.</summary>
public sealed class FatherAndAltMobileTests
{
    [Theory]
    [InlineData("S/O Ramesh Kumar, 12-3 Main Rd, Pune 411001", "Ramesh Kumar")]
    [InlineData("S/O: Suresh Kumar, 1 Main Road", "Suresh Kumar")]
    [InlineData("W/O Sita Devi, House 4, Nashik", "Sita Devi")]
    [InlineData("D/O. Mohan Lal, near temple", "Mohan Lal")]
    [InlineData("H/O Ravi, Plot 9", "Ravi")]
    [InlineData("House 5, S/O Ganesh Rao, Nagar Road, Pune", "Ganesh Rao")]
    [InlineData("C/O Somebody, 1 Main Road", null)]      // care of is not a relative
    [InlineData("Flat 3, Pune 411001", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void CareOfName_ReadsTheRelativeFromTheAddress_ButNotCareOf(string? address, string? expected)
    {
        Assert.Equal(expected, DigioKyc.CareOfName(address));
    }

    [Fact]
    public void AlternateMobile_IsNotNeededForContactToBeComplete()
    {
        var p = new PartyForm
        {
            Mobile = "9876543210", Email = "a@b.co", Address1 = "1 Main", Address2 = "Sadar", City = "Pune", State = "Maharashtra", PinCode = "411001",
            ResidenceType = "Owned", YearsAtAddress = 5, AltMobile = "",
        };

        Assert.True(p.IsContactComplete);
    }

    [Fact]
    public void ABlankFormValue_NeverErasesAStoredOne_ButATypedValueReplacesIt()
    {
        Assert.Equal("SURESH KUMAR", FormMerge.KeepIfBlank("", "SURESH KUMAR"));
        Assert.Equal("SURESH KUMAR", FormMerge.KeepIfBlank("  ", "SURESH KUMAR"));
        Assert.Equal("RAMESH", FormMerge.KeepIfBlank("RAMESH", "SURESH KUMAR"));
        Assert.Null(FormMerge.KeepIfBlank(null, null));
    }
}
