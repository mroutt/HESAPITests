﻿using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Tests
{
    [TestFixture]
    public class ServiceTests
    {
        [Flags]
        public enum VmsResponseCodes
        {
            Unset = 0,
            Expired = 1,
            TurnedIn = 2,
            Destroyed = 4,
            Cancelled = 8,
            BadgeNotFound = 16,
            InvalidBase = 32,
            InvalidGate = 64,
            FPCON = 128,
            PersonBarred = 256,
            NCIC = 1024,
            InternalError = 65536
        }

        private const string ValidBarcode = "STRV0001003276";
        private const string ExpiredBarcode = "TEMP0001003177";
        private const string TurnedInBarcode = "TEMP0001003179";
        private const string DestroyedBarcode = "TEMP0001003180";
        private const string CancelledBarcode = "TEMP0001003181";
        private const string BadgeNotFoundBarcode = "TEMP0001999999";
        private const string InvalidBaseBarcode = "STRV0001003282";
        private const string InvalidGateBarcode = "STRV0001003277";
        private const string FPCONBarcode = "TEMP0001003185";
        private const string PersonBarredBarcode = "STRV0001003283";
        private const string NCICBarcode = "STRV0001003273";
        private const string InternalErrorBarcode = "ERRTEST8675309";

        #region Helpers

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
                .AddParameter("StationId", 0)
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
                .AddParameter("ScanDateTime", DateTime.UtcNow.AddYears(100))
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

        private HESVisitorScanResponse PostBarcodeToService(string barcode,
           Func<string, IRestRequest> getRequestFunc)
        {
            var request = getRequestFunc(barcode);
            return GetHESResponseFromRequest(request);
        }

        private RestClient GetHESClient()
        {
            return new RestClient("http://app.huntinc.com/");
        }

        private HESVisitorScanResponse GetHESResponseFromRequest(IRestRequest request)
        {
            try
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
                throw new WebException(string.Format("HTTP status code of {0} returned.", response.StatusCode));
            }
            catch (Exception ex)
            {
               throw new Exception(string.Format("Exception occurred while processing service call. Inner Exception:{0} Message: {1}", ex, ex.Message), ex); 
                
            }
        }

        private void AssertHESResponseToEnsureResponseMeetsMinimumCriteria(string content)
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(content);
            Assert.IsTrue(dict.Keys.Contains("Code"), "Code is a required field in the response.");
            Assert.IsTrue(!dict.Keys.Contains("Reason"), "Reason is no longer a valid field in the response.");
        }

        private void TestThatExpectedResponseValuesArePresent(HESVisitorScanResponse response)
        {
            if (!response.Code.HasFlag(VmsResponseCodes.BadgeNotFound) &&
                !response.Code.HasFlag(VmsResponseCodes.InternalError))
            {
                Assert.IsNotEmpty(response.Firstname,
                    "Firstname field should have a value for all responses except those with codes BadgeNotFound and InternalError.");
                Assert.IsNotEmpty(response.Lastname, "Lastname field should have a value for all responses except those with codes BadgeNotFound and InternalError.");
                Assert.IsTrue(response.Expirationdate.HasValue, "Expiration date is required for all responses except those with codes BadgeNotFound and InternalError.");
                Assert.IsNotEmpty(response.Photo, "Photo is required for all responses except those with codes BadgeNotFound and InternalError.");
                Assert.IsNotEmpty(response.Sponsorg, "SponsorOrg is required for all responses except those with codes BadgeNotFound and InternalError");
            }
            Assert.IsNotEmpty(response.Status, "Status  must always be returned");
            Assert.IsTrue(response.Code >= 0, "Value of Code must always be equal to or greater than 0.");
        }

        #endregion

        [Test]
        public void TestValidBarcodeWithHes()
        {
            var response = PostBarcodeToService(ValidBarcode, GetValidHESRequest);
            Assert.IsTrue(response.Code == VmsResponseCodes.Unset);
            TestThatExpectedResponseValuesArePresent(response);
        }

        [Test]
        public void TestBadgeNotFoundBarcode()
        {
            var response = PostBarcodeToService(BadgeNotFoundBarcode, GetValidHESRequest);
            Assert.IsTrue(response.Code.HasFlag(VmsResponseCodes.BadgeNotFound), "Barcode {0} should return code BadgeNotFound", BadgeNotFoundBarcode);
            Assert.AreEqual("RED", response.Status, "When barcode has a valid format but is not in the system, the status should be 'RED'");
            TestThatExpectedResponseValuesArePresent(response);
        }

        [Test]
        public void TestExpiredBarcode()
        {
            var response = PostBarcodeToService(ExpiredBarcode, GetValidHESRequest);
            Assert.IsTrue(response.Code.HasFlag(VmsResponseCodes.Expired));
            Assert.AreEqual("RED", response.Status, "When barcode is expired, the status should be 'RED'");
            TestThatExpectedResponseValuesArePresent(response);
        }

        [Test]
        public void TestTurnedInBarcode()
        {
            var response = PostBarcodeToService(TurnedInBarcode, GetValidHESRequest);
            Assert.IsTrue(response.Code.HasFlag(VmsResponseCodes.TurnedIn));
            Assert.AreEqual("RED", response.Status, "When barcode is turned in, the status should be 'RED'");
            TestThatExpectedResponseValuesArePresent(response);
        }

        [Test]
        public void TestCancelledBarcode()
        {
            var response = PostBarcodeToService(CancelledBarcode, GetValidHESRequest);
            Assert.IsTrue(response.Code.HasFlag(VmsResponseCodes.Cancelled));
            Assert.AreEqual("RED", response.Status, "When barcode is Canceled, the status should be 'RED'");
            TestThatExpectedResponseValuesArePresent(response);
        }

        [Test]
        public void TestDestroyedBarcode()
        {
            var response = PostBarcodeToService(DestroyedBarcode, GetValidHESRequest);
            Assert.IsTrue(response.Code.HasFlag(VmsResponseCodes.Destroyed));
            Assert.AreEqual("RED", response.Status, "When barcode is destroyed, the status should be 'RED'");
            TestThatExpectedResponseValuesArePresent(response);
        }

        [Test]
        public void TestInvalidBaseBarcode()
        {
            var response = PostBarcodeToService(InvalidBaseBarcode, GetValidHESRequest);
            Assert.IsTrue(response.Code.HasFlag(VmsResponseCodes.InvalidBase));
            Assert.AreEqual("RED", response.Status, "When barcode is invalid base, the status should be 'RED'");
            TestThatExpectedResponseValuesArePresent(response);
        }

        [Test]
        public void TestInvalidGateBarcode()
        {
            var response = PostBarcodeToService(InvalidGateBarcode, GetHESRequestWithInvalidStationId);
            Assert.IsTrue(response.Code.HasFlag(VmsResponseCodes.InvalidGate));
            Assert.AreEqual("RED", response.Status, "When barcode is invalid gate, the status should be 'RED'");
            TestThatExpectedResponseValuesArePresent(response);
        }

        [Test]
        public void TestFPCONBarcode()
        {
            var response = PostBarcodeToService(FPCONBarcode, GetValidHESRequest);
            Assert.IsTrue(response.Code.HasFlag(VmsResponseCodes.FPCON));
            Assert.AreEqual("RED", response.Status, "When barcode is FPCON, the status should be 'RED'");
            TestThatExpectedResponseValuesArePresent(response);
        }

        [Test]
        public void TestPersonBarredBarcode()
        {
            var response = PostBarcodeToService(PersonBarredBarcode, GetValidHESRequest);
            Assert.IsTrue(response.Code.HasFlag(VmsResponseCodes.PersonBarred));
            Assert.AreEqual("RED", response.Status, "When barcode is person barred, the status should be 'RED'");
            TestThatExpectedResponseValuesArePresent(response);
        }

        [Test]
        public void TestNCICBarcode()
        {
            var response = PostBarcodeToService(NCICBarcode, GetValidHESRequest);
            Assert.IsTrue(response.Code.HasFlag(VmsResponseCodes.NCIC));
            Assert.AreEqual("RED", response.Status, "When barcode is NCIC, the status should be 'RED'");
            TestThatExpectedResponseValuesArePresent(response);
        }

        [Test]
        public void TestInternalErrorBarcode()
        {
            var response = PostBarcodeToService(InternalErrorBarcode, GetValidHESRequest);
            TestThatExpectedResponseValuesArePresent(response);
            Assert.IsTrue(response.Code.HasFlag(VmsResponseCodes.InternalError));
            Assert.AreEqual("YELLOW", response.Status, "Internal error response should return a status of 'YELLOW'");
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
            var response = PostBarcodeToService(ValidBarcode, GetHESRequestWithInvalidStationId);
            Assert.AreEqual("RED", response.Status, "Invalid StationId should return a status of 'RED'");
        }

        [Test]
        public void ShouldGetREDStatusReponseWithMissingScanDateTime()
        {
            var response = PostBarcodeToService(ValidBarcode, GetHESRequestWithMissingScanDateTime);
            Assert.AreEqual("RED", response.Status, "Missing ScanDateTime field  in request should return a status of 'RED'");
        }

        [Test]
        public void ShouldGetREDStatusReponseWithInvalidScanDateTime()
        {
            var response = PostBarcodeToService(ValidBarcode, GetHESRequestWithInvalidScanDateTime);
            Assert.AreEqual("RED", response.Status, "Missing ScanDateTime field  in request should return a status of 'RED'. This could be YELLOW too. Let us know.");
        }

        [Test]
        public void ShouldGetREDStatusWithMissingStationId()
        {
            var response = PostBarcodeToService(ValidBarcode, GetHESRequestWithMissingStationId);
            Assert.AreEqual("RED", response.Status, "Missing StationId should return a status of 'RED'");
        }

        [Test]
        public void ShouldGetGREENStatusWithMissingIncludePiiAndGoodBarcode()
        {
            var response = PostBarcodeToService(ValidBarcode, GetHESRequestWithMissingIncludePii);
            Assert.AreEqual("GREEN", response.Status, "Missing IncludePii should return a status of 'GREEN' with good barcode.");
        }
      
    }
}
