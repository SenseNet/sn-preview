---
title: "Create a custom document preview generator"
source_url: 'https://github.com/SenseNet/sn-preview/blob/master/docs/custom-document-preview-generator.md'
category: Development
version: v7.0
tags: [document, preview, preview generator]
---
Overview
========

**Sense/Net ECM** provides means to [view documents in the browser](http://wiki.sensenet.com/Viewing_documents_in_the_browser "Viewing documents in the browser") without the need of *Microsoft Office* or other tools to be installed on the users' computers. The built-in solution is only available in the **Enterprise Edition**. However Sense/Net developers can integrate any third-party preview generator module easily even in the **Community Edition**. This tutorial shows you how to do it.

The information in this article is related to **Sense/Net ECM 6.3.1** and above. For implementations for previous versions please refer to the source code.

Creating the provider
=====================

To implement a custom preview provider you will need to [create a new class](http://wiki.sensenet.com/How_to_create_a_new_class_for_source_code "How to create a new class for source code") that inherits one of the following base classes:

-   *SenseNet.Preview.DocumentPreviewProvider*: in both the *Enterprise* or the *Community Edition*. Contains all the necessary methods for a full implementation.
-   *AsposePreviewProvider.AsposePreviewProvider*: only in the *Enterprise Edition*. Additionally to the one above, it contains methods and properties related to the [Aspose](http://www.aspose.com/) image generator plugin.

namespace MyNamespace
{
    public class MyPreviewProvider : DocumentPreviewProvider
    {
		...
    }
}

The class can be anywhere in your project as it will be discovered automatically by the system when the application starts.

Generating images
=================

The preview provider uses the [Task Management](http://wiki.sensenet.com/Task_Management "Task Management") framework for queueing and managing image generator tasks. The actual preview image generation is done using a command line tool (a task tool), as you can see later in this article

In your provider you have to override at least the following methods and properties:

-   **IsContentSupported(Node)**: returns if this implementation supports the specified content. This is necessary for performance reasons: the task that will generate images will only start for supported content (e.g. documents, but not Javascript files).
-   **GetPreviewGeneratorTaskName(path)**: this method should return the name of the preview generator task (the name of the command line tool). Please read the [Task Management](http://wiki.sensenet.com/Task_Management "Task Management") article for more details about tasks.

Before version **6.3.1 Patch 4** this is a property; after the patch it is a method that may return a different task name based on the content. This may be useful when you plan to execute certain tasks on dedicated agents.

If you only want to integrate a custom preview generator tool, but leave the built-in preview serving mechanism as-is, then your custom provider is done - please continue with the [image generator tool](http://wiki.sensenet.com/How_to_integrate_a_custom_document_preview_generator#Creating_the_image_generator_tool) section below.

Serving images
==============

All image requests in the portal go through the configured preview provider. In this section you'll see a list of available API methods related to serving images and can be overridden in your custom class. Override them only if you want to achieve a different behavior than the built-in one (e.g. store preview images with a different name or in a different place).

-   **IsPreviewOrThumbnailImage(NodeHead)**: determines if the provided nodehead belongs to a preview or thumbnail image.
-   **IsThumbnailImage(Image)**: determines if the provided image is a thumbnail image.
-   **IsPreviewAccessible(NodeHead)**: determines if the provided nodehead belongs to a document that the user has preview permissions to and makes sure that the user has access to preview images for minor versions only if he or she is allowed to.
-   **HasPreviewImages(Node)**: this method returns true if a document has (or will have) preview images. It returns false in case of unsupported or empty documents for example.
-   **GetPreviewImages(Content)**: returns all preview images for a document (creating them if they do not exist). Used when generating a pdf document from a content.
-   **GetExistingPreviewImages(Content)** (from version **6.4.2**): returns existing preview images without generating new ones. Returns only the *first uninterrupted interval* of preview images (e.g. if there is a gap, subsequent images will not be returned).
-   **GetPreviewImage**(Content, int): returns the preview image for the specified page. If the image does not exist, it registers a preview generation task but returns null.
-   **GetThumbnailImage**(Content, int): returns the thumbnail image for the specified page. If the image does not exist, it registers a preview generation task but returns null.
-   **GetPreviewImagesDocumentStream(Content, IEnumerable<Image>, DocumentFormat, RestrictionType)**: constructs a full document (stream) of the provided type (e.g. PDF) using the given preview images.
-   **GetPreviewImagePageIndex(Image)**: determines the page index of the provided preview image. Override this if you want to store your images with a different name than the built-in *previewX.png*.
-   **GetPreviewsFolder(Node)**: finds or creates the previews folder for a given *version* of a content. E.g. */Root/Library/MyDocument.docx/Previews/V2.0.A*. Override this if you want to store images in a different place than the built-in one.
-   **EmptyPreviewsFolder(Node)**: clears all preview images in a preview folder. Override this if you want to store images in a different place than the built-in one.
-   **RemovePreviewImagesAsync(int, VersionNumber, bool)** (from version **6.3.1 Patch 3**): Deletes preview images for one version or for all of them asynchronously.

For additional helper methods available in the base class (e.g. getting preview or thumbnal image names or modifying the page count or status of the document) please refer to the source code.

Restricted preview images
=========================

It is possible to add restrictions to the displayed preview images. See the [document preview article](http://wiki.sensenet.com/Viewing_documents_in_the_browser#Restrictions_and_annotations "Viewing documents in the browser") for detailed explanation. The built-in document preview provider has a default implementation for serving restricted images, but you can customize that behavior as shown in the following sections.

Get restricted image stream
---------------------------

When a preview image is accessed, the portal serves the request through the current Document Preview Provider. It places the restrictions onto the preview images on-the-fly, based on user permissions. You can override the **GetRestrictedImage** method of DocumentPreviewProvider to implement a custom logic for placing redactions and watermark onto preview images.

### Drawing watermark

If you do not want to change the built-in behavior of generating the whole image, you may customize only how the watermark is drawn on top of it. You can do that by overriding only the **DrawWatermark** method that receives all the necessary option values as parameters.

Get restriction type
--------------------

You can override the **GetRestrictionType** method of DocumentPreviewProvider to implement custom logic for determinig whether the user has the appropriate permissions for preview images. The method returns a **flag enumeration value** that contains the restrictions that need to be placed onto the preview image. The portal uses this method to determine if the user has any previw permissions. If not, the portal will not serve preview images.

The possible return values are the following:

-   **NoAccess**: the user does not have any preview permissons. If this value is returned, the portal will not serve preview images to the user.
-   **NoRestriction**: the user has access to the full preview, without restrictions.
-   **Redaction**: redactions must be placed onto the document if they exist.
-   **Watermark**: a watermark must be placed onto the document if it exists.

The built-in document preview provider works with the built-in [preview permissions](http://wiki.sensenet.com/Viewing_documents_in_the_browser#Preview_permissions "Viewing documents in the browser"). You can customize this method for example to check group membership instead:

namespace MyNamespace
{
    public class MyPreviewProvider : DocumentPreviewProvider
    {
        public override RestrictionType GetRestrictionType(NodeHead nodeHead)
        {
            if (nodeHead == null)
                return RestrictionType.NoAccess;

            //has Open permission: no restriction
            if (SecurityHandler.HasPermission(nodeHead, PermissionType.Open))
                return RestrictionType.NoRestriction;

            var editor = User.Current.IsInGroup(editors);
            var manager = User.Current.IsInGroup(managers);

            if (!editors && !managers)
                return RestrictionType.Redaction | RestrictionType.Watermark;

            if (!editors)
                return RestrictionType.Redaction;

            if (!managers)
                return RestrictionType.Watermark;

            return RestrictionType.NoRestriction;
        }
    }
}

Creating the image generator tool
=================================

If you want to use the built-in preview generator shipped with the **Enterprise Edition**, you do not have to create your own generator. This is necessary only in case of the **Community Edition** or if you want to replace the built-in one.

Preview image generation is done using a [task executor tool](http://wiki.sensenet.com/Task_Management#Creating_a_custom_task_executor "Task Management"). This is a command line tool that downloads the document (e.g. a word document), and generates preview images using a 3rd party image generator plugin (e.g. [Aspose](http://www.aspose.com/)).

Adding new extensions (from version 6.3.1 Patch 4)
--------------------------------------------------

If you want to add more capabilities to the image generator and you are using the **Enterprise Edition**, you have the possibility to simply extend the built-in tool instead of creating a new one. For example if you want to add support for *.abc* files, you have to implement an interface and put your library to the folder of the executor. The system will find the implementations by type automatically.

The following interfaces can be found in the *SenseNet.Preview.dll*.

### IPreviewImageGenerator interface

This interface lets you provide a list of supported extensions and write your custom image generator logic for certain custom extensions, or override the built-in logic for already known extensions.

-   **KnownExtensions**: list of extensions supported by your plugin.
-   **GetTaskNameByExtension(extension)**: the name of the task that targets this particular extension. If you return empty here, the default task name defined by the preview provider will be used. This is useful when you want a dedicated executor (and task agent) to work with a certain file type. E.g. you want only a couple of agents to be able to generate preview images for *3DS* files and all the other agents to work with office documents. In this case you will need to create the custom executor tool for that extension.
-   **GeneratePreview(stream, context)**: this is the method that actually generates preview images from the document stream, using the information and helper methods found in the provided *context*.

#### Example

The following example shows a sample implementation that is built on the default class.

public class MyPreviewImageGenerator1 : PreviewImageGenerator
{
    private static readonly string MyExt1 = ".myext1";
    private static readonly string MyExt2 = ".myext2";
    private static readonly string MyTaskExecutorName = "MyTaskExecutorName";

    public override string[] KnownExtensions { get { return new[] { MyExt1, MyExt2 }; } }

    public override string GetTaskNameByExtension(string extension)
    {
        if (extension.Equals(MyExt1, StringComparison.InvariantCultureIgnoreCase))
            return MyTaskExecutorName;
        else
            return base.GetTaskNameByExtension(extension);
    }

    public override void GeneratePreview(Stream docStream, IPreviewGenerationContext context)
    {
        // image generation logic that uses the context to save images
    }
}

### IPreviewGenerationContext interface

Provides properties and methods for context information in a specific generator implementation. This context is responsible for holding document-specific information and for the communication with the portal (e.g. setting the page count or preview status of the document and saving the generated images to the portal). In most cases you will not need to change the built-in implementation, only if you want to customize the environment of the preview generation process. The methods and properties defined in this interface are the following.

-   **ContentId**: id of the document
-   **PreviewsFolderId**: id of the preview folder where images will be saved
-   **StartIndex**: the page number where we need to start generating images
-   **MaxPreviewCount**: maximum number of images to generate
-   **PreviewResolution**: [image resolution](http://wiki.sensenet.com/Viewing_documents_in_the_browser#Generator_configuration "Viewing documents in the browser")
-   **Version**: document version
-   **SetPageCount**: saves the exact page count to the document back on the portal
-   **SetIndexes**: helper method to set the final start and stop index for images
-   **SavePreviewAndThumbnail**: saves both preview and thumbnail images and writes the progress to the console.
-   **SaveEmptyPreview**: saves an empty preview image in case the 3rd party image generator was not able to recognise a page.
-   **SaveImage**: a different API for saving both preview and thumbnail images using the previously defined *SavePreviewAndThumbnail* method.
-   **LogInfo**: logs an information message
-   **LogWarning**: logs a warning message
-   **LogError**: logs an error message

OData API for the image generator
---------------------------------

The tool uses the [OData REST API](http://wiki.sensenet.com/OData_REST_API "OData REST API") of Sense/Net to get the necessary information from the portal and upload the generated preview images. The Document Preview Provider has the following **OData API** (OData actions and functions) for this tool:

-   **GetPreviewsFolder**: loads or creates the preview folder for the given version of the content. Optionally it can also empty the folder in case a cleanup is needed.
-   **SetPreviewStatus**: sets the preview status of the document (e.g. Error, NotSupported or InProgress)
-   **SetPageCount**: saves the page count of a document as a metadata field.
-   **SetInitialPreviewProperties**: a helper method that sets the initial preview image properties necessary for the preview framework to work (e.g. the creator or modifyer user). It is mandatory to call this method after creating a new preview image.

To actually upload the images you may use the built-in [Upload action](http://wiki.sensenet.com/Upload_action "Upload action"). See the example in that article about how to upload image binaries to the portal.

Deploying the tool
------------------

As the tool is a task tool, please read the [following article](https://github.com/SenseNet/sn-taskmanagement/blob/master/docs/task-management.md "Task Management") about deploying it.

Configuration
=============

To test your custom provider build your source code, deploy it to the web server and simply give the fully qualified name of your class in the web.config:

<add key="DocumentPreviewProvider" value="MyNamespace.MyPreviewProvider" />

After that, try to upload a new document: your class' functions will be invoked after the document is persisted to the repository.
