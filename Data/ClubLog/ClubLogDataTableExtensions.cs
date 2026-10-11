using System;
using System.Collections.Generic;
using System.Data;

namespace ClubLogXmlParser {
    public static class ClubLogDataTableExtensions {
        // Helper to convert nullable types to DBNull.Value
        private static object ToDbNull<T>(T? value) where T : struct
            => value.HasValue ? value.Value : DBNull.Value;

        private static object ToDbNull(string? value)
            => string.IsNullOrEmpty(value) ? DBNull.Value : value;

        // 1. Entities -> rbn.clEntity
        public static DataTable ToDataTable(this IEnumerable<ClubLogEntity> items) {
            var dt = new DataTable("clEntity");
            dt.Columns.Add("Adif",typeof(short));
            dt.Columns.Add("Name",typeof(string));
            dt.Columns.Add("Prefix",typeof(string));
            dt.Columns.Add("Deleted",typeof(bool));
            dt.Columns.Add("Cqz",typeof(byte)).AllowDBNull = true;
            dt.Columns.Add("Cont",typeof(string)).AllowDBNull = true;
            dt.Columns.Add("Longitude",typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("Latitude",typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("Start",typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("End",typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("Whitelist",typeof(bool)).AllowDBNull = true;
            dt.Columns.Add("WhitelistStart",typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("WhitelistEnd",typeof(DateTime)).AllowDBNull = true;

            foreach(var item in items) {
                dt.Rows.Add(
                    (short)item.Adif,
                    item.Name,
                    item.Prefix,
                    item.Deleted,
                    ToDbNull(item.Cqz),
                    ToDbNull(item.Cont),
                    ToDbNull(item.Longitude),
                    ToDbNull(item.Latitude),
                    ToDbNull(item.Start),
                    ToDbNull(item.End),
                    ToDbNull(item.Whitelist),
                    ToDbNull(item.WhitelistStart),
                    ToDbNull(item.WhitelistEnd)
                );
            }
            return dt;
        }

        // 2. Exceptions -> rbn.clException
        public static DataTable ToDataTable(this IEnumerable<ClubLogException> items) {
            var dt = new DataTable("clException");
            dt.Columns.Add("RecordId",typeof(int));
            dt.Columns.Add("Call",typeof(string));
            dt.Columns.Add("Entity",typeof(string));
            dt.Columns.Add("Adif",typeof(short));
            dt.Columns.Add("Cqz",typeof(byte)).AllowDBNull = true;
            dt.Columns.Add("Cont",typeof(string)).AllowDBNull = true;
            dt.Columns.Add("Longitude",typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("Latitude",typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("Start",typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("End",typeof(DateTime)).AllowDBNull = true;

            foreach(var item in items) {
                dt.Rows.Add(
                    (int)item.Record,
                    item.Call,
                    item.Entity,
                    (short)item.Adif,
                    ToDbNull(item.Cqz),
                    ToDbNull(item.Cont),
                    ToDbNull(item.Longitude),
                    ToDbNull(item.Latitude),
                    ToDbNull(item.Start),
                    ToDbNull(item.End)
                );
            }
            return dt;
        }

        // 3. Prefixes -> rbn.clPrefix
        public static DataTable ToDataTable(this IEnumerable<ClubLogPrefix> items) {
            var dt = new DataTable("clPrefix");
            dt.Columns.Add("RecordId",typeof(int));
            dt.Columns.Add("Call",typeof(string));
            dt.Columns.Add("Entity",typeof(string));
            dt.Columns.Add("Adif",typeof(short));
            dt.Columns.Add("Cqz",typeof(byte)).AllowDBNull = true;
            dt.Columns.Add("Cont",typeof(string)).AllowDBNull = true;
            dt.Columns.Add("Longitude",typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("Latitude",typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("Start",typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("End",typeof(DateTime)).AllowDBNull = true;

            foreach(var item in items) {
                dt.Rows.Add(
                    (int)item.Record,
                    item.Call,
                    item.Entity,
                    (short)item.Adif,
                    ToDbNull(item.Cqz),
                    ToDbNull(item.Cont),
                    ToDbNull(item.Longitude),
                    ToDbNull(item.Latitude),
                    ToDbNull(item.Start),
                    ToDbNull(item.End)
                );
            }
            return dt;
        }

        // 4. Invalid Operations -> rbn.clInvalidOperation
        public static DataTable ToDataTable(this IEnumerable<ClubLogInvalidOperation> items) {
            var dt = new DataTable("clInvalidOperation");
            dt.Columns.Add("RecordId",typeof(int));
            dt.Columns.Add("Call",typeof(string));
            dt.Columns.Add("Start",typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("End",typeof(DateTime)).AllowDBNull = true;

            foreach(var item in items) {
                dt.Rows.Add(
                    (int)item.Record,
                    item.Call,
                    ToDbNull(item.Start),
                    ToDbNull(item.End)
                );
            }
            return dt;
        }

        // 5. Satellites -> rbn.clSatelite
        public static DataTable ToDataTable(this IEnumerable<ClubLogSatellite> items) {
            var dt = new DataTable("clSatelite");
            dt.Columns.Add("RecordId",typeof(int));
            dt.Columns.Add("SatId",typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("Name",typeof(string));
            dt.Columns.Add("Description",typeof(string));

            foreach(var item in items) {
                dt.Rows.Add(
                    (int)item.Record,
                    ToDbNull(item.SatId),
                    item.Name,
                    item.Description
                );
            }
            return dt;
        }

        // 6. Zone Exceptions -> rbn.clZoneException
        public static DataTable ToDataTable(this IEnumerable<ClubLogZoneException> items) {
            var dt = new DataTable("clZoneException");
            dt.Columns.Add("RecordId",typeof(int));
            dt.Columns.Add("Call",typeof(string));
            dt.Columns.Add("Zone",typeof(byte));
            dt.Columns.Add("Start",typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("End",typeof(DateTime)).AllowDBNull = true;

            foreach(var item in items) {
                dt.Rows.Add(
                    (int)item.Record,
                    item.Call,
                    item.Zone,
                    ToDbNull(item.Start),
                    ToDbNull(item.End)
                );
            }
            return dt;
        }
    }
}