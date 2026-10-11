using Common;
using DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Data {
    /// <summary>
    /// Thread-safe base class for database operations.
    /// All public methods use proper locking to prevent concurrent access issues.
    /// </summary>
    public class DataETLBase {
        public DBManager dBManager = null;
        private readonly object _dbLock = new object();

        public string InputFileLocation = @"C:\_DATA";

        protected virtual List<string> GetQSOLines(DataTable dt, ContestEnum cabrilloCode) {
            return new List<string>();
        }

        public DataETLBase(string connString) {
            InitiateDBManager(connString);
        }

        private void InitiateDBManager(string connString) {
            lock (_dbLock) {
                dBManager = new DBManager(DataProvider.SqlServer, connString);
                dBManager.Open();
            }
        }

        /// <summary>
        /// Ensure connection is open (must be called inside lock)
        /// </summary>
        public void EnsureConnectionOpen() {
            if (dBManager.Connection == null || dBManager.Connection.State != ConnectionState.Open) {
                dBManager.Open();
            }
        }

        /// <summary>
        /// Thread-safe: Get QSO data for contest export
        /// </summary>
        protected DataTable GetQSODataContestForExport(string ContestIdDash, int ContestYear) {
            lock (_dbLock) {
                try {
                    EnsureConnectionOpen();
                    dBManager.CreateParameters(2);
                    dBManager.AddParameters(0, "@contestId", ContestIdDash);
                    dBManager.AddParameters(1, "@year", ContestYear);

                    string sql = @"
                        SELECT * 
                        FROM vQSOs q
                        WHERE q.contest_id = @contestId
                          AND YEAR(q.QSO_Begin) = @year
                        ORDER BY q.QSO_Begin, q.STX";

                    return dBManager.ExecuteDataTable(CommandType.Text, sql);
                } catch (Exception ex) {
                    throw new Exception($"GetQSODataContestForExport failed: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Thread-safe: Execute SQL query and return DataTable
        /// </summary>
        public DataTable GetResultsetasDatatable(string sql) {
            lock (_dbLock) {
                try {
                    EnsureConnectionOpen();
                    dBManager.CreateParameters(0);
                    return dBManager.ExecuteDataTable(CommandType.Text, sql);
                } catch (Exception ex) {
                    throw new Exception($"GetResultsetasDatatable failed for SQL: {sql.Substring(0, Math.Min(100, sql.Length))}... Error: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Thread-safe: Execute stored procedure with QSOInfo parameter
        /// </summary>
        public IEnumerable<T> ExecuteSPObject<T>(QSOInfo qsoInfo) {
            DataTable dtvQSOs;

            lock (_dbLock) {
                try {
                    EnsureConnectionOpen();
                    dBManager.CreateParameters(1);
                    dBManager.AddParameters(0, "@callPattern", $"%{qsoInfo.stnInfo.CALL}%");
                    dtvQSOs = dBManager.ExecuteDataTable(CommandType.StoredProcedure, "GetvQSOs");
                } catch (Exception ex) {
                    throw new Exception($"ExecuteSPObject failed: {ex.Message}", ex);
                }
            }

            // Process results OUTSIDE lock
            List<T> items = new List<T>();
            foreach (var row in dtvQSOs.Rows) {
                T item = (T)Activator.CreateInstance(typeof(T), row);
                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// Thread-safe: Load SQL results into List of objects using reflection
        /// OPTIMIZED: Uses cached property info and column ordinals
        /// </summary>
        public List<T> LoadSQLToListOfObjects<T>(string sql, ref RetCode ret) where T : class, new() {
            ret = new RetCode();

            try {
                DataTable dt;

                lock (_dbLock) {
                    EnsureConnectionOpen();
                    dBManager.CreateParameters(0);
                    dt = dBManager.ExecuteDataTable(CommandType.Text, sql);
                }

                // Cache property info and column ordinals for performance
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanWrite)
                    .ToDictionary(
                        p => p.Name,
                        p => new {
                            Property = p,
                            ColumnOrdinal = dt.Columns.Contains(p.Name) ? dt.Columns[p.Name].Ordinal : -1
                        }
                    );

                var list = new List<T>(dt.Rows.Count);
                DataRow[] rows = dt.Select();

                foreach (DataRow row in rows) {
                    T obj = new T();

                    foreach (var prop in properties.Values) {
                        if (prop.ColumnOrdinal == -1) continue;

                        try {
                            object value = row[prop.ColumnOrdinal];
                            if (value != DBNull.Value) {
                                prop.Property.SetValue(
                                    obj,
                                    Convert.ChangeType(value, prop.Property.PropertyType),
                                    null
                                );
                            }
                        } catch {
                            continue;
                        }
                    }

                    list.Add(obj);
                }

                return list;
            } catch (Exception ex) {
                ret.Code = -1;
                ret.Msg = $"LoadSQLToListOfObjects failed: {ex.Message}";
                return new List<T>();
            }
        }

        /// <summary>
        /// Thread-safe: Convert DataTable to List using reflection
        /// NOTE: This is slower than LoadSQLToListOfObjects due to repeated reflection
        /// </summary>
        public List<T> ConvertDatatabletoListofT<T>(DataTable dt) where T : class, new() {
            List<T> list = new List<T>();

            foreach (var row in dt.AsEnumerable()) {
                T obj = new T();

                foreach (var prop in obj.GetType().GetProperties()) {
                    try {
                        PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
                        propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                    } catch {
                        continue;
                    }
                }
                list.Add(obj);
            }
            return list;
        }

        /// <summary>
        /// Thread-safe: Bulk insert DX announcements with WHERE NOT EXISTS check
        /// </summary>
        public RetCode DXAnnouncementInsertToDB(List<DXAnnouncement> DXItems) {
            RetCode ret = new RetCode();
            string currCall = "";

            try {
                lock (_dbLock) {
                    EnsureConnectionOpen();

                    foreach (DXAnnouncement item in DXItems) {
                        currCall = item.Callsign;

                        string dxccName = item.DXCCName?.Replace("'", "''");
                        string callsign = item.Callsign?.Replace("'", "''");
                        string qslVia = item.QSLVia?.Replace("'", "''");
                        string reportedBy = item.ReportedBy?.Replace("'", "''");
                        string info = item.Info?.Replace("'", "''");

                        string sql = $@"
                            INSERT INTO QSOLOG..DXAnnouncement (
                                StartDate, EndDate, DXccPrefix, DXCCName, Callsign, 
                                QSLVia, ReportedBy, ReportedDate, Info, IsActive
                            )
                            SELECT 
                                '{item.StartDate:yyyy-MM-dd}',
                                '{item.EndDate:yyyy-MM-dd}',
                                '{item.DXCCPrefix}',
                                '{dxccName}',
                                '{callsign}',
                                {(qslVia == null ? "NULL" : $"'{qslVia}'")},
                                {(reportedBy == null ? "NULL" : $"'{reportedBy}'")},
                                {(item.ReportedDate.HasValue ? $"'{item.ReportedDate.Value:yyyy-MM-dd}'" : "NULL")},
                                {(info == null ? "NULL" : $"'{info}'")},
                                {(item.IsActive ? "1" : "0")}
                            WHERE NOT EXISTS (
                                SELECT 1 FROM QSOLOG..DXAnnouncement 
                                WHERE Callsign = '{callsign}' 
                                  AND StartDate = '{item.StartDate:yyyy-MM-dd}'
                            )";

                        dBManager.ExecuteNonQuery(CommandType.Text, sql);
                    }
                }

                ret.Code = 0;
                ret.IntValue = DXItems.Count;
                ret.Msg = $"Successfully processed {DXItems.Count} DX announcements";
            } catch (Exception ex) {
                ret.Code = -1;
                ret.Msg = $"Error inserting DX announcements at '{currCall}': {ex.Message}";
            }

            return ret;
        }




        public RetCode GetDXAnnouncement(DateTime dateStart, DateTime dateEnd,ref List<DXAnnouncement> dxList) {
            RetCode ret = new RetCode();
            DataTable dt = null;

            dBManager.CreateParameters(2); //   
            dBManager.AddParameters(0,"@StartDate",$"{dateStart.ToString("yyyy-MM-dd")}");
            dBManager.AddParameters(1,"@EndDate",$"{dateEnd.ToString("yyyy-MM-dd")}");
            dt=dBManager.ExecuteDataTable(System.Data.CommandType.StoredProcedure,"QSOLOG..GetDXAnnouncement");
            if(dt==null ||dt.Rows.Count==0) {
                dxList=new List<DXAnnouncement>();//blank list
                return new RetCode { Code=-1,Msg=$"No data" };
            }
            dxList=        ConvertDatatabletoListofT<DXAnnouncement>(dt);
            return ret;
        }


        // ========== OBSOLETE METHODS (KEPT FOR BACKWARDS COMPATIBILITY) ==========

        [Obsolete("Use LoadSQLToListOfObjects<T> instead - this method uses inefficient reflection")]
        public IEnumerable<T> LoadSQLToListOfObjectsNOTUSED_KEEP<T>(string sql, ref RetCode ret) {
            ret = new RetCode();
            List<T> items = new List<T>();

            try {
                DataTable dt;

                lock (_dbLock) {
                    EnsureConnectionOpen();
                    dt = dBManager.ExecuteDataTable(CommandType.Text, sql);
                }

                foreach (var row in dt.Rows) {
                    T item = (T)Activator.CreateInstance(typeof(T), row);
                    items.Add(item);
                }
            } catch (Exception ex) {
                ret = new RetCode { Code = -1, Msg = ex.Message };
            }
            return items;
        }

        [Obsolete("Use LoadSQLToListOfObjects<T> instead - this method has incorrect constructor signature")]
        public IEnumerable<T> LoadSQLToListOfObjectsEXCEPTION<T>(string sql, ref RetCode ret) {
            ret = new RetCode();
            List<T> items = new List<T>();

            try {
                DataTable dt;

                lock (_dbLock) {
                    EnsureConnectionOpen();
                    dt = dBManager.ExecuteDataTable(CommandType.Text, sql);
                }

                foreach (var row in dt.Rows) {
                    T item = (T)Activator.CreateInstance(typeof(T), row, new object[0]);
                    items.Add(item);
                }
            } catch (Exception ex) {
                ret = new RetCode { Code = -1, Msg = ex.Message };
            }
            return items;
        }
    }
}