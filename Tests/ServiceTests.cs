using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;

namespace Tests
{
    public class ServiceTests
    {

        private const string GoodBarcode = "TEMP0001003127";
       
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
        public void ShouldGetBadReponseWithInvalidStationId()
        {
            var response = PostBarcodeToService(GoodBarcode, GetHESRequestWithInvalidStationId);
            Assert.IsTrue(response.Status != "GREEN", "Invalid station id should return a status of 'RED'");
        }

        [Test]
        public void ShouldGetBadReponseWithMissingStationId()
        {
            var response = PostBarcodeToService(GoodBarcode, GetHESRequestWithMissingStationId);
            Assert.IsTrue(response.Status != "GREEN", "Missing station id should return a status of 'RED'");
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
            request.AddParameter("StationId", "10012");
            request.AddParameter("ScanDateTime", DateTime.UtcNow.ToString());
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
        #endregion

        private RestClient GetHESClient()
        {
            return new RestClient("http://app.huntinc.com/");
        }

        private HESVisitorScanResponse GetHESResponseFromRequest(IRestRequest request)
        {
            var response =  GetHESClient().Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
                return JsonConvert.DeserializeObject<HESVisitorScanResponse>(response.Content);
            throw new Exception();
        }
    }
}
