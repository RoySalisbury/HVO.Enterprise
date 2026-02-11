using System;
using System.Net.Http;
using System.Threading.Tasks;
using HVO.Enterprise.Samples.Net8;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Samples.Net8.Tests.Integration
{
    /// <summary>
    /// Diagnostic test to identify runtime errors in the sample app.
    /// </summary>
    [TestClass]
    public class DiagnosticTests
    {
        [TestMethod]
        public async Task Diagnose_InternalServerError()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/ping");
            var body = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Body: {body}");

            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, $"Response body: {body}");
        }
    }
}
