---
title: "Viewing documents in the browser"
source_url: 'https://github.com/SenseNet/sn-preview/blob/master/docs/preview.md'
category: Guides
version: v7.0
tags: [document, preview, sn6, sn7]
description: Sometimes it is necessary to open documents in the web browser directly - for example because Microsoft Office (or another desktop office application) is not installed on the user's computer. The Enterprise Edition of sensenet ECM comes with an integrated document preview generator that creates images of documents in the background that can be viewed in any browser.

---

# Viewing documents in the browser
Managing Office documents is a popular feature in every ECM system. This is why **sensenet ECM** comes with built-in document management features like [Office integration](/docs/managing-documents-from-microsoft-office) that enables users to directly open Office documents from the repository and edit them using Microsoft Office (e.g. Word or Excel). It is however in many cases desired to open documents **in the web browser directly** even if Microsoft Office (or another desktop office application) is not installed on the user's computer. The **Enterprise Edition** of sensenet ECM comes with an integrated document preview generator that creates images of documents in the background that can be viewed in any browser.

> Please note that the preview generator feature is available out of the box only in the **Enterprise Edition** of sensenet ECM. Other parts of the preview feature - e.g. the [Document viewer](/docs/document-viewer) that displays the images - are available for everyone, so it is possible to use it if you create a custom tool for generating preview images.

When the user uploads or modifies a document, sensenet ECM generates preview images of the document and persists them into the [Content Repository](/docs/content-repository). These images then can be viewed in the browser and thus **no desktop application is required** to view contents of a document.

The preview generator implementation uses a provider approach, which means that you can easily use a different preview generation technology than the one that comes with sensenet ECM. Also, this way the whole preview generation feature can be turned on and off just by a simple web.config setting. See the [Configuration](#Configuration) section for details.

> The built-in preview generator is built on the following technologies: 
> - [Task Management](/docs/task-management) framework
> - [Aspose](http://www.aspose.com) image generator

## Supported document types
The supported document types depend on the preview generator implementation. The built-in generator supports the following types:

- **Microsoft Office Word documents (.doc, .docx)**
  - a single image is generated for every page of the document
- **Microsoft Office Excel workbooks (.xls, .xlsx, .xlsm, .xltm, .xltx)**
  - a single image is generated for every worksheet
- **Microsoft Office Powerpoint presentations (.ppt, .pptx, .pot, .potx, .ppsx, .pptx)**
  - a single image is generated for every slide
- **Microsoft Office Project files (.mpp)**
  - a single image is generated for every project page
- **Microsoft Visio files (.vsd, .vdx, .vss, .vst, .vsx, .vtx, .vdw)**
  - a single image is generated for every page
- **Open Document Format files (.ods, .odp, .odt)**
  - a single image is generated for every page
- **Adobe Portable Document Format (.pdf)**
  - a single image is generated for every page
- **Images (.gif, .jpg, .jpeg, .bmp, .png, .svg, .exif, .icon, .tiff, .tif)**
  - a single image is generated for every image
- **Email files (.msg)**
  - a single image is generated for every page of the document
- **Other file formats (.xml, .txt, .rtf, .csv)**
  - a single image is generated for every page

## Document viewer
When you click on a document in a document library (or choose the *Preview* item from its menu) you'll see a preview of the document along with some metadata and a few editing features (depending on your permissions). About adding redactions and annotations see the restriction section later.

![Document Preview](https://github.com/SenseNet/sn-preview/raw/master/docs/images/preview-viewer.png "Document Preview")

## Preview image generation
1. Whenever a user modifies or uploads a document anywhere in the Content Repository, a new task will be registered (see the [Task Management](/docs/task-management) article for details) for generating images. Only a configured number of images will be generated in one round: one task is defined for only a small page interval (e.g. 1-10, 11-20, see the settings section below).
2. Also if a user visits the preview page of a document and there are missing preview images (e.g. the user scrolls toward the end of the document), the system will generate new tasks for the missing intervals too. The advantage of this is that it is possible to have multiple tasks for the same document for different intervals, and have multiple preview generator agents in the background creating preview images for the same document at the same time.
3. Preview images will be generated under the document in a system folder called *Previews*. The name of the images will be *preview[i].png*, where *[i]* is the index (page number) of the image. Small thumbnail images are also generated.

##### Monitoring preview generation
You can follow the progress of generating preview images on the **Task monitor** page, accessible from the Root console in Content Explorer.

## Performance considerations
After uploading a document the built-in preview generator creates images for the first few pages of the document. If your users upload many documents at the same time this could result in slower preview image generation, depending on the number of available task agents that are generating images in the background. See the [Task Management](/docs/task-management) article for more details on tasks.

In case the user performs a *checkout* operation, the preview provider does not register a new task: it simply copies previously generated images. Also when a content version is removed (e.g. in case of an *undo changes* operation), preview images are removed from the system to free up disk space.

## Generator configuration
The built-in preview generator tool has the following configuration options in its config file.

- **ChunkSize**: Size of chunks (in bytes) that will be uploaded to the portal during image generation. Default: *10485760*.
- **PreviewResolution**: Resolution of generated preview images. Default: *300*.

## Errors during image generation
The built-in preview generator tool logs all error messages through the agent (it does not have a log file or event log section itself). The agent than sends the log to sensenet ECM through the task management web app. This means that all error messages can be processed and handled by the developer in the sensenet ECM web application.

In case only a page fails (not the whole document), the generator tool substitutes that page with an empty image that can be customized by replacing the *empty.png* in the folder of the generator tool.

## Versioning and preview images
Preview images for different versions are stored in separate subfolders (e.g. *.../mydoc.docx/Previews/V2.3.A/preview1.png*), so when you visit the preview page for a particular version, you'll see the preview images related to that version. This applies to users with permissions only for major versions: they will see previews for the latest major version, while administrators see the previews of the last draft.

## Restrictions and annotations
sensenet ECM supports adding visual layers onto preview images. In this section you can learn about the types and possibilities.

- watermark
- redaction
- annotations
- highlights

### Watermark
It is possible to define a watermark text globally or in a local setting (see the [Settings](#Settings) section for details). Additionally to this, every file has a text field called **Watermark**. If you fill this field with a text, it will override the setting and appear on the preview images of the document for users who do not have the *Preview without watermark* permission.

> **For developers**: the Watermak property of the File type is overridable, so it is possible to implement a custom logic for getting the watermark text from a custom location.

#### Watermark templates
When defining the watermark, you can provide templates that the system will fill with the actual values. You can provide the name of the template in the following format (the property name is optional):

```@@TemplateName.PropertyName@@```

The built-in templates are the following:

- **CurrentDate**: short date for the current day
- **CurrentTime**: full date containing the time
- **CurrentUser**: any property of the current user (e.g. @@CurrentUser.Name@@)
- **FullName**: full name of the current user
- **Email**: email address of the current user
- **IpAddress**: IP address of the client (it may be the same for users behind a firewall)

It is also possible to customize or extend the list of available templates. Developers can inherit from the class *SenseNet.ContentRepository.Preview.WatermarkTemplateReplacer* and add new templates or override the built-in ones. For more information on how to use template replacers, please visit the [TemplateManager](/docs/templatemanager) article.

### Redaction
Editors can hide certain parts of a document so that users without open permission can see only the parts that they are allowed to. Redactions are basically rectangles that *will be drawn onto preview images on-the-fly* when a user with less permissions loads those preview images. These rectangle coordinates are stored on the document as metadata. For users with less permissions *there is no way to see the image without the redaction*, as this is managed by the document preview provider on the server side.

> The same redacted preview images will be used to generate a pdf document when a user with only preview permissions wants to download the document in a pdf format.

### Annotations
Editors can place annotations (comments) onto preview images. These comments are visible for everyone with at least preview permissions, but can be switched OFF in the [Document Viewer](/docs/document-viewer).

### Highlights
Editors can place highlights onto preview images. These highlights are visible for everyone with at least preview permissions, but can be switched OFF in the [Document Viewer](/docs/document-viewer).

## Preview permissions
There are three preview-related permissions in sensenet ECM:

- **Restricted preview**: the user can see the preview image, but only with all the restrictions if there are any.
- **Preview without watermark**: the user can see the preview image without watermark. Please note that even if the user has this permission, it is possible to switch ON watermark in the [Document Viewer](/docs/document-viewer) to check how it will look for users with less permissions.
- **Preview without redaction**: the user can see the preview image without redaction.

If the user has at least one of the permissions above, than she can see the preview images of the document. Also she will have access to all content metadata, but not to the binary fields. Only users with *Open* permission are able to access the binaries. 

> The preview permission behavior of the portal can be customized. See the [How to integrate a custom document preview generator](/docs/custom-document-preview-generator) article for more information.

<a name="Configuration"></a>
## Configuration
To enable the built-in preview generation make sure that the following line is present in the *web.config* (*sensenet/providers* section). To use a [custom preview generator](/docs/custom-document-preview-generator) set this value to the full class name of the custom preview generator.

```xml
<add key="DocumentPreviewProvider" value="SenseNet.Preview.AsposePreviewProvider" />
```

<a name="Settings"></a>
## Settings
The global preview settings can be found in the following settings content:
- */Root/System/Settings/DocumentPreview.settings*

You can customize the following values here:

- **MaxPreviewCount**: the number of preview images to generate in one round. Default is *10*. The optimal value depends on how many preview generator agents you have and the number of average simultaneous document uploads.
- **WatermarkEnabled**: true or false (default is true). This is a master switch for the watermark feature. You can use this to hide all watermarks, even if the texts are set on the documents.
- **WatermarkText**: if you want to, you may provide a watermark text here instead of on individual documents. Watermark text set on documents will override this setting. You may use templates here as well, as you can see in the section above.
- **WatermarkFont**: watermark font name (e.g. "Microsoft Sans Serif")
- **WatermarkFontSize**: font size of the text
- **WatermarkBold**: *true* or *false*
- **WatermarkItalic**: *true* or *false*
- **WatermarkColor**: color of the text. Plain text (e.g. Blue) or color code (e.g. #123456).
- **WatermarkOpacity**: opacity of the text
- **WatermarkPosition**: text orientation. Possible values: 
  - *BottomLeftToUpperRight*
  - *UpperLeftToBottomRight*
  - *Top*
  - *Bottom*
  - *Center*

> You may have different preview settings on lower levels in your content tree - e.g. under every workspace. See [Settings](/docs/settings) for more details.

## Integrating a custom preview generator
To integrate a custom document preview generator you'll need to implement your preview provider that will define the supported types and the [custom task](/docs/task-management) that will generate the images. To learn more on this topic read this article:

- [How to integrate a custom document preview generator](/docs/custom-document-preview-generator)

## Related links
- [How to integrate a custom document preview generator](/docs/custom-document-preview-generator)
- [Managing Documents from Microsoft Office](/docs/managing-documents-from-microsoft-office)
- [Task Management](/docs/task-management)
- [Document Viewer](/docs/document-viewer)
- [Document Viewer for Developers](/docs/document-viewer-for-developers)
- [Aspose](http://www.aspose.com)