using System.Data;
using System.Text;
using Common;
using Data;
using CCSpotViewer.Models;

namespace CCSpotViewer.Services;

/// <summary>
/// Inserts parsed <see cref="CC11SpotMessage"/> rows into [rbn].[CC11SpotMessage]
/// using a dynamic parameterised SQL INSERT (no stored procedure).
/// Mirrors the lock(DataEtl.dBManager) pattern from DXSpiderTelnet.
/// </summary>
public sealed class CC11SpotDbService:IDisposable {
    private readonly DataETLBase _dataEtl;
    private bool _disposed;

    public bool IsAvailable => !_disposed && _dataEtl?.dBManager != null;

    // Pre-built INSERT statement — columns match [rbn].[CC11SpotMessage] exactly.
    // SpotId is IDENTITY — never inserted. Band is a model-only field — not in table.
    private const string InsertSql = @"
INSERT INTO [rbn].[CC11SpotMessage]
(
    [MessageType], [FrequencyKHz], [DxCallsign], [UTC], [Comments],
    [SpotterCallsign], [Hops], [RxViaNode], [OriginNode],
    [DxItuZone], [DxCqZone], [SpotterItuZone], [SpotterCqZone],
    [DxState], [SpotterState], [DxDxccPrefix], [SpotterDxccPrefix],
    [DxGrid], [SpotterGrid], [InternalIdentifier], [Coordinates],
    [RoutingNode], [Type], [Mode], [Db], [Wpm], [Activity]
)
VALUES
(
    @MessageType, @FrequencyKHz, @DxCallsign, @UTC, @Comments,
    @SpotterCallsign, @Hops, @RxViaNode, @OriginNode,
    @DxItuZone, @DxCqZone, @SpotterItuZone, @SpotterCqZone,
    @DxState, @SpotterState, @DxDxccPrefix, @SpotterDxccPrefix,
    @DxGrid, @SpotterGrid, @InternalIdentifier, @Coordinates,
    @RoutingNode, @Type, @Mode, @Db, @Wpm, @Activity
);";

    public CC11SpotDbService(string connectionString) {
        _dataEtl = new DataETLBase(connectionString);
    }

    /// <summary>
    /// Inserts a single CC11 spot using a dynamic parameterised SQL INSERT.
    /// Returns RetCode — Code 0 = success.  Safe to call from any thread.
    /// </summary>
    public RetCode InsertCC11Spot(CC11SpotMessage spot) {
        RetCode ret = new();

        if(!IsAvailable) {
            ret.Code = -1;
            ret.Msg  = "DB manager not initialised.";
            return ret;
        }

        try {
            lock(_dataEtl.dBManager) {
                _dataEtl.dBManager.CreateParameters(27);

                // ── p0  MessageType  varchar(6) ───────────────────────────────
                _dataEtl.dBManager.AddParameters(0,"@MessageType",spot.MessageType);

                // ── p1  FrequencyKHz  decimal(10,2) ──────────────────────────
                _dataEtl.dBManager.AddParameters(1,"@FrequencyKHz",spot.Frequency);

                // ── p2  DxCallsign  varchar(16) ───────────────────────────────
                _dataEtl.dBManager.AddParameters(2,"@DxCallsign",spot.DxCallsign);

                // ── p3  UTC  datetime2(0) NULL ────────────────────────────────
                // spot.UTC is set by the parser via Util.CombineToUtcDateTime
                _dataEtl.dBManager.AddParameters(3,"@UTC",(object?)spot.UTC ?? DBNull.Value);

                // ── p4  Comments  nvarchar(50) NULL ───────────────────────────
                _dataEtl.dBManager.AddParameters(4,"@Comments",NullIfEmpty(spot.Comments));

                // ── p5  SpotterCallsign  varchar(16) ──────────────────────────
                _dataEtl.dBManager.AddParameters(5,"@SpotterCallsign",spot.SpotterCallsign);

                // ── p6  Hops  int NULL ────────────────────────────────────────
                _dataEtl.dBManager.AddParameters(6,"@Hops",spot.Hops);

                // ── p7  RxViaNode  varchar(30) NULL ───────────────────────────
                _dataEtl.dBManager.AddParameters(7,"@RxViaNode",NullIfEmpty(spot.RxViaNode));

                // ── p8  OriginNode  varchar(30) NULL ──────────────────────────
                _dataEtl.dBManager.AddParameters(8,"@OriginNode",NullIfEmpty(spot.OriginNode));

                // ── p9  DxItuZone  smallint NULL ──────────────────────────────
                _dataEtl.dBManager.AddParameters(9,"@DxItuZone",NullIfZero(spot.DxItuZone));

                // ── p10 DxCqZone  smallint NULL ───────────────────────────────
                _dataEtl.dBManager.AddParameters(10,"@DxCqZone",NullIfZero(spot.DxCqZone));

                // ── p11 SpotterItuZone  smallint NULL ─────────────────────────
                _dataEtl.dBManager.AddParameters(11,"@SpotterItuZone",NullIfZero(spot.SpotterItuZone));

                // ── p12 SpotterCqZone  smallint NULL ──────────────────────────
                _dataEtl.dBManager.AddParameters(12,"@SpotterCqZone",NullIfZero(spot.SpotterCqZone));

                // ── p13 DxState  varchar(10) NULL ─────────────────────────────
                _dataEtl.dBManager.AddParameters(13,"@DxState",NullIfEmpty(spot.DxState));

                // ── p14 SpotterState  varchar(10) NULL ────────────────────────
                _dataEtl.dBManager.AddParameters(14,"@SpotterState",NullIfEmpty(spot.SpotterState));

                // ── p15 DxDxccPrefix  varchar(5) NULL ─────────────────────────
                _dataEtl.dBManager.AddParameters(15,"@DxDxccPrefix",NullIfEmpty(spot.DxDxccPrefix));

                // ── p16 SpotterDxccPrefix  varchar(10) NULL ───────────────────
                _dataEtl.dBManager.AddParameters(16,"@SpotterDxccPrefix",NullIfEmpty(spot.SpotterDxccPrefix));

                // ── p17 DxGrid  varchar(8) NULL ───────────────────────────────
                _dataEtl.dBManager.AddParameters(17,"@DxGrid",NullIfEmpty(spot.DxGrid));

                // ── p18 SpotterGrid  varchar(8) NULL ──────────────────────────
                _dataEtl.dBManager.AddParameters(18,"@SpotterGrid",NullIfEmpty(spot.SpotterGrid));

                // ── p19 InternalIdentifier  varchar(30) NULL ──────────────────
                // Model stores as int; table column is varchar(30)
                _dataEtl.dBManager.AddParameters(19,"@InternalIdentifier",spot.InternalIdentifier > 0
                                                         ? (object)spot.InternalIdentifier.ToString()
                                                         : DBNull.Value);

                // ── p20 Coordinates  varchar(30) NULL ─────────────────────────
                _dataEtl.dBManager.AddParameters(20,"@Coordinates",NullIfEmpty(spot.Coordinates));

                // ── p21 RoutingNode  varchar(30) NULL ─────────────────────────
                _dataEtl.dBManager.AddParameters(21,"@RoutingNode",NullIfEmpty(spot.RoutingNode));

                // ── p22 Type  varchar(3) NULL ─────────────────────────────────
                _dataEtl.dBManager.AddParameters(22,"@Type",NullIfEmpty(spot.Type));

                // ── p23 Mode  varchar(5) NULL ─────────────────────────────────
                _dataEtl.dBManager.AddParameters(23,"@Mode",NullIfEmpty(spot.Mode));

                // ── p24 Db  int NULL ──────────────────────────────────────────
                _dataEtl.dBManager.AddParameters(24,"@Db",NullIfZero(spot.Db));

                // ── p25 Wpm  int NULL ─────────────────────────────────────────
                _dataEtl.dBManager.AddParameters(25,"@Wpm",NullIfZero(spot.Wpm));

                // ── p26 Activity  varchar(30) NULL ────────────────────────────
                _dataEtl.dBManager.AddParameters(26,"@Activity",NullIfEmpty(spot.Activity));

                _dataEtl.dBManager.ExecuteNonQuery(CommandType.Text,InsertSql);
            }
        } catch(Exception ex) {
            ret.Code = -1;
            ret.Msg  = $"{spot.DxCallsign} @ {spot.Frequency} kHz: {ex.Message}";
        }

        return ret;
    }

    // ── Helpers — convert empty/zero model values to DBNull for nullable columns ──

    private static object NullIfEmpty(string? s)
        => string.IsNullOrWhiteSpace(s) ? DBNull.Value : (object)s.Trim();

    private static object NullIfZero(int i)
        => i == 0 ? DBNull.Value : (object)i;

    public void Dispose() {
        if(_disposed) return;
        _disposed = true;
        _dataEtl?.dBManager?.Close();
    }
}