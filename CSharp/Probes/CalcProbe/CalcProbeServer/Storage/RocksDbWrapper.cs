using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RocksDbSharp;

namespace CalcProbeServer.Storage
{
    public class RocksDbWrapper
    {
        private readonly string _databaseRootFolder;
        private readonly DbOptions _options;

        public RocksDbWrapper(string databaseRootFolder)
        {
            _databaseRootFolder = databaseRootFolder;
            Directory.CreateDirectory(_databaseRootFolder);

            _options = new DbOptions().
                SetCreateIfMissing(true).
                SetAllowMmapWrites(false).
                SetAllowMmapReads(true);
        }
        public void WriteAll(IEnumerable<byte[]> data, string dbName, string tableName = null)
        {
            var rocksPath = Path.Combine(_databaseRootFolder, dbName);
            Directory.CreateDirectory(rocksPath);

            using var db = RocksDb.Open(_options, rocksPath);
            db.CreateColumnFamily(new ColumnFamilyOptions().
                SetCompression(Compression.Snappy), tableName ?? "data");

            var wb = new WriteBatch();

            var i = 0L;
            const long batchFlushInterval = 100_000;
            foreach (var d in data)
            {
                i++;
                wb.Put(ToKey(i), d);
                if (i % batchFlushInterval == 0L)
                {
                    db.Write(wb);
                    wb.Dispose();
                    wb = new WriteBatch();
                }
            }

            db.Write(wb);
            wb.Dispose();

            db.CompactRange("k", "l");
        }

        public IEnumerable<(long, byte[])> ReadAll(string dbName, string tableName = null)
        {
            var rocksPath = Path.Combine(_databaseRootFolder, dbName);
            using var db = RocksDb.OpenReadOnly(_options, rocksPath, false);

            var cf = db.GetColumnFamily(tableName ?? "data");

            using var iter = db.NewIterator(cf);
            iter.SeekToFirst();
            while (iter.Valid())
            {
                yield return (FromKey(iter.Key()), iter.Value());
                iter.Next();
            }
        }
        
        public IEnumerable<(long, byte[])> ReadPage(string dbName, long startFrom, uint pageSize, string tableName = null)
        {
            var rocksPath = Path.Combine(_databaseRootFolder, dbName);
            using var db = RocksDb.OpenReadOnly(_options, rocksPath, false);

            var cf = db.GetColumnFamily(tableName ?? "data");

            using var iter = db.NewIterator(cf);
            iter.Seek(ToKey(startFrom));
            for (var i = 0; i < pageSize && iter.Valid(); i++, iter.Next())
            {
                yield return (FromKey(iter.Key()), iter.Value());
            }
        }
        
        private static byte[] ToKey(long i) => BitConverter.GetBytes(IPAddress.HostToNetworkOrder(i));
        private static long FromKey(byte[] key) => IPAddress.NetworkToHostOrder(BitConverter.ToInt64(key));
    }
}
