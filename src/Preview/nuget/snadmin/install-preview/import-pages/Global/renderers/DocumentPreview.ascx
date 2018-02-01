<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage.Schema" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage.Security" %>
<%@ Import Namespace="SenseNet.Portal.Virtualization" %>
<%@ Import Namespace="SenseNet.Portal.OData" %>
<%@ Import Namespace="SenseNet.Search" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.Preview" %>
<sn:ScriptRequest ID="Scriptrequest2" runat="server" Path="$skin/scripts/viewer.js" />
<% var file = this.Content.ContentHandler as SenseNet.ContentRepository.File;
   var filePath = file.Path;
   var version = file.Version.ToString();
   var ext = SenseNet.ContentRepository.ContentNamingProvider.GetFileExtension(file.Name);
   var xls = ext == ".xls" || ext == ".xlsx";
   var doc = ext == ".doc" || ext == ".docx";
   var ppt = ext == ".ppt" || ext == ".pptx";
   var pdf = ext == ".pdf";
   //var binarySize = file.Size;
   //var size = ((double)binarySize) / 1024 / 1024;
   //size = size < 1 ? size : (binarySize / 1024 / 1024);
   var previews = SenseNet.ContentRepository.Storage.RepositoryPath.Combine(file.Path, "Previews");
   var previewCount = file.PageCount;
   var doctype = xls ? "Microsoft Office Excel" : doc ? "Microsoft Office Word" : ppt ? "Microsoft Office Powerpoint" : pdf ? "Adobe PDF" : "Other Document";
   //var sizestr = size < 1 ? String.Format("{0:0.##}", size) : size.ToString("N0");
   var createdby = (file.CreatedBy as SenseNet.ContentRepository.User).FullName;
   var modifiedby = (file.ModifiedBy as SenseNet.ContentRepository.User).FullName;
   var lockedby = file.Locked ? (file.LockedBy as SenseNet.ContentRepository.User).FullName : string.Empty;
   var createdbyc = SenseNet.ContentRepository.Content.Create(file.CreatedBy);
   var modifiedbyc = SenseNet.ContentRepository.Content.Create(file.ModifiedBy);
   var lockedbyc = file.Locked ? SenseNet.ContentRepository.Content.Create(file.LockedBy as SenseNet.ContentRepository.User) : null;
   
   // These are needed on the client side; eliminating needless AJAX requests
   var canSave = PortalContext.Current.ContextNode.Security.HasPermission(SenseNet.ContentRepository.User.Current, PermissionType.Save);
   var canPreviewWithoutRedaction = PortalContext.Current.ContextNode.Security.HasPermission(SenseNet.ContentRepository.User.Current, PermissionType.PreviewWithoutRedaction);
   var canPreviewWithoutWatermark = PortalContext.Current.ContextNode.Security.HasPermission(SenseNet.ContentRepository.User.Current, PermissionType.PreviewWithoutWatermark);

   var resourceScript = SenseNet.Portal.Resources.ResourceScripter.RenderResourceScript("DocViewer", System.Globalization.CultureInfo.CurrentUICulture);

   var enterpriseEdition = true;
%>

<div class="sn-docviewer-actions">
</div>

<div id="<%= this.ClientID %>">
</div>
<input type="hidden" class="currentcontent" value='<%= SenseNet.Portal.Virtualization.PortalContext.Current.ContextNodePath %>' />
<input type="hidden" class="currentparent" value='<%= SenseNet.Portal.Virtualization.PortalContext.Current.ContextNode.ParentPath %>' />
<input type="hidden" class="currentnode" value='<%= SenseNet.Portal.Virtualization.PortalContext.Current.ContextNode.Name %>' />
<input type="hidden" class="currentuser" value='<%= User.Current.Path %>' />
<sn:ScriptRequest runat="server" Path="$skin/scripts/SN/SN.Util.js" />
<script type="text/javascript">

    <%= resourceScript %>
    $(function () {
        $("#<%= this.ClientID %>").viewer({
                docType: '<%= doctype%>',
                createdby: '<%= createdby %>',
                modifiedby:  '<%= modifiedby %>',
                ClientId: '<%= this.ClientID %>',
                previews: '<%= previews %>',
                filePath: '<%= filePath %>',
                fileVersion: '<%= version %>',
                canSave: '<%= canSave %>', 
                canPreviewWithoutWatermark: '<%= canPreviewWithoutWatermark %>',
                canPreviewWithoutRedaction: '<%= canPreviewWithoutRedaction %>',
                shapes: '<%=GetValue("Shapes") %>',
                pageAttributes: '<%=GetValue("pageAttributes")%>',
                ext: '<%= ext %>',
                IsHeadOnly: '<%= file.IsHeadOnly%>',
                IsPreviewOnly: '<%= file.IsPreviewOnly %>',
                Size: '<%= file.Size %>',
                mdate: '<%= file.ModificationDate.ToString(System.Globalization.CultureInfo.GetCultureInfo("en-US")) %>',
                cdate: '<%= file.CreationDate.ToString(System.Globalization.CultureInfo.GetCultureInfo("en-US")) %>',
                CurrentCulture: '<%= System.Globalization.CultureInfo.CurrentUICulture%>',
                emptyDocumentMessage: '<%=GetGlobalResourceObject("DocViewer", "emptyDocumentMessage")%>',
                extensionFailureMessage: '<%=GetGlobalResourceObject("DocViewer", "extensionFailureMessage")%>',
                uploadFailureMessage: '<%=GetGlobalResourceObject("DocViewer", "uploadFailureMessage")%>',
                noPreviewProviderEnabledMessage: '<%=GetGlobalResourceObject("DocViewer", "noPreviewProviderEnabledMessage")%>',
                displayName: '<%= file.DisplayName %>',
                loadingString: '<%=GetGlobalResourceObject("DocViewer", "generatingPreview")%>',
            });
            });


</script>

