using System;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Preview.Controller
{
    internal class PreviewCommentUser
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("avatarUrl")]
        public string AvatarUrl { get; set; }
    }

    internal class PreviewCommentData
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }
        [JsonProperty("creationDate")]
        public DateTime CreationDate { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("page")]
        public int Page { get; set; }
        [JsonProperty("x")]
        public double X { get; set; }
        [JsonProperty("y")]
        public double Y { get; set; }

        internal PreviewComment ToPreviewComment()
        {
            return new PreviewComment(this);
        }
    }

    internal class PreviewComment
    {
        private PreviewCommentData Data { get; }

        [JsonProperty("id")]
        public string Id => Data.Id;
        [JsonProperty("createdBy")]
        public PreviewCommentUser CreatedBy { get; set; }
        [JsonProperty("creationDate")]
        public DateTime CreationDate => Data.CreationDate;
        [JsonProperty("text")]
        public string Text => Data.Text;
        [JsonProperty("page")]
        public int Page => Data.Page;
        [JsonProperty("x")]
        public double X => Data.X;
        [JsonProperty("y")]
        public double Y => Data.Y;

        public PreviewComment(PreviewCommentData data)
        {
            Data = data;

            // Workaround: we only have a domain\username information here that cannot be used
            // to load a node head. We have to try to load the whole user node in elevated mode
            // and check for permissions after.
            var caller = AccessProvider.Current.GetOriginalUser();
            var user = SystemAccount.Execute(() => string.IsNullOrEmpty(data.CreatedBy) ? null : User.Load(data.CreatedBy));
            if (user == null || !Providers.Instance.SecurityHandler.HasPermission(caller, user.Id, PermissionType.Open))
                user = User.Somebody;
            
            CreatedBy = new PreviewCommentUser
            {
                Id = user.Id,
                Path = user.Path,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl
            };
        }
    }
}
