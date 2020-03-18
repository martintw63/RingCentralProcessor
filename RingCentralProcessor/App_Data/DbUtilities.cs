using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace RingCentralProcessor.App_Data
{
    public class DbUtilities
    {
        private const string SqlConnStringFormat = "Data Source=tcp:{0};Initial Catalog={1};Persist Security Info=True;User ID={2};Password={3};Application Name={4};Workstation ID={4}";

        public enum DbRoles
        {
            sa_admin,           // - sa Admin User
            data_uploader,      //- User for the data import and upload processes.
            lead_processor,     //- User for ELI and other lead import processes.
            deal_push,          //-  User for all DMS pushes and SDCM processing
            email_processor,    //- User for all email applications
            research_processor, //- User for all research applications if necessary
            invoice_processor,  //- User for invoice upload processing
            cm_processor,       //- User for all measurement applications
            crm_user,           //- This ID will be used for database access within the CRM application web pages.
            xrm_user            //- This ID will be used for database access within the XRM application.
        }

        public static string BuildMyConnectionString(string encServerName, string encDbName, DbRoles role = DbRoles.xrm_user, string appName = "EmailBlast")
        {
            try
            {
                return string.Format(SqlConnStringFormat, encServerName, encDbName, GetRoleUserName(role), GetRolePassword(role), appName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public System.Data.DataSet GetDataSetFromStoredProc(string connectionString, string procedureName, string tableName)
        {
            var ds = new DataSet(tableName);
            using (var conn = new SqlConnection(connectionString))
            {
                var sqlComm = new SqlCommand(procedureName, conn) { CommandType = CommandType.StoredProcedure };
                var da = new SqlDataAdapter { SelectCommand = sqlComm };
                da.Fill(ds);
            }
            return ds;
        }

        //public RingCentalLoginInfo GetRingCentralClients(DataBaseInfo dataBaseInfo)
        //{

        //    var connString = BuildMyConnectionString(dataBaseInfo.ServerName,dataBaseInfo.DbName);
        //    using (var conn = new SqlConnection(connString))
        //    {
        //        var loginInfo = conn.Query<RingCentalLoginInfo>("P_WEBXRM_GET_RINGCENTRAL_INFO", commandType: CommandType.StoredProcedure).FirstOrDefault();

        //        return loginInfo;
        //    }

        //}

        public System.Data.DataTable GetRingCentralClients(DataBaseInfo dataBaseInfo)
        {
            var dt = new DataTable();
            try
            {

                var connectionString = BuildMyConnectionString(dataBaseInfo.ServerName, dataBaseInfo.DbName, DbRoles.xrm_user, "ServerInfo");
                using (var sqlConnection = new SqlConnection(connectionString))
                {
                    using (var sqlCommand = new SqlCommand())
                    {
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "P_WEBXRM_GET_RINGCENTRAL_INFO";
                        sqlCommand.CommandTimeout = 120;

                        var da = new SqlDataAdapter(sqlCommand);

                        da.Fill(dt);

                        sqlConnection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMsg = "GetRingCentralClients - Error Message: " + ex.Message;
                throw new Exception(errorMsg);
            }

            return dt;
        }

        public XRMServer GetAudioPath()
        {
            var result = new XRMServer();
            try
            {
                var connString = BuildMyConnectionString("CARSQLSERVER", "CARDB", DbRoles.xrm_user, "XrmCore");
                using (var conn = new SqlConnection(connString))
                {
                    var param = new DynamicParameters();
                    param.Add("@SeverCode", "AudioFiles");
                    result = conn.Query<XRMServer>("P_XRMCORE_GET_XRM_SERVER_PATH", param, commandType: CommandType.StoredProcedure).FirstOrDefault();
                }
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void InsertCallLog(RingCentralEvent objEvent, DataBaseInfo dataBaseInfo)
        {
            var connString = BuildMyConnectionString(dataBaseInfo.ServerName, dataBaseInfo.DbName, DbRoles.xrm_user, "XrmCore");
            try
            {
                using (var conn = new SqlConnection(connString))
                {
                    var param = new DynamicParameters();
                    param.Add("@PRM_DEALERID", objEvent.DealerId);
                    param.Add("@PRM_EVENTTYPE", objEvent.EventType);
                    param.Add("@PRM_CALLSOURCE", objEvent.CallSource);
                    param.Add("@PRM_CALLDATE", objEvent.CallDate);
                    param.Add("@PRM_CALLTIME", objEvent.CallTime);
                    param.Add("@PRM_CALLTYPE", objEvent.CallType);
                    param.Add("@PRM_EXTENSION", objEvent.Extension);
                    param.Add("@PRM_ACCOUNTCODE", objEvent.Extension);
                    param.Add("@PRM_RINGTIME", objEvent.RingTime);
                    param.Add("@PRM_DURATION", objEvent.Duration);
                    param.Add("@PRM_PHONENO", objEvent.PhoneNo);
                    if (objEvent.CustId > 0)
                    {
                        param.Add("@PRM_CUSTID", objEvent.CustId);
                    }
                    param.Add("@PRM_CALLTRACKID", objEvent.CallTrackId);
                    param.Add("@PRM_SOURCETYPE", "C2C");
                    param.Add("@PRM_SERVER", dataBaseInfo.ServerName);
                    param.Add("@PRM_DB", dataBaseInfo.DbName);
                    param.Add("@PRM_RECORDINGURL", objEvent.RecordingUrl);
                    param.Add("@PRM_SESSIONID", objEvent.SessionId);
                    conn.Execute("P_CTI_RC_PROCESS_DATA", param, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void LogProcess(RingCentalLoginInfo obj, DataBaseInfo dataBaseInfo, string returnedJson)
        {

            if (obj.DealerId > 0)
            {
                var fileUniqueName = Guid.NewGuid();
                var connString = BuildMyConnectionString(dataBaseInfo.ServerName, "CAR_LOGGING", DbRoles.sa_admin, "RingCentral");
                try
                {
                    using (var conn = new SqlConnection(connString))
                    {
                        var param = new DynamicParameters();
                        param.Add("@LogReason", "RingCentral Pull Call-logs - Json");
                        param.Add("@DealershipID", obj.DealerId);
                        param.Add("@XmlDoc", returnedJson);
                        conn.Execute("P_ADD_CTI_XML_LOG_ENTRY", param, commandType: CommandType.StoredProcedure);
                    }
                }
                catch (Exception ex)
                {
                    var strEx = ex.Message;
                }
            }

        }
        public string GetDealerInfo(string dbname)
        {
            var rntServer = string.Empty;

            try
            {
                var dt = new DataTable();
                var connectionString = BuildMyConnectionString("CARSQLSERVER", "CARDB", DbRoles.xrm_user, "ServerInfo");
                using (var sqlConnection = new SqlConnection(connectionString))
                {
                    using (var sqlCommand = new SqlCommand())
                    {
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "P_GET_ACTIVE_SERVER_BY";
                        sqlCommand.CommandTimeout = 120;

                        var sqlParams = sqlCommand.Parameters;
                        sqlParams.Add("@PRM_DBNAME", SqlDbType.VarChar).Value = dbname;

                        var da = new SqlDataAdapter(sqlCommand);

                        da.Fill(dt);

                        if ((dt.Rows.Count > 0))
                        {
                            foreach (DataRow dr in dt.Rows)
                            {
                                rntServer = Convert.ToString(dr["ServerName"]);
                                break;
                            }
                        }
                        sqlConnection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMsg = "GetDealerInfo - Error Message: " + ex.Message;
                throw new Exception(errorMsg);
            }

            return rntServer;
        }

        private static string GetRolePassword(DbRoles role)
        {
            switch (role)
            {
                case DbRoles.sa_admin:
                    return "whitehouse";

                default:
                    return Convert.ToString(role) + "1!";
            }
        }

        private static string GetRoleUserName(DbRoles role)
        {
            switch (role)
            {
                case DbRoles.sa_admin:
                    return "sa";

                default:
                    return Convert.ToString(role);
            }
        }


    }
}
