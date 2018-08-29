using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.Diagnostics;
using SenseNet.Packaging.Steps;
using SenseNet.Preview.Controller;
using SenseNet.Search;
using SenseNet.Search.Querying;
using ExecutionContext = SenseNet.Packaging.ExecutionContext;
using Retrier = SenseNet.Tools.Retrier;

namespace SenseNet.Preview.Packaging.Steps
{
    public enum CleanupMode
    {
        All,
        KeepLastVersions,
        EmptyFoldersOnly
    }
    
    public class CleanupPreviews : Step
    {
        public string Path { get; set; }
        public int MaxIndex { get; set; }
        public CleanupMode Mode { get; set; }

        private ExecutionContext _context;
        private int _folderCount;
        private int _imageCount;

        public override void Execute(ExecutionContext context)
        {
            _context = context;

            using (new Timer(state => WriteProgress(), null, 1000, 2000))
            {
                var pc = new PreviewCleaner(Path, Mode, MaxIndex);
                pc.OnFolderDeleted += OnFolderDeleted;
                pc.OnImageDeleted += OnImageDeleted;

                pc.Execute();
            }

            WriteProgress();
            _context?.Console?.WriteLine("");
        }

        private void WriteProgress()
        {
            _context?.Console?.Write($"  Deleted folders: {_folderCount}, images: {_imageCount}                        \r");
        }
        private void OnFolderDeleted(object sender, EventArgs eventArgs)
        {
            Interlocked.Increment(ref _folderCount);
        }
        private void OnImageDeleted(object sender, EventArgs eventArgs)
        {
            Interlocked.Increment(ref _imageCount);
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class PreviewCleaner
    {
        public event EventHandler OnFolderDeleted;
        public event EventHandler OnImageDeleted;

        private struct NodeInfo
        {
            public int Id;
            public string Path;
        }

        private static readonly int MaxDegreeOfParallelism = 10;
        private static readonly int BlockCount = 100;

        private readonly MemoryCache LastVersionCache = new MemoryCache("LastNodeVersions");

        private static class Scripts
        {
            internal static readonly string PREVIEWFOLDERS_ALL = @"SELECT TOP " + BlockCount + @" N.NodeId, N.Path
FROM Nodes as N 
INNER JOIN dbo.SchemaPropertySets AS T ON N.NodeTypeId = T.PropertySetId
WHERE T.Name = 'SystemFolder'
	AND (N.Path like '%/Previews/V%' OR N.Name = 'Previews')
	{0}
ORDER BY N.NodeId";

            internal static readonly string PREVIEWFOLDERS_ROOT = @"SELECT TOP " + BlockCount + @" N.NodeId, N.Path
FROM Nodes as N 
INNER JOIN dbo.SchemaPropertySets AS T ON N.NodeTypeId = T.PropertySetId
WHERE T.Name = 'SystemFolder'
	AND (N.Name = 'Previews')
	{0}
ORDER BY N.NodeId";

            internal const string EMPTYFOLDER =
                @" AND (select count(0) from Nodes where ParentNodeId = N.NodeId) = 0";

            internal static readonly string PREVIEWIMAGES =
                @"SELECT TOP " + BlockCount + @" N.NodeId FROM Nodes as N 
INNER JOIN dbo.SchemaPropertySets AS T ON N.NodeTypeId = T.PropertySetId
WHERE T.Name = 'PreviewImage'
    {0}
ORDER BY N.NodeId";

            internal static readonly string LASTVERSIONS = @"
                    SELECT N.NodeId, N.Path, V.VersionId, V.MajorNumber, V.MinorNumber, V.Status, 
	LastMajor = CASE 
		WHEN V.VersionId = N.LastMajorVersionId THEN CAST(1 AS BIT)
		ELSE CAST(0 AS BIT)
		END,
	LastMinor = CASE 
		WHEN V.VersionId = N.LastMinorVersionId THEN CAST(1 AS BIT)
		ELSE CAST(0 AS BIT)
		END
FROM Versions V join Nodes N on V.NodeId = N.NodeId
WHERE (N.Path = @Path) AND (V.VersionId = N.LastMajorVersionId OR V.VersionId = N.LastMinorVersionId)
ORDER BY MajorNumber, MinorNumber";
        }

        //========================================================================================= Properties

        private string Path { get; }
        private int MaxIndex { get; }
        private CleanupMode Mode { get; }

        //========================================================================================= Constructors

        internal PreviewCleaner(string path = null, CleanupMode cleanupMode = CleanupMode.All, int maxIndex = 0)
        {
            Path = path;
            MaxIndex = maxIndex;
            Mode = cleanupMode;
        }

        //========================================================================================= Public API

        internal void Execute()
        {
            if (Mode != CleanupMode.EmptyFoldersOnly)
            {
                // Delete folders only if we have to delete:
                //      1. everything or
                //      2. old versions
                if (MaxIndex == 0 || Mode == CleanupMode.KeepLastVersions)
                    DeletePreviewFolders();

                // Keep the first few images, delete the rest one by one.
                // All unneeded folders (for old versions) are already deleted at this point.
                if (MaxIndex > 0)
                    DeletePreviewImages();
            }

            DeleteEmptyPreviewFolders();
        }

        //========================================================================================= Internal methods

        #region Delete preview images

        private void DeletePreviewImages()
        {
            var blockCount = 0;
            var lastDeletedId = 0;

            while (DeletePreviewImagesBlock(Path, MaxIndex, lastDeletedId, out lastDeletedId))
            {
                var message = $"Preview image delete block {blockCount++} finished.";
                SnTrace.Database.Write(message);
            }
        }
        private bool DeletePreviewImagesBlock(string path, int maxIndex, int minimumNodeId, out int lastDeletedId)
        {
            lastDeletedId = 0;

            if (!LoadPreviewImagesBlock(path, maxIndex, minimumNodeId, out var buffer))
                return false;

            var workerBlock = new ActionBlock<int>(
                async imageId =>
                {
                    await DeleteContentFromDb(imageId);
                    OnImageDeleted?.Invoke(imageId, EventArgs.Empty);
                },
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism });

            foreach (var image in buffer)
                workerBlock.Post(image.Id);

            lastDeletedId = buffer.Max(n => n.Id);

            workerBlock.Complete();
            workerBlock.Completion.Wait();

            return true;
        }
        private static bool LoadPreviewImagesBlock(string path, int maxIndex, int minimumNodeId, out List<NodeInfo> buffer)
        {
            var block = new List<NodeInfo>();
            Retrier.Retry(3, 1000, typeof(Exception), () => { block = LoadPreviewImagesBlockInternal(path, maxIndex, minimumNodeId); });

            buffer = block;

            return block.Any();
        }
        private static List<NodeInfo> LoadPreviewImagesBlockInternal(string path, int maxIndex, int minimumNodeId)
        {
            var filters = new StringBuilder();
            var parameters = new List<IDataParameter>();

            if (!string.IsNullOrEmpty(path))
            {
                filters.AppendLine(" AND Path like @PathStart");
                parameters.Add(new SqlParameter("@PathStart", SqlDbType.NVarChar) { Value = path.TrimEnd('/') + "/%" });
            }

            if (maxIndex > 0)
            {
                filters.AppendLine(" AND N.[Index] > @MaxIndex");
                parameters.Add(new SqlParameter("@MaxIndex", SqlDbType.Int) { Value = maxIndex });
            }

            if (minimumNodeId > 0)
            {
                filters.AppendLine(" AND NodeId > @MinimumNodeId");
                parameters.Add(new SqlParameter("@MinimumNodeId", SqlDbType.Int) { Value = minimumNodeId });
            }

            return LoadData(string.Format(Scripts.PREVIEWIMAGES, filters), parameters.ToArray());
        }

        #endregion

        #region Delete preview folders
        
        private void DeletePreviewFolders()
        {
            var blockCount = 0;
            var lastDeletedId = 0;
            var keepLastVersions = Mode == CleanupMode.KeepLastVersions;

            while (DeletePreviewFoldersBlock(Path, keepLastVersions, lastDeletedId, out lastDeletedId))
            {
                var message = $"Preview folder delete block {blockCount++} finished.";
                SnTrace.Database.Write(message);
            }
        }
        private bool DeletePreviewFoldersBlock(string path, bool keepLastVersions, int minimumNodeId, out int lastDeletedId)
        {
            lastDeletedId = 0;

            if (!LoadPreviewFoldersBlock(path, keepLastVersions, minimumNodeId, out var buffer))
                return false;

            var pathBag = new ConcurrentBag<string>();
            var workerBlock = new ActionBlock<NodeInfo>(
                async node =>
                {
                    if (!keepLastVersions || IsFolderDeletable(node.Path))
                    {
                        await DeleteContentFromDb(node.Id);
                        OnFolderDeleted?.Invoke(node.Id, EventArgs.Empty);

                        if (!string.IsNullOrEmpty(node.Path))
                            pathBag.Add(node.Path);
                    }
                },
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism });

            foreach (var folder in buffer)
            {
                workerBlock.Post(folder);
            }

            lastDeletedId = buffer.Max(n => n.Id);

            workerBlock.Complete();
            workerBlock.Completion.Wait();

            SnTrace.Index.Write("Removing preview folder block from the index.");

            IndexManager.IndexingEngine.WriteIndex(pathBag.SelectMany(p => new[]
                {
                    new SnTerm(IndexFieldName.InTree, p),
                    new SnTerm(IndexFieldName.Path, p)
                }),
                null, null);

            return true;
        }
        private static bool LoadPreviewFoldersBlock(string path, bool keepLastVersions, int minimumNodeId, out List<NodeInfo> buffer)
        {
            var block = new List<NodeInfo>();
            Retrier.Retry(3, 1000, typeof(Exception), () => { block = LoadPreviewFoldersBlockInternal(path, keepLastVersions, minimumNodeId); });

            buffer = block;

            return block.Any();
        }
        private static List<NodeInfo> LoadPreviewFoldersBlockInternal(string path, bool keepLastVersions, int minimumNodeId)
        {
            var filters = new StringBuilder();
            var parameters = new List<IDataParameter>();

            if (!string.IsNullOrEmpty(path))
            {
                filters.AppendLine(" AND Path like @PathStart");
                parameters.Add(new SqlParameter("@PathStart", SqlDbType.NVarChar) { Value = path.TrimEnd('/') + "/%" });
            }
            if (minimumNodeId > 0)
            {
                filters.AppendLine(" AND NodeId > @MinimumNodeId");
                parameters.Add(new SqlParameter("@MinimumNodeId", SqlDbType.Int) { Value = minimumNodeId });
            }

            var script = keepLastVersions
                ? string.Format(Scripts.PREVIEWFOLDERS_ALL, filters)
                : string.Format(Scripts.PREVIEWFOLDERS_ROOT, filters);

            return LoadData(script, parameters.ToArray());
        }

        #endregion

        #region Delete empty preview folders

        private void DeleteEmptyPreviewFolders()
        {
            var blockCount = 0;

            while (DeleteEmptyPreviewFoldersBlock(Path))
            {
                var message = $"Preview folder delete block {blockCount++} finished.";
                SnTrace.Database.Write(message);
            }
        }
        private bool DeleteEmptyPreviewFoldersBlock(string path)
        {
            if (!LoadEmptyPreviewFoldersBlock(path, out var buffer))
                return false;

            var pathBag = new ConcurrentBag<string>();
            var workerBlock = new ActionBlock<NodeInfo>(
                async node =>
                {
                    await DeleteContentFromDb(node.Id);
                    OnFolderDeleted?.Invoke(node.Id, EventArgs.Empty);

                    if (!string.IsNullOrEmpty(node.Path))
                        pathBag.Add(node.Path);
                },
                new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = MaxDegreeOfParallelism });

            foreach (var folder in buffer)
            {
                workerBlock.Post(folder);
            }

            workerBlock.Complete();
            workerBlock.Completion.Wait();

            SnTrace.Index.Write("Removing preview folder block from the index.");

            IndexManager.IndexingEngine.WriteIndex(pathBag.SelectMany(p => new[]
                {
                    new SnTerm(IndexFieldName.InTree, p),
                    new SnTerm(IndexFieldName.Path, p)
                }),
                null, null);

            return true;
        }
        private static bool LoadEmptyPreviewFoldersBlock(string path, out List<NodeInfo> buffer)
        {
            var block = new List<NodeInfo>();
            Retrier.Retry(3, 1000, typeof(Exception), () => { block = LoadEmptyPreviewFoldersBlockInternal(path); });

            buffer = block;

            return block.Any();
        }
        private static List<NodeInfo> LoadEmptyPreviewFoldersBlockInternal(string path)
        {
            var filters = new StringBuilder(Scripts.EMPTYFOLDER);
            var parameters = new List<IDataParameter>();

            if (!string.IsNullOrEmpty(path))
            {
                filters.AppendLine(" AND Path like @PathStart");
                parameters.Add(new SqlParameter("@PathStart", SqlDbType.NVarChar) { Value = path.TrimEnd('/') + "/%" });
            }

            return LoadData(string.Format(Scripts.PREVIEWFOLDERS_ALL, filters), parameters.ToArray());
        }

        #endregion

        #region Database operations

        private static async Task DeleteContentFromDb(int nodeId)
        {
            await Retrier.RetryAsync(3, 3000, () => DeleteContentFromDbInternalAsync(nodeId), (counter, exc) =>
            {
                if (exc == null)
                    return true;

                SnTrace.Database.WriteError($"Content {nodeId} could not be deleted. {exc.Message}");
                return false;
            });
        }
        private static async Task DeleteContentFromDbInternalAsync(int nodeId)
        {
            using (var proc = (SqlProcedure)DataProvider.CreateDataProcedure("proc_Node_DeletePhysical"))
            {
                proc.CommandType = CommandType.StoredProcedure;
                proc.Parameters.Add(new SqlParameter("@NodeId", SqlDbType.Int) { Value = nodeId });
                proc.Parameters.Add(new SqlParameter("@Timestamp", SqlDbType.Int) { Value = DBNull.Value });

                await proc.ExecuteNonQueryAsync();

                SnTrace.Database.Write($"Content {nodeId} DELETED from database.");
            }
        }

        private static Tuple<VersionNumber, VersionNumber> GetLastVersions(string path)
        {
            VersionNumber majorVersion = null;
            VersionNumber minorVersion = null;

            SqlProcedure cmd = null;
            SqlDataReader reader = null;

            try
            {
                cmd = new SqlProcedure
                {
                    CommandText = Scripts.LASTVERSIONS,
                    CommandType = CommandType.Text
                };
                cmd.Parameters.Add("@Path", SqlDbType.NVarChar, 450).Value = path;
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var majorNumber = reader.GetInt16(reader.GetOrdinal("MajorNumber"));
                    var minorNumber = reader.GetInt16(reader.GetOrdinal("MinorNumber"));
                    var statusCode = reader.GetInt16(reader.GetOrdinal("Status"));
                    var isLastMajor = reader.GetBoolean(reader.GetOrdinal("LastMajor"));
                    var isLastMinor = reader.GetBoolean(reader.GetOrdinal("LastMinor"));

                    var versionNumber = new VersionNumber(majorNumber, minorNumber, (VersionStatus)statusCode);

                    if (isLastMajor)
                        majorVersion = versionNumber;
                    if (isLastMinor)
                        minorVersion = versionNumber;
                }

            }
            finally
            {
                reader?.Dispose();
                cmd?.Dispose();
            }

            return new Tuple<VersionNumber, VersionNumber>(majorVersion, minorVersion);
        }

        private static List<NodeInfo> LoadData(string script, params IDataParameter[] parameters)
        {
            var buffer = new List<NodeInfo>();

            using (var proc = DataProvider.CreateDataProcedure(script))
            {
                proc.CommandType = CommandType.Text;

                if (parameters != null)
                {
                    foreach (var dataParameter in parameters)
                    {
                        proc.Parameters.Add(dataParameter);
                    }
                }

                using (var reader = proc.ExecuteReader())
                {
                    if (!reader.HasRows)
                        return buffer;

                    while (reader.Read())
                    {
                        buffer.Add(GetNodeInfo(reader));
                    }
                }

                return buffer;
            }
        }

        #endregion

        //========================================================================================= Helper methods

        private static NodeInfo GetNodeInfo(IDataReader reader)
        {
            var node = new NodeInfo { Id = reader.GetSafeInt32(0) };
            if (reader.FieldCount > 1)
                node.Path = reader.GetSafeString(1);

            return node;
        }
        private bool IsFolderDeletable(string path)
        {
            var name = path.Substring(path.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase) + 1);
            if (!name.StartsWith("V"))
                return false;

            if (!VersionNumber.TryParse(name, out var version))
                return false;

            // do not delete locked versions
            if (version.Status == VersionStatus.Locked)
                return false;

            VersionNumber lastMajor;
            VersionNumber lastMinor;

            // look up the content (most likely a file) that this preview folder is related to
            var contentPath = path.GetParentPath().GetParentPath();

            // get cached last versions for the content if possible
            if (!(LastVersionCache.Get(contentPath) is VersionNumber[] versions))
            {
                // not in cache, load from database
                var versionNumbers = GetLastVersions(contentPath);
                lastMajor = versionNumbers.Item1;
                lastMinor = versionNumbers.Item2;
                versions = new[] {lastMajor, lastMinor};

                LastVersionCache.Set(contentPath, versions, ObjectCache.InfiniteAbsoluteExpiration);
            }
            else
            {
                lastMajor = versions[0];
                lastMinor = versions[1];
            }

            return version != lastMajor && version != lastMinor;
        }
    }
}