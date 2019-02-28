using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Preview.Controller
{
    public static class PreviewCommentActions
    {
        private static readonly object DeleteResultModified = new {modified = true};
        private static readonly object DeleteResultNotModified = new {modified = false};

        [ODataFunction]
        public static object GetPreviewComments(Content content, int page)
        {
            if (!(content?.ContentHandler is File))
                return null;

            return GetPreviewComments(((File)content.ContentHandler).PreviewComments, page)
                .Select(cd => cd.ToPreviewComment());
        }
        [ODataAction]
        public static object AddPreviewComment(Content content, int page, int x, int y, string text)
        {
            AssertCommentFeature(content);

            var commentsArray = AddPreviewComment(((File)content.ContentHandler).PreviewComments, 
                User.Current.Username, page, x, y, text, out var comment);

            SaveComments(content, commentsArray);

            return comment.ToPreviewComment();
        }
        [ODataAction]
        public static object DeletePreviewComment(Content content, string id)
        {
            AssertCommentFeature(content);

            var commentsArray = DeletePreviewComment(((File)content.ContentHandler).PreviewComments, id,
                out var modified);

            if (!modified)
                return DeleteResultNotModified;

            SaveComments(content, commentsArray);

            return DeleteResultModified;
        }

        internal static IEnumerable<PreviewCommentData> GetPreviewComments(string comments, int page)
        {
            if (string.IsNullOrEmpty(comments))
                return new PreviewCommentData[0];

            var commentsArray = (JArray)JsonConvert.DeserializeObject(comments);

            return commentsArray
                .Where(c => page < 0 || c["page"].Value<int>() == page)
                .Select(c => c.ToObject<PreviewCommentData>());
        }
        internal static JArray AddPreviewComment(string comments, string userName, int page, int x, int y, 
            string text, out PreviewCommentData commentData)
        {
            if (string.IsNullOrEmpty(userName) || page < 0 || x < 0 || y < 0)
                throw new InvalidOperationException("Incorrect comment parameter.");

            if (string.IsNullOrEmpty(comments))
                comments = "[]";

            var commentsArray = (JArray)JsonConvert.DeserializeObject(comments);

            commentData = new PreviewCommentData
            {
                Id = Guid.NewGuid().ToString(),
                CreatedBy = userName,
                CreationDate = DateTime.UtcNow,
                Page = page,
                X = x,
                Y = y,
                Text = text?.Substring(0, Math.Min(text.Length, 500))
            };

            var index = 0;

            // Find the correct place to insert the comment to. The order is determined
            // by the page and the comment coordinates inside the page.
            foreach (var jToken in commentsArray)
            {
                var currentPage = jToken["page"]?.Value<int>() ?? 0;
                var currentX = jToken["x"]?.Value<int>() ?? 0;
                var currentY = jToken["y"]?.Value<int>() ?? 0;

                // current comment comes before the new one
                if (currentPage < page || currentPage == page && (currentY < y || currentY == y && currentX <= x))
                {
                    index++;
                    continue;
                }

                break;
            }

            commentsArray.Insert(index, JToken.FromObject(commentData));

            return commentsArray;
        }
        internal static JArray DeletePreviewComment(string comments, string id, out bool modified)
        {
            modified = false;

            if (string.IsNullOrEmpty(comments) || string.IsNullOrEmpty(id))
                return null;

            var commentsArray = (JArray)JsonConvert.DeserializeObject(comments);
            var comment = commentsArray?.FirstOrDefault(c =>
                string.Equals(id, c["id"]?.Value<string>(), StringComparison.InvariantCultureIgnoreCase));

            if (comment == null)
                return commentsArray;

            commentsArray.Remove(comment);

            modified = true;

            return commentsArray;
        }

        private static void SaveComments(Content content, JToken commentsArray)
        {
            // Load and save the content in elevated mode, because it is allowed 
            // to comment on files for users who have only preview permission for it.

            using (new SystemAccount())
            {
                var file = Node.Load<File>(content.Id, content.ContentHandler.Version);

                file.PreviewComments = JsonConvert.SerializeObject(commentsArray);
                file.VersionCreatedBy = content.ContentHandler.VersionCreatedBy;
                file.VersionCreationDate = content.ContentHandler.VersionCreationDate;
                file.VersionModifiedBy = content.ContentHandler.VersionModifiedBy;
                file.VersionModificationDate = content.ContentHandler.VersionModificationDate;
                file.Save(SavingMode.KeepVersion);
            }
        }
        private static void AssertCommentFeature(Content content)
        {
            if (!(content?.ContentHandler is File))
                throw new SnNotSupportedException("Cannot comment on this type of content.");

            var latestContent = content.IsLatestVersion
                ? content
                : SystemAccount.Execute(() => Content.Load(content.Id));

            if (latestContent.ContentHandler.Locked && latestContent.ContentHandler.LockedById != User.Current.Id)
                throw new InvalidOperationException("Content is locked by someone else.");
        }
    }
}
