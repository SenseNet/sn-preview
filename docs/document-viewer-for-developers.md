---
title: "Document viewer for Developers"
source_url: 'https://github.com/SenseNet/sn-preview/blob/master/docs/document-viewer-for-developers.md'
category: Development
version: v7.0
tags: [document, preview, sn6, sn7]
description: sensenet ECM comes with a built-in Document Viewer that helps you viewing documents and images in any modern browser on PCs, tablets and mobile phones. This article is for developers about how to customize the Document Viewer plugin on the client side.

---

# Document Viewer for Developers
The Document Viewer in sensenet ECM helps users to read documents in the browser, and complete them with highlights, redactions and annotations to facilitate collaboration. All this without needing a document viewer plugin or software. The Document Viewer shows the preview images of the document that are generated when you upload a file. You can learn more about preview generation in the main [Preview article](/docs/preview).

## Document Viewer
Document viewer’s basic functionality is to read a document in the browser. Actually it is not the document that you see in the viewer, only its pre-generated preview images. So users without the right permissions can’t access the original document, they can only see the preview images. There are some special permissions that are related to the document viewer.

#### Restricted preview
All fields of the document can be accessed except the documents binary, so the users who have only this permission can’t download the original document and can’t see the text under redacted parts of the document even when they browse the preview images outside the Document Viewer.

#### Preview without watermark
Users who have this permission can see the watermarked document without watermark. For these users a button is shown in the toolbar to hide and show watermark. For users without this permission this button is hidden.

#### Preview without redaction
Users who have this permission can see the redacted document without redactions and can edit the available redactions and add new redactions.  For users without these permission redactions can’t be hidden they are generated on the preview image.

## Add/edit annotations, highlights and redactions
Adding annotations, highlights and redactions can be done the same way. There are three canvases for the three functions over the pages of the document and there’s a fourth technical canvas to help the drawing, resizing and positioning issues. When you select a button the related canvas will be positioned over the others and you can draw – add and edit shapes - on it. You can add a shape by double clicking on the selected canvas or select a rectangular area by drawing. If you add the shape by double clicking, the shape will have a default size but if you’ve added it with drawing a rectangle, the shape will have the same size that the drawn area.

An available shape can be resized and repositioned by the user with the right permission. If the chosen button is selected and the related canvas is selected also, you can select shapes on the chosen canvas by clicking on it and after that it can be moved with drag and drop or resized with the marked points around the shape. Move and resize are processing on the technical canvas and after the end of the activity they will be moved on the chosen canvas and removed from the technical canvas.

### Save annotations, highlights and redactions
If a user has ‘Save’ permission he can save the added/edited shapes to the document. These shapes aren’t saved in the documents binary but the document content type has a longtext field (*Shapes*) to store shape information in JSON format. The document viewer uses this field’s content to redraw saved shapes after reloading the document previews in the Document Viewer.

```javascript
[{
"redactions":[{"x":85.68852240366039,"y":91.66401273885349,"w":568.9490445859872,"h":29.50106157112525,"imageIndex":1,"index":0}]
},{
"highlights":[{"x":92.01017845461574,"y":339.2622080679405,"w":613.200636942675,"h":91.66401273885344,"imageIndex":1,"index":0}]
},{
"annotations":[]
}]
```

While the user edits/adds annotations, highlights or redactions to the previews, the Document Viewer stores the shape information in the background. This stored information can be passed to a save functionality.

### Save page orientations
If a user has ‘Save’ permission he can save the rotation data of the document's pages. Page orientation aren’t saved in the documents binary but the document content type has a longtext field to store rotation data in JSON format. The document viewer uses this field’s content to reload pages with the saved orientation.

```javascript
[{
    "pageNum":"1",
    "options":{"degree":90}
 },
 {
    "pageNum":"2",
    "options":{"degree":-90}
}]
```

When the user rotates a page in the Document Viewer, it stores the rotation data in the background. This stored information can be passed to a save functionality. 

> Please note that edit/add annotation, redaction or highlight functionality is disabled on rotated pages.

Only the original preview images are stored, with the original orientation (which may be landscape actually, if a page was rotated originally in Word for example). When you rotate images in the viewer and save these modifications, it is the viewer's responsibility to get the images with the appropriate orientation. There is a simple URL parameter for this (called *rotation*) that you can use when requesting individual preview images:

```.../Previews/v2.0A/preview1.png?rotation=90```

The rotated images will also appear correctly when we generate a PDF document from these preview images on the server.

### Watermark
Watermark can be set on every document or globally (on a folder, a workspace or sitewide). You can set the watermark text in the watermark field of a document or in the *DocumentPreview.setting* file. There you can enable the watermark functionality and set the watermark text or customize watermark texts font type, weight, style, color, opacity or position.

You can learn more about [Settings](/docs/settings) here.

## Getting started
You can bind the Document Viewer to a common html container element just like other jQuery plugins.
 
First create a container element:

```html
<div id=”documentViewer”></div>
```

Initialize the Query builder via a jQuery selector:

```javascript
     $dv.documentViewer({
	getImage: functionName,
	getThumbnail: functionName,
	showthumbnails: true,
	metadata: true,
        showtoolbar: true,   
        edittoolbar: true,                              
        title: 'My document', 
	containerWidth: ’1024’,
	containerHeight: ’500’,
        reactToResize: true, 
	metadataHtml: ’’,
        isAdmin: isAdmin, 
        showShapes: true,
        shapes: '<%=GetValue("Shapes") %>',
        SR: resourceArray,
        functions: {
	    print: {
		action: yourPrintFunctionName,
		title: „Print”,
		icon: html
		type: ’dataRelated’,
		touch: false
            }
        }
        previewCount : '<%= previewCount %>',
        filePath : filePath,
        fitContainer : true
     });
```

## Configuring the Document Viewer jQuery plugin

<div><span style="font-size: 16px;font-weight: bold;">getImage</span><span style="font-size: 16px;color:#008cd1">  function</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: null)</span></div>
The function that gives the path of a previewimage for pagenumber (this function will be called, when the docviewer want to display a page).

<div><span style="font-size: 16px;font-weight: bold;">getExistingPreviewImages</span><span style="font-size: 16px;color:#008cd1">  function</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: null)</span></div>
The function that gives an array with the paths, index, width and height of all the preview images that are already generated (this function will be called, when the docviewer initializing).

<div><span style="font-size: 16px;font-weight: bold;">getThumbnail</span><span style="font-size: 16px;color:#008cd1">  function</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: null)</span></div>
The function that gives the path of a thumbnailimage for a page number (this function will be called when the docviewer want to display a thumbnail).

<div><span style="font-size: 16px;font-weight: bold;">showthumbnails</span><span style="font-size: 16px;color:#008cd1">  Boolean</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: true)</span></div>
You can set visibility of thumbnail images.

<div><span style="font-size: 16px;font-weight: bold;">metadata</span><span style="font-size: 16px;color:#008cd1">  Boolean</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: false)</span></div>
You can set visibility of metadata but it isn’t visible if the ‘metadataHtml’ is empty.

<div><span style="font-size: 16px;font-weight: bold;">showtoolbar</span><span style="font-size: 16px;color:#008cd1">  Boolean</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: false)</span></div>
You can set visibility of the toolbar.

<div><span style="font-size: 16px;font-weight: bold;">edittoolbar</span><span style="font-size: 16px;color:#008cd1">  Boolean</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: false)</span></div>
You can set visibility of the editor toolbar width annotation, highlight and redaction buttons.

<div><span style="font-size: 16px;font-weight: bold;">title</span><span style="font-size: 16px;color:#008cd1">  String</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: null)</span></div>
You can set the title of the document. It will be shown in the toolbar.

<div><span style="font-size: 16px;font-weight: bold;">containerWidth</span><span style="font-size: 16px;color:#008cd1">  Number</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: $container.width())</span></div>
You can set a special width to size the Document viewer. If you leave this property empty, the plugin will calculate viewer’s width from elements width. This property is useful if you want to use different widths on different devices e.g. tablets, mobile phones.

<div><span style="font-size: 16px;font-weight: bold;">containerHeight</span><span style="font-size: 16px;color:#008cd1">  Number</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: $container.height())</span></div>
You can set a special height to size the Document viewer. If you leave this property empty, the plugin will calculate viewer’s height from elements height. This property is useful if you want to use different height on different devices e.g. tablets, mobile phones.

<div><span style="font-size: 16px;font-weight: bold;">reactToResize</span><span style="font-size: 16px;color:#008cd1">  Boolean</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: false)</span></div>
Set it to true if you want to resize document viewer dynamically when the browser window is resized.

<div><span style="font-size: 16px;font-weight: bold;">metadataHtml</span><span style="font-size: 16px;color:#008cd1">  String</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: null)</span></div>
You can set here what Document Viewer show in the metadata section. It can be html or simple text.

<div><span style="font-size: 16px;font-weight: bold;">isAdmin</span><span style="font-size: 16px;color:#008cd1">  Boolean</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: false)</span></div>
You can tell the viewer if the current user has the Save permission. It can work dynamically if you get the permissions of the current user for example through OData and give it to the Document Viewer plugin. 

If this property is true all the buttons will be shown in the toolbar, and the user can edit/hide redactions and watermark. So it’s recommended to test current users ‘Save permission’ outside the plugin and give its result to the Document Viewer. 

<div><span style="font-size: 16px;font-weight: bold;">showShapes</span><span style="font-size: 16px;color:#008cd1">  Boolean</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: true)</span></div>
You can set the visibility of shapes over the previews. If it’s set to true, the shapes will be displayed when the document is loaded. If it’s set to false the shapes will not appear but you can display them by clicking on the ‘Show shapes’ button.

<div><span style="font-size: 16px;font-weight: bold;">shapes</span><span style="font-size: 16px;color:#008cd1">  Object</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: null)</span></div>
You must set the shapes of the document. The shapes are stored in one of the documents field in JSON format. You can get the value of this field the way that you preferred.

<div><span style="font-size: 16px;font-weight: bold;">pageAttributes</span><span style="font-size: 16px;color:#008cd1">  Object</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: null)</span></div>
You must set where the document's page orientation data is stored. This data is stored in one of the documents field in JSON format. You can get the value of this field the way that you preferred.

<div><span style="font-size: 16px;font-weight: bold;">SR</span><span style="font-size: 16px;color:#008cd1">  Object</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: null)</span></div>
You can set custom string resources to the Document Viewer but we recommend to change them in the /Root/Localization/SN_DocViewer.xml string resource file.

#### Functions

Additional functions can be added here in json format. DocViewer will iterate over these functions and add them to the toolbar. Functions can be customized with the following options (e.g. Print functionality)

```javascript
     functions: {
         print: {
            action: printDocument,
            title: SN.Resources.DocViewer["DocViewer-toolbarPrint"],
            icon: '<span class="sn-icon sn-icon-print"></span>',
            type: 'dataRelated',
            touch: false,
		    permission: Save
         }
     }
```

<div><span style="font-size: 16px;font-weight: bold;">action</span><span style="font-size: 16px;color:#008cd1">  function</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: null)</span></div>
This function will be called when the user click on the toolbar button of the function.

<div><span style="font-size: 16px;font-weight: bold;">title</span><span style="font-size: 16px;color:#008cd1">  String</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: empty string)</span></div>
This will be the title of the toolbar button.

<div><span style="font-size: 16px;font-weight: bold;">type</span><span style="font-size: 16px;color:#008cd1">  'dataRelated' or 'drawingRelated'</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: 'dataRelated')</span></div>
If you set type to ‘drawingRelated’ your custom functions button will be displayed next to the redaction button. If you choose dataRelated, the button will be displayed at the end of the toolbar.

<div><span style="font-size: 16px;font-weight: bold;">icon</span><span style="font-size: 16px;color:#008cd1">  String</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: empty string)</span></div>
You can add html to display as button/link in the toolbar.

<div><span style="font-size: 16px;font-weight: bold;">touch</span><span style="font-size: 16px;color:#008cd1">  Boolean</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: false)</span></div>
You can choose whether your function will be displayed on touch devices or not. 

<div><span style="font-size: 16px;font-weight: bold;">permission</span><span style="font-size: 16px;color:#008cd1">  Boolean</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: false)</span></div>
You can add a function that you can return whether the current user can see the functions button in the toolbar or not. For example Save function is only enabled for a user who  has a Save permission, so you create a function that return true only when the current user has Save permission for the current content and you call this function in this permission option. The option expects only true or false as a value.

<div><span style="font-size: 16px;font-weight: bold;">getPC</span><span style="font-size: 16px;color:#008cd1">  function</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: null)</span></div>
The function that gives the actual pagecount and can be called from the plugin (this function will be called, when the docviewer is refreshed).

<div><span style="font-size: 16px;font-weight: bold;">getShapes</span><span style="font-size: 16px;color:#008cd1">  function</span><span style="font-size: 16px;font-style: italic;color: #555">  (default: null)</span></div>
The function that gives the actual document's shapes and can be called from the plugin (this function will be called, when the docviewer is refreshed).

## Methods
<div><span style="font-size: 16px;font-weight: bold;">setZoomLevel</span>
Sets the current zoom level of the viewer. 
<div style="font-size: 100%;font-weight: bold;">Parameters:</div>

- newLevel <span style="font-size: 13px;color:#666">   required |</span><span style="font-size: 13px;color:#008cd1">  integer</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: 1)</span> The chosen zoom rate.

-  x0 <span style="font-size: 13px;color:#666">   optional |</span><span style="font-size: 13px;color:#008cd1">  integer</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: null)</span> The chosen x coordinate.

- y0 <span style="font-size: 13px;color:#666">   optional |</span><span style="font-size: 13px;color:#008cd1">  integer</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: null)</span> The chosen y coordinate.

- $rel <span style="font-size: 13px;color:#666">   optional |</span><span style="font-size: 13px;color:#008cd1">  object</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: technicalCanvas)</span> This will be the selected top canvas after zooming.

- rb <span style="font-size: 13px;color:#666">   optional |</span><span style="font-size: 13px;color:#008cd1">  boolean</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: false)</span> Set it to true if you use the rubberband zoom.

```javascript
viewer = $dv.data('snDocViewer');
viewer.setZoomLevel(2);
```

<div><span style="font-size: 16px;font-weight: bold;">enterFullscreenMode</span>
Enters fullscreen mode.

```javascript
viewer = $dv.data('snDocViewer');
viewer.enterFullscreenMode();
```

<div><span style="font-size: 16px;font-weight: bold;">exitFullscreenMode</span>
Exits fullscreen mode.

```javascript
viewer = $dv.data('snDocViewer');
viewer.exitFullscreenMode();
```

<div><span style="font-size: 16px;font-weight: bold;">destroy</span>
Destroys the current plugin instance and closes the document.

```javascript
viewer = $dv.data('snDocViewer');
viewer.destroy;
```

<div><span style="font-size: 16px;font-weight: bold;">getAllShapes</span>
Returns the array which contains all shapes of the current document.

```javascript
viewer = $dv.data('snDocViewer');
var shapes = viewer.getAllShapes();
```

<div><span style="font-size: 16px;font-weight: bold;">isUnsaved</span>
Returns true if the document has unsaved changes. There's a boolean property in the background ('unsaved') which is set to true, if a user modified something related to the document (redaction, highlight, etc.)

```javascript
viewer = $dv.data('snDocViewer');
if(viewer.isUnsaved){
  //do something
}
```

<div><span style="font-size: 16px;font-weight: bold;">setUnsaved</span>
Sets unsaved property. You can set it to false in your custom save method, when the latest modifications are saved.
<div style="font-size: 100%;font-weight: bold;">Parameters:</div>

- isUnsaved <span style="font-size: 13px;color:#666">   required |</span><span style="font-size: 13px;color:#008cd1">  boolean</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: false)</span>

```javascript
viewer = $dv.data('snDocViewer');
viewer.setUnsaved(false);
```

<div><span style="font-size: 16px;font-weight: bold;">isFullscreen</span>
Tells if the viewer is in fullscreen mode.

```javascript
viewer = $dv.data('snDocViewer');
if(viewer.isFullscreen()){
  //do something
}
```

<div><span style="font-size: 16px;font-weight: bold;">zoomLevel</span>
Gets the current zoom level of the viewer.

```javascript
viewer = $dv.data('snDocViewer');
var zoomLevel = viewer.zoomLevel();
```

<div><span style="font-size: 16px;font-weight: bold;">getContainer</span>
Gets the container DOM element of the viewer.

```javascript
viewer = $dv.data('snDocViewer');
var $container = viewer.getContainer();
```

<div><span style="font-size: 16px;font-weight: bold;">getViewport</span>
Gets the viewport element of the viewer.

```javascript
viewer = $dv.data('snDocViewer');
var $docpreview = viewer.getViewport();
```

<div><span style="font-size: 16px;font-weight: bold;">getViewerId</span>
Gets the identifier of this viewer.

```javascript
viewer = $dv.data('snDocViewer');
var viewerID = viewer.getViewerId();
```

<div><span style="font-size: 16px;font-weight: bold;">scheduleRedraw</span>
Schedules a redraw for the viewer.

```javascript
viewer = $dv.data('snDocViewer');
viewer.scheduleRedraw();
```

<div><span style="font-size: 16px;font-weight: bold;">changePage</span>
Changes the currently displayed page in the viewer.
<div style="font-size: 100%;font-weight: bold;">Parameters:</div>

- page <span style="font-size: 13px;color:#666">   required |</span><span style="font-size: 13px;color:#008cd1">  integer</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: 1)</span>
:The number of the page which you want to display.

```javascript
viewer = $dv.data('snDocViewer');
viewer.changePage(5);
```

<div><span style="font-size: 16px;font-weight: bold;">currentPage</span>
Gets the current page the viewer is on.

```javascript
viewer = $dv.data('snDocViewer');
var currentPageNumber = viewer.currentPage();
```

<div><span style="font-size: 16px;font-weight: bold;">calledPage</span>
Gets number of the called page.

```javascript
viewer = $dv.data('snDocViewer');
var calledPageNumber = viewer.calledPage();
```

<div><span style="font-size: 16px;font-weight: bold;">pageCount</span>
Gets the number of pages the current document has.

```javascript
viewer = $dv.data('snDocViewer');
var pageCount = viewer.pageCount();
```

<div><span style="font-size: 16px;font-weight: bold;">loadedImages</span>
Gets the number of the loaded preview pages (array).

```javascript
viewer = $dv.data('snDocViewer');
var loadedImages = viewer.loadedImages();
```

<div><span style="font-size: 16px;font-weight: bold;">pageIsLoaded</span>
Tells when a preview page image is loaded.
<div style="font-size: 100%;font-weight: bold;">Parameters:</div>

- page <span style="font-size: 13px;color:#666">   required |</span><span style="font-size: 13px;color:#008cd1">  integer</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: 1)</span> The number of the page.

```javascript
viewer = $dv.data('snDocViewer');
if(viewer.pageIsLoaded(3)){
  //do something
}
```

<div><span style="font-size: 16px;font-weight: bold;">canvasesAreLoaded</span>
Tells when all canvases of a preview page are loaded.
<div style="font-size: 100%;font-weight: bold;">Parameters:</div>

- page <span style="font-size: 13px;color:#666">   required |</span><span style="font-size: 13px;color:#008cd1">  integer</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: 1)</span> The number of the page.

```javascript
viewer = $dv.data('snDocViewer');
if(viewer.canvasesAreLoaded(3)){
  //do something
}
```

<div><span style="font-size: 16px;font-weight: bold;">saveShapes</span>
Returns the shapes and the page rotation data of the current document in an array.

```javascript
viewer = $dv.data('snDocViewer');
var savable = viewer.saveShapes();
savable.Path = filePath;
   var p = odata.saveContent({
       contentItem: savable
   }).done(function () {
       overlayManager.showMessage({
           type: "success",
           title: SN.Resources.DocViewer["MessageBox-Success"],
           text: SN.Resources.DocViewer["DocViewer-burnSuccessful"]
   });
});
```

<div><span style="font-size: 16px;font-weight: bold;">setPageAccordingToScroll</span>
Sets the viewers current page related properties according to scroll.

```javascript
viewer = $dv.data('snDocViewer');
viewer.setPageAccordingToScroll();
```

<div><span style="font-size: 16px;font-weight: bold;">appendPreviewPostfix</span>
Set url postfixes (e.g. '&watermark=true').
<div style="font-size: 100%;font-weight: bold;">Parameters:</div>

- url <span style="font-size: 13px;color:#666">   required |</span><span style="font-size: 13px;color:#008cd1">  string</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: null)</span> Path of the preview or thumbnail image.

- addWatermark <span style="font-size: 13px;color:#666">   optional |</span><span style="font-size: 13px;color:#008cd1">  boolean</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: false)</span> Adds the watermark prefix to the image url (&watermark=true).

- addNoCache <span style="font-size: 13px;color:#666">   optional |</span><span style="font-size: 13px;color:#008cd1">  boolean</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: false)</span> Adds the nocache prefix to the image url (&nocache=date).

```javascript
viewer = $dv.data('snDocViewer');
appendPreviewPostfix = viewer.appendPreviewPostfix;
if (viewer.appendPreviewPostfix && typeof appendPreviewPostfix == "function") {
    var wmParam = '&watermark=true';
    $images.each(function (i) {
        var $img = $($images[i]);
        var oldsrc = $img.attr('src');
        var hadNoCache = oldsrc.indexOf('&nocache=') > 0;
        var newsrc = null;
        // Set the src parameter according to the watermark URL parameter
        var questionMarkPos = oldsrc.indexOf('?');
        var newsrc = (questionMarkPos > 0) ? oldsrc.substring(0, questionMarkPos) : oldsrc;
        newsrc = appendPreviewPostfix(newsrc, enabled, hadNoCache);
        $img.attr('src', newsrc);
    });
}
```

<div><span style="font-size: 16px;font-weight: bold;">refreshViewer</span>
Refreshes the viewer according to the document changes.
<div style="font-size: 100%;font-weight: bold;">Parameters:</div>

- refreshPager <span style="font-size: 13px;color:#666">   optional |</span><span style="font-size: 13px;color:#008cd1">  boolean</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: false)</span> If it's set to true, the pager of the viewer will be refreshed.

- refreshPreviews <span style="font-size: 13px;color:#666">   optional |</span><span style="font-size: 13px;color:#008cd1">  boolean</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: false)</span> If it's set to true, the preview images will be refreshed.

- refreshThumbnails <span style="font-size: 13px;color:#666">   optional |</span><span style="font-size: 13px;color:#008cd1">  boolean</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: false)</span> If it's set to true, the thumbnail images will be refreshed.

```javascript
viewer = $dv.data('snDocViewer');
$('#refreshButton').on('click', function(){
  viewer.refreshViewer(true, true, true)
});
```

<div><span style="font-size: 16px;font-weight: bold;">refreshEditorButtons</span>
It allows you to add and set additional properties on the editor buttons (annotation, highlight, redaction).
<div style="font-size: 100%;font-weight: bold;">Parameters:</div>

- buttonArray <span style="font-size: 13px;color:#666">   required |</span><span style="font-size: 13px;color:#008cd1">  Array</span><span style="font-size: 13px;font-style: italic;color: #555">  | (default: null)</span> An array of name and value pairs that you want to add as properties to the buttons (e.g. disabled)

```javascript
viewer = $dv.data('snDocViewer');

function adminbutton(name, buttonAdditonalProperties) {
   this.name = name;
   this.additionalProps = buttonAdditonalProperties;
}

$('#disable').on('click', function(){
     buttonArray = [];
     buttonArray[0] = new adminbutton('annotation',{disabled: true});
     buttonArray[1] = new adminbutton('highlight',{disabled: false});
     buttonArray[2] = new adminbutton('redaction',{disabled: false});
     viewer.refreshEditorButtons(buttonArray);
}
```

## Callbacks
<div><span style="font-size: 16px;font-weight: bold;">documentOpened</span>
Called when the document was opened, i.e. when this plugin was initialized

```javascript
   $dv.documentViewer({
         ...
         callbacks: {
            documentOpened: function(){
                // Get time
                var now = moment();
                lastPageChangeTime = now;
                ...
            }
         }
     });
```

<div><span style="font-size: 16px;font-weight: bold;">documentClosed</span>
Called when the document is closed, i.e. when either the plugin is destroyed or the window is unloaded

```javascript
   $dv.documentViewer({
         ...
         callbacks: {
            documentClosed: function(){
                // Calculate time spent
                var now = moment();
                var timeSpentOnDocument = 0;
                var timeSpentOnPage = now.diff(lastPageChangeTime, 'seconds');
                ...
            }
         }
     });
```

<div><span style="font-size: 16px;font-weight: bold;">pageChanged</span>
Called after going to a different page of the document (parameter: page number)
NOTE: this is called when the user scrolls to a different page, or clicks a thumbnail, or when the viewer otherwise goes to a different page

```javascript
   $dv.documentViewer({
         ...
         callbacks: {
            pageChanged: function(page){
                // Calculate time spent
                var now = moment();
                var timeSpentOnPage = now.diff(lastPageChangeTime, 'seconds');
                timeSpentOnEachPage[lastPage] = (timeSpentOnEachPage[lastPage] || 0) + timeSpentOnPage;
                // Reset time
                lastPageChangeTime = now;
                lastPage = page;
            }
         }
     });
```

<div><span style="font-size: 16px;font-weight: bold;">contextMenuShown</span>
Called after a context menu is shown (parameter: context menu object)

```javascript
   $dv.documentViewer({
         ...
         callbacks: {
            contextMenuShown: function($cm) {
               // do something
            }
         }
     });
```

<div><span style="font-size: 16px;font-weight: bold;">zoomLevelChanged</span>
Called when the zoom level is changed

```javascript
   $dv.documentViewer({
         ...
         callbacks: {
            zoomLevelChanged: function() {
               // do something
            }
         }
     });
```

<div><span style="font-size: 16px;font-weight: bold;">viewerError</span>
Called when an error happened in the viewer

```javascript
   $dv.documentViewer({
         ...
         callbacks: {
            viewerError: function(errorMessage) {
                overlayManager.showMessage({
                    type: "error",
                    title: errorMessage,
                    text: errorMessage
                });
            }
         }
     });
```

<div><span style="font-size: 16px;font-weight: bold;">documentChanged</span>
Called when the document was changed, i.e. when a shape is added

```javascript
   $dv.documentViewer({
         ...
         callbacks: {
            documentChanged: function() {
                //do something
            }
         }
     });
```

<div><span style="font-size: 16px;font-weight: bold;">rotationStarted</span>
Called when the rotation is started.

```javascript
   $dv.documentViewer({
         ...
         callbacks: {
            rotationStarted: function() {
                //do something
            }
         }
     });
```

<div><span style="font-size: 16px;font-weight: bold;">rotationEnded</span>
Called when the rotation is ended.

```javascript
   $dv.documentViewer({
         ...
         callbacks: {
            rotationEnded: function() {
                //do something
            }
         }
     });
```

<div><span style="font-size: 16px;font-weight: bold;">loadingStarted</span>
Called when the preview loading is started.

```javascript
   $dv.documentViewer({
         ...
         callbacks: {
            loadingStarted: function() {
                //do something
            }
         }
     });
```

<div><span style="font-size: 16px;font-weight: bold;">loadingEnded</span>
Called when the preview loading is ended.

```javascript
   $dv.documentViewer({
         ...
         callbacks: {
            loadingEnded: function() {
                //do something
            }
         }
     });
```

## OData actions and functions for Preview
There are a couple of built-in [OData](/docs/odata-rest-api) methods for customizing the preview plugin:
- [Built-in preview actions](/docs/built-in-odata-actions-and-functions#Preview_actions)
- [Built-in preview functions](/docs/built-in-odata-actions-and-functions#Preview_functions)

## Document Viewer on tablet
Document viewer can be used on tablets but with reduced functionality. On these devices we integrated a jQuery plugin named [iScroll](http://cubiq.org/iscroll-5) to the Document Viewer to get pinch zooming, double tap zooming, fancy scrolling and many more.
