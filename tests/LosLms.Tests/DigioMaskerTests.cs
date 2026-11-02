using System.Text.Json.Nodes;
using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

public class DigioMaskerTests
{
    [Fact]
    public void Mask_KeepsKeysAndStructure_ButHidesNamesDobAddressPincodeIdsAndPhotos()
    {
        var input = JsonNode.Parse("""{"status":"approved","name":"Raj Kumar","dob":"1990-05-17","pincode":"400001","city":"Pune","address":"1 Main Rd","id_no":"XXXXXXXX1234","mobile":"9876543210","pan":"ABCPE1234F","photo":"%s"}""".Replace("%s", new string('A', 300)));

        var text = DigioMasker.Mask(input)!.ToJsonString();

        foreach (var leak in new[] { "Raj", "1990", "400001", "Pune", "Main Rd", "9876543210", "ABCPE", new string('A', 50) })
        {
            Assert.DoesNotContain(leak, text);
        }

        Assert.Contains("\"status\":\"approved\"", text);
        Assert.Contains("\"dob\":\"0000-00-00\"", text);
    }
}
