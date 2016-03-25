using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public class ServiceTests
    {

        [Test]
        public void CanGetGoodReturnFromHES()
        {
            var expectedExpirationDate = new DateTime(2016, 3, 31);
            string expectedSponsorOrg = "TEST OFFICE SYMBOL 2";
            string expectedStatus = "GREEN";

            var response = PostBarcodeToService("TEMP0001003127");

            Assert.AreEqual("CAMEY", response.Firstname);
            Assert.AreEqual("ANDERSON", response.Lastname);
            Assert.AreEqual(expectedExpirationDate, response.Expirationdate);
            Assert.AreEqual(expectedStatus, response.Status);
            Assert.AreEqual(expectedSponsorOrg, response.Sponsorg);
            Assert.IsNotNull(response.Photo);
        }

        [Test]
        public void CanGetBadReturnFromHES()
        {
            var expectedExpirationDate = new DateTime(2016, 3, 18);
            string expectedStatus = "RED";
            string expectedFailureReason = "NCIC Check Issue.";

            var response = PostBarcodeToService("TEMP0001003142");

            Assert.AreEqual("TUESDAY", response.Firstname);
            Assert.AreEqual("NGUYEN", response.Lastname);
            Assert.AreEqual(expectedExpirationDate, response.Expirationdate);
            Assert.AreEqual(expectedStatus, response.Status);
            Assert.AreEqual(expectedFailureReason, response.Reason);

            Assert.IsNotEmpty(response.Photo, "Denied scans must still include a photo");
            Assert.IsNull(response.Middlename, "Null fields must be passed as true nulls and not a string representation of the word null");
            Assert.IsNull(response.Sponsorg, "Null fields must be passed as true nulls and not a string representation of the word null");
        }
        
        private HESVisitorScanResponse PostBarcodeToService(string barcode)
        {
            var request = GetHESRequest(barcode);
            return GetHESResponseFromRequest(request);
        }
        
        private RestClient GetHESClient()
        {
            return new RestClient("http://app.huntinc.com/");
        }

        private RestRequest GetHESRequest(string barcode)
        {
            var request = new RestRequest("api/MAXCheck/CheckVisitor", Method.POST);
            request.AddParameter("ScanData", barcode);
            request.AddParameter("FacilityId", "PNDL");
            request.AddParameter("ScanDateTime", DateTime.UtcNow.ToString());
            request.AddParameter("IncludePii", "true");

            return request;
        }

        private HESVisitorScanResponse GetHESResponseFromRequest(RestRequest request)
        {
            var client = GetHESClient();

            var rawResponse = client.Execute(request).Content;

            return JsonConvert.DeserializeObject<HESVisitorScanResponse>(rawResponse);
        }
    }
}
