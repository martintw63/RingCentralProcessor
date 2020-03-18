using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingCentralProcessor.App_Data
{
    public class RingCentalLoginInfo
    {
        public string AcctId;
        public string AppKey;
        public string AppSecret;
        public int DealerId;
        public bool IsProduction;
        public string Username;
        public string Password;
        public string Extension;
        public string Url;
        public string MediaUrl;
        public string UrlRoute;
        public string AuthRoute;

    }
    public class Calling
    {
        public string Uri { get; set; }
        public List<Records> Records { get; set; }

    }
    public class Records
    {
        public string Uri { get; set; }
        public string Id { get; set; }
        public string SessionId { get; set; }
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
        public string Type { get; set; }
        public string Direction { get; set; }
        public string Action { get; set; }
        public string Result { get; set; }
        public To To { get; set; }
        public From From { get; set; }
        public Recording Recording { get; set; }
        public Extension Extension { get; set; }
    }

    public class From
    {
        public string PhoneNumber { get; set; }
        public string Name { get; set; }
    }

    public class To
    {
        public string PhoneNumber { get; set; }
        public string Location { get; set; }
    }

    public class Extension
    {
        public string Uri { get; set; }
        public string Id { get; set; }
    }



    public class Recording
    {
        public string Uri { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
        public string ContentUri { get; set; }
    }
    public class RingCentralEvent
    {
        public int CustId { get; set; }
        public int DealerId { get; set; }
        public int SalesPersonId { get; set; }
        public DateTime CallDate { get; set; }
        public string CallTime { get; set; }
        public string CallType { get; set; }
        public string Extension { get; set; }
        public string RingTime { get; set; }
        public string Duration { get; set; }
        public string PhoneNo { get; set; }
        public int EventType { get; set; }
        public string RecordingUrl { get; set; }
        public int CallSource { get; set; }
        public string CallTrackId { get; set; }
        public string SessionId { get; set; }
        public string ActionResult { get; set; }

    }
    public class XRMServer
    {
        public string ServerCode { get; set; }
        public string Description { get; set; }
        public string PhysicalFolder { get; set; }
        public string WebUrl { get; set; }
    }

    public class DataBaseInfo
    {
        public string ServerName;
        public string DbName;
    }
}
