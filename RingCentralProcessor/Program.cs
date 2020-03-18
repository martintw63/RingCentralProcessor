using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Data;
using System.Net.Http;
using RingCentralProcessor.App_Data;
using System.Globalization;

namespace RingCentralProcessor
{
    class Program
    {

        public static Logging LogBody;
        public static string accessToken;
        // public static string returnedJson;

        private static void Main(string[] args)
        {
            var dbset = new DbUtilities();

            try
            {

                if (args.Length == 0)
                {
                    LogBody = new Logging("CAR17");
                    LogBody.WriteAction("Searching " + "ZSAMPLE For RingCentral Users");

                    var dataBaseInfo = new DataBaseInfo
                    {
                        DbName = "CAR17",
                        ServerName = "CARSQLSERVER"
                    };

                    var dt = dbset.GetRingCentralClients(dataBaseInfo);
                    if ((dt.Rows.Count > 0))
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            dataBaseInfo = ProcessLoad(dataBaseInfo, dr);
                        }
                    }

                }
                else
                {
                    foreach (var dbname in args)
                    {
                        var dataBaseInfo = new DataBaseInfo
                        {
                            DbName = dbname,
                            ServerName = dbset.GetDealerInfo(dbname)
                        };


                        LogBody = new Logging(dataBaseInfo.DbName);
                        LogBody.WriteAction("Searching " + dbname + " For RingCentral Users");

                        var dt = dbset.GetRingCentralClients(dataBaseInfo);
                        if ((dt.Rows.Count > 0))
                        {
                            foreach (DataRow dr in dt.Rows)
                            {
                                dataBaseInfo = ProcessLoad(dataBaseInfo, dr);

                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            
        }

        private static DataBaseInfo ProcessLoad(DataBaseInfo dataBaseInfo, DataRow dr)
        {
            var ringCentralInfo = new RingCentalLoginInfo
            {
                AppKey = string.IsNullOrEmpty(Convert.ToString(dr["AppKey"])) ? "" : Convert.ToString(dr["AppKey"]),
                AppSecret = string.IsNullOrEmpty(Convert.ToString(dr["AppSecret"])) ? "" : Convert.ToString(dr["AppSecret"]),
                DealerId = string.IsNullOrEmpty(Convert.ToString(dr["DealerId"])) ? 0 : Convert.ToInt32(dr["DealerId"]),
                Username = string.IsNullOrEmpty(Convert.ToString(dr["Username"])) ? "" : Convert.ToString(dr["Username"]),
                Password = string.IsNullOrEmpty(Convert.ToString(dr["Password"])) ? "" : Convert.ToString(dr["Password"]),
                Extension = string.IsNullOrEmpty(Convert.ToString(dr["Extension"])) ? "" : Convert.ToString(dr["Extension"]),
                Url = string.IsNullOrEmpty(Convert.ToString(dr["Url"])) ? "" : Convert.ToString(dr["Url"]),
                MediaUrl = string.IsNullOrEmpty(Convert.ToString(dr["MediaUrl"])) ? "" : Convert.ToString(dr["MediaUrl"]),
                UrlRoute = string.IsNullOrEmpty(Convert.ToString(dr["UrlRoute"])) ? "" : Convert.ToString(dr["UrlRoute"]),
                AuthRoute = string.IsNullOrEmpty(Convert.ToString(dr["AuthRoute"])) ? "" : Convert.ToString(dr["AuthRoute"])
            };

            accessToken = GetRingCentralToken(ringCentralInfo);
            var result = GetCallLogs(ringCentralInfo, dataBaseInfo);
            ProcessCallLogs(result, ringCentralInfo, dataBaseInfo);

            return dataBaseInfo;
        }

        private static string GetRingCentralToken(RingCentalLoginInfo obj)
        {
            var _accessToken = "";
            try
            {
                using (var client = new HttpClient())
                {
                    var baseAddress = obj.Url + obj.AuthRoute;
                    var appKey = obj.AppKey;
                    var appSecret = obj.AppSecret;
                    var request = new HttpRequestMessage()
                    {
                        RequestUri = new System.Uri($"{baseAddress}"),
                        Method = HttpMethod.Post,
                    };
                    request.Headers.TryAddWithoutValidation("Content-Type", @"application/x-www-form-urlencoded;charset=UTF-8");
                    request.Headers.Add("Authorization", $"Basic {Base64Encode($"{appKey}:{appSecret}")}");
                    request.Content = new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("grant_type", "password"),
                        new KeyValuePair<string, string>("username", obj.Username),
                        new KeyValuePair<string, string>("password", obj.Password),
                        new KeyValuePair<string, string>("extension", obj.Extension)
                    });

                    var response = client.SendAsync(request).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        var d = (dynamic)Newtonsoft.Json.JsonConvert.DeserializeObject(responseContent);
                        _accessToken = d.access_token;
                    }
                }
                return _accessToken;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static Calling GetCallLogs(RingCentalLoginInfo obj, DataBaseInfo dataBaseInfo)
        {
            try
            {
                var callLogUrl = obj.Url + obj.UrlRoute;

                DateTimeFormatInfo usDtfi = new CultureInfo("en-US", false).DateTimeFormat;
                
                var dtm = DateTime.Now.AddMinutes(-30);
                //var dtm = Convert.ToDateTime("2018-10-08 00:00",usDtfi);
                dtm = TimeZoneInfo.ConvertTimeToUtc(dtm);
                var dtmEnd = DateTime.Now.AddMinutes(15);
                //var dtmEnd = Convert.ToDateTime("2018-10-08 23:59", usDtfi);
                dtmEnd = TimeZoneInfo.ConvertTimeToUtc(dtmEnd);

                // var dtm = DateTime.Now.AddDays(-6);
                // var dtmEnd = DateTime.Now.AddDays(-5);

                var yyyy = dtm.ToString("yyyy");
                var mm = dtm.ToString("MM");
                var dd = dtm.ToString("dd");

                var filterData = "view=Simple&perPage=100&dateFrom=" + yyyy + "-" + mm + "-" + dd + "T" + dtm.ToString("HH:mm") + ":00.000Z&dateTo=" + dtmEnd.ToString("yyyy") + "-" + dtmEnd.ToString("MM") + "-" + dtmEnd.ToString("dd")  + "T" + dtmEnd.ToString("HH:mm") + ":59.000Z";

                callLogUrl = callLogUrl + "?" + filterData;
                string responseContent;
                var request = System.Net.WebRequest.Create(callLogUrl);
                request.Headers.Add("Authorization", $"Bearer " + accessToken);

                var newStream = request.GetResponse().GetResponseStream();
                var newreadStream = new StreamReader(newStream, Encoding.UTF8);
                responseContent = newreadStream.ReadToEnd();

                var jsonDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Calling>(responseContent);

                var dbset = new DbUtilities();
                dbset.LogProcess(obj, dataBaseInfo, responseContent);

                return jsonDict;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static void ProcessCallLogs(Calling objCalling, RingCentalLoginInfo obj, DataBaseInfo dataBaseInfo)
        {
            var callData = new RingCentralEvent();
            var errString = String.Empty;
            try
            {
                foreach (var r in objCalling.Records)
                {
                    try
                    {
                        errString = String.Empty;
                        var webapi = string.Empty;
                        if (r.Direction == "Outbound")
                        {
                            
                            if (!string.IsNullOrEmpty(r.To.PhoneNumber))
                            {
                                callData.PhoneNo = r.To.PhoneNumber.Substring(2);
                                callData.Extension = r.From.PhoneNumber.Substring(2);
                                callData.CallType = "OG";
                                callData.CallSource = 2;
                            }
                        }
                        else //inbound
                        {
                            if (!string.IsNullOrEmpty(r.From.PhoneNumber))
                            {
                                callData.PhoneNo = r.From.PhoneNumber.Substring(2);
                                callData.Extension = r.To.PhoneNumber.Substring(2);
                                callData.CallType = "IC";
                                callData.CallSource = 2;
                            }
                        }
                        if (r.Recording != null && !string.IsNullOrEmpty(r.Recording.ContentUri))
                        {
                            webapi = GetRecording(r.Recording.ContentUri, obj.DealerId);
                        }

                        var dbset = new DbUtilities();

                        var convertedDtm = TimeZone.CurrentTimeZone.ToLocalTime(r.StartTime);
                        callData.CallDate = convertedDtm.Date;
                        callData.CallTime = convertedDtm.ToLongTimeString();
                        callData.CustId = 0;
                        callData.DealerId = obj.DealerId;
                        callData.Duration = r.Duration.ToString();
                        callData.EventType = Convert.ToInt32(Properties.Settings.Default.EventType);
                        callData.SessionId = r.SessionId; 
                        callData.RingTime = convertedDtm.ToLongTimeString();
                        callData.SalesPersonId = 0;
                        callData.RecordingUrl = webapi;
                        callData.CallTrackId = r.Id;
                        callData.ActionResult = r.Action + " - " + r.Result;
                        dbset.InsertCallLog(callData, dataBaseInfo);                    }
                    catch (Exception ex)
                    {
                        errString = ex.Message;
                    }


                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private static string GetRecording(string recordingUrl, int dealerId)
        {
            try
            {
                //var recordingUrl = "https://media.devtest.ringcentral.com/restapi/v1.0/account/246021004/recording/4703046004/content";
                //var fileUniqueName = Guid.NewGuid();
                var dbset = new DbUtilities();
                var path = dbset.GetAudioPath();

                var audioFileName = "RingCentral_" + recordingUrl.Split('/')[8] + ".mp3";
                var audioPath = $"{dealerId}\\";
                var webPath = $"{dealerId}/";

                Directory.CreateDirectory(Path.Combine(path.PhysicalFolder, audioPath));
                var wc = new WebClient
                {
                    UseDefaultCredentials = true,
                    Credentials = new NetworkCredential("administrator", "aalwayss!") // Check with Tim where to get this, else when it got change we have to change here
                };
                wc.Headers.Add("Authorization", $"Bearer " + accessToken);
                wc.DownloadFile(recordingUrl, Path.Combine(path.PhysicalFolder, audioPath) + audioFileName);
                return Path.Combine(path.WebUrl, webPath) + audioFileName;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

    }
}
