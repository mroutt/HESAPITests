using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;

namespace Tests
{
    [TestFixture]
    public class ServiceTests
    {

        private const string GoodBarcode = "TEMP0001003127";
        private const string InternalErrorBarcode = "ERRTEST8675309";
        private const string BarcodeNotInSystem = "TEMP0001999999";

        private const string UnsecureHesUri = "http://app.huntinc.com/";
       
        [Test]
        public void CanGetGoodReturnFromHES()
        {
            var expectedExpirationDate = new DateTime(2016, 3, 31);
            string expectedSponsorOrg = "TEST OFFICE SYMBOL 2";
            string expectedStatus = "GREEN";

            var response = PostBarcodeToService(GoodBarcode, GetValidHESRequest);

            Assert.AreEqual("CAMEY", response.Firstname);
            Assert.AreEqual("ANDERSON", response.Lastname);
            Assert.AreEqual(expectedExpirationDate, response.Expirationdate);
            Assert.AreEqual(expectedStatus, response.Status);
            Assert.AreEqual(expectedSponsorOrg, response.Sponsorg);
            Assert.IsNotNull(response.Photo);
        }

        [Test]
        public void CanGetReturnFromHESWhenBarcodeNotInVMS()
        {
            var response = PostBarcodeToService(BarcodeNotInSystem, GetValidHESRequest);
            Assert.AreEqual(response.Status, "RED",
                "When barcode has a valid format but is not in the system, the status should be 'RED'");
        }


        [Test]
        public void CanGetBadReturnFromHES()
        {
            var expectedExpirationDate = new DateTime(2016, 3, 18);
            string expectedStatus = "RED";
            string expectedFailureReason = "NCIC Check Issue.";

            var response = PostBarcodeToService("TEMP0001003142", GetValidHESRequest);

            Assert.AreEqual("TUESDAY", response.Firstname);
            Assert.AreEqual("NGUYEN", response.Lastname);

            Assert.AreEqual(expectedExpirationDate, response.Expirationdate);
            Assert.AreEqual(expectedStatus, response.Status);
            Assert.AreEqual(expectedFailureReason, response.Reason);

            Assert.IsNotEmpty(response.Photo, "Denied scans must still include a photo");
            Assert.IsNull(response.Middlename, "Null fields must be passed as true nulls and not a string representation of the word null");
            Assert.IsNull(response.Sponsorg, "Null fields must be passed as true nulls and not a string representation of the word null");
        }

        [Test]
        public void InternalErrorResponseShouldHaveYELLOWStatus()
        {
            var response = PostBarcodeToService(InternalErrorBarcode, GetValidHESRequest);
            Assert.AreEqual(response.Status, "YELLOW", "Internal error response should return a status of 'YELLOW'");
            Assert.IsTrue(!String.IsNullOrEmpty(response.ExtendedInfo), "Internal error response should contain ExtendedInfo");
        }

        [Test]
        public void InternalErrorResponseShouldIncludeExtendedInfo()
        {
            var response = PostBarcodeToService(InternalErrorBarcode, GetValidHESRequest);
            Assert.IsTrue(!String.IsNullOrEmpty(response.ExtendedInfo), "Internal error response should contain ExtendedInfo");
        }


        [Test]
        public void ShouldGetREDStatusWithInvalidStationId()
        {
            var response = PostBarcodeToService(GoodBarcode, GetHESRequestWithInvalidStationId);
            Assert.AreEqual(response.Status, "RED", "Invalid StationId should return a status of 'RED'");
        }

        [Test]
        public void ShouldGetREDStatusReponseWithMissingScanDateTime()
        {
            var response = PostBarcodeToService(GoodBarcode, GetHESRequestWithMissingScanDateTime);
            Assert.AreEqual(response.Status, "RED", "Missing ScanDateTime field  in request should return a status of 'RED'");
        }

        [Test]
        public void ShouldGetREDStatusReponseWithInvalidScanDateTime()
        {
            var response = PostBarcodeToService(GoodBarcode, GetHESRequestWithInvalidScanDateTime);
            Assert.AreEqual(response.Status, "RED", "Missing ScanDateTime field  in request should return a status of 'RED'. This could be YELLOW too. Let us know.");
        }

        [Test]
        public void ShouldGetREDStatusWithMissingStationId()
        {
            var response = PostBarcodeToService(GoodBarcode, GetHESRequestWithMissingStationId);
            Assert.AreEqual(response.Status, "RED", "Missing StationId should return a status of 'RED'");
        }

        [Test]
        public void ShouldGetGREENStatusWithMissingIncludePiiAndGoodBarcode()
        {
            var response = PostBarcodeToService(GoodBarcode, GetHESRequestWithMissingIncludePii);
            Assert.AreEqual(response.Status, "GREEN", "Missing IncludePii should return a status of 'GREEN' with good barcode.");
        }
        
        private HESVisitorScanResponse PostBarcodeToService(string barcode, 
            Func<string, IRestRequest> getRequestFunc )
        {
            var request = getRequestFunc(barcode);
            return GetHESResponseFromRequest(request);
        }

        #region BuildRequests
        private RestRequest GetValidHESRequest(string barcode)
        {
            var request = new RestRequest("api/MAXCheck/CheckVisitor", Method.POST);
            request.AddParameter("ScanData", barcode);
            request.AddParameter("StationId", 10012);
            request.AddParameter("ScanDateTime", DateTime.UtcNow);
            request.AddParameter("IncludePii", "true");
            return request;
        }

        private IRestRequest GetHESRequestWithInvalidStationId(string barcode)
        {
            return new RestRequest("api/MAXCheck/CheckVisitor", Method.POST)
                .AddParameter("ScanData", barcode)
                .AddParameter("StationId", "Bogus")
                .AddParameter("ScanDateTime", DateTime.UtcNow)
                .AddParameter("IncludePii", "true");
        }

        private IRestRequest GetHESRequestWithMissingStationId(string barcode)
        {
            return new RestRequest("api/MAXCheck/CheckVisitor", Method.POST)
                .AddParameter("ScanData", barcode)
                .AddParameter("ScanDateTime", DateTime.UtcNow)
                .AddParameter("IncludePii", "true");
        }

        private IRestRequest GetHESRequestWithMissingScanDateTime(string barcode)
        {
            return new RestRequest("api/MAXCheck/CheckVisitor", Method.POST)
                .AddParameter("ScanData", barcode)
                .AddParameter("StationId", 10012)
                .AddParameter("IncludePii", "true");
        }

        private IRestRequest GetHESRequestWithInvalidScanDateTime(string barcode)
        {
            return new RestRequest("api/MAXCheck/CheckVisitor", Method.POST)
                .AddParameter("ScanData", barcode)
                .AddParameter("ScanDateTime", "BogusValue")
                .AddParameter("StationId", 10012)
                .AddParameter("IncludePii", "true");
        }

        private IRestRequest GetHESRequestWithMissingIncludePii(string barcode)
        {
            return new RestRequest("api/MAXCheck/CheckVisitor", Method.POST)
                .AddParameter("ScanData", barcode)
                .AddParameter("StationId", 10012).
                AddParameter("IncludePii", "true");
        }

        #endregion

        private RestClient GetHESClient()
        {
            return new RestClient(UnsecureHesUri);
        }

        private HESVisitorScanResponse GetHESResponseFromRequest(IRestRequest request)
        {
            var response = GetHESClient().Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                AssertHESResponseToEnsureResponseMeetsMinimumCriteria(response.Content);
                return JsonConvert.DeserializeObject<HESVisitorScanResponse>(response.Content);
            }
            if (response.ErrorException != null)
            {
                throw response.ErrorException;
            }
            throw new Exception(string.Format("Response status code {0} returned.", response.ResponseStatus));
        }

        private void AssertHESResponseToEnsureResponseMeetsMinimumCriteria(string content)
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(content);
            Assert.AreEqual(dict.Count, 9, "All HES responses should contain 9 fields.");
            dict.Values.ToList().ForEach(value => Assert.AreNotEqual(value, "null", " The string 'null' is never a valid value in an HES response field."));
        }
    }
}
