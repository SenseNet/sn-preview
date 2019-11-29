---
title: "Document viewer"
source_url: 'https://github.com/SenseNet/sn-preview/blob/master/docs/document-viewer.md'
category: Guides
version: v7.0
tags: [document, preview, sn6, sn7]
description: sensenet comes with a built-in Document Viewer that helps you viewing documents and images in any modern browser on PCs, tablets and mobile phones. Document Viewer improves the productivity of companies by facilitating collaboration with features like highlights, redactions, annotations and watermarking across any enterprises.

---

# Document viewer
Managing Office documents is a popular feature in every ECM system. This is why sensenet comes with a built-in Document Viewer that helps you viewing documents and images in any modern browser on PCs, tablets and mobile phones. Document Viewer improves the productivity of companies by facilitating collaboration with features like highlights, redactions, annotations and watermarking across any enterprises. 

Our document viewer is a zero footprint HTML5 solution. It helps users to read documents in the browser without using any plugin or installing any software. Besides the common document viewer functionalities like paging, zooming, etc., you get the power to highlight, redact and annotate documents. On tablets the document viewer works in read only mode, so you can view the highlighted, redacted, annotated and watermarked documents while on the go.

Document Viewer:

- **Operates in any modern browser** – Sense/Net Viewer is runs in Internet Explorer (8 or later), Chrome, Firefox, Safari and Opera.
- **Requires no plugin or software installation** – We have built sensenet Document Viewer on HTML5. 
- **Is cross platform** – Our viewer works on PC and tablet. We have tested it on Windows and OS X thoroughly. Any modern HTML5 compliant browser is supported.
- **Can protect your information** – You can redact confidential document segments with redaction. Users without sufficient permissions will see only the redacted version of the documents and images. 
- **Improves productivity** – Apart from redaction, you can highlight, annotate, zoom and navigate documents with thumbnails in a browser.
- **Is highly configurable** – Our HTML5 viewer can be custom tailored to your special needs.

The component supports many document and image formats. Converting documents and images to a browser compatible format is performed on the server, so the viewer delivers an extremely high-speed response. Check out the supported formats in the main [Preview](/docs/preview) article.

## Basic document viewer features
### Paging
#### Pager

![Pager](https://github.com/SenseNet/sn-preview/raw/master/docs/images/preview-pager.png "Pager")

You can move to other pages in the preview with the document viewer’s pager. The pager shows the current page number and the total page count in the document. You can switch to the next or the previous page, or jump to the first or the last page.

#### Thumbnails
![Thumbnails](https://github.com/SenseNet/sn-preview/raw/master/docs/images/preview-thumbnails.png "Thumbnails")

The document viewer can show thumbnail images of the pages. If you click on a thumbnail, you can jump to the chosen page. If a part of the document is redacted, the thumbnail image of the page will be shown redacted as well.

#### Scrolling
You can switch pages by scrolling through the document. Contrary to most of the browser based document viewers with sensenet Document Viewer’s **continuous scroll** feature you can see the beginning of the next page while the last part of the current page is still on the screen. 

#### Jump to page
In the pager the actual page number is shown in a textbox. If you type a page number in this text box and hit enter you can jump to the chosen page.

### Rotating
#### Rotate current page left
You can rotate the current page or the whole document 90 degrees in both directions.

![Rotating](https://github.com/SenseNet/sn-preview/raw/master/docs/images/preview-rotatedleft.png "Rotating")

### Resizing
#### Original size
You can switch back to the original size of the page after zooming, resizing, etc.

#### Fit to window
You can fit the current page to the Document Viewer’s frame. If the page is in landscape orientation, it will fill the frame horizontally. Likewise if the page is in portrait orientation, it will fill the frame vertically.

#### Fit to height or width
The Fit to height and width buttons adjust the magnification so that one page fills the Document Viewer’s height or width respectively. 

### Zooming
You can obtain a closer view of the page by clicking the ‘Zoom in’ button. Every time you click this button the preview images are shown with a bigger scale.

![Zoom](https://github.com/SenseNet/sn-preview/raw/master/docs/images/preview-zoom.png "Zoom")

#### Rubber Band Zoom
With rubber band zoom you can zoom in to a selected area of the page. You can select a rectangular area and the viewer will zoom in to this part of the document. The zooming scale depends on the size of the selected area. 

#### Zooming on Tablets
On tablets you can zoom in and out with pinch zoom. For this reason on tablets the toolbar’s zoom buttons are not shown.  

### Full screen mode
In full screen mode the Document Viewer fills out the whole browser window. All the features of the viewer are still available.

The thumbnails are hidden and can be shown by clicking on the ‘Show thumbnails’ button.

You can switch back from full screen mode by clicking on the ‘Exit full screen mode’ button.

### Printing
You can print the preview images of the document by clicking on the ‘Print’ button.  Note that you can’t print the original document from the viewer, only the preview images according to your permissions. If you can see only redacted and/or watermarked previews in the viewer, you can print only with redaction and/or watermark. Highlights and annotations are not printed.

Document Viewer's print functionality detects pages with landscape orientation and dynamically *rotates them to portrait mode* in the browsers print preview.

### Metadata
Document metadata can be shown in the viewer. By default the type of the document, the creator and last modifier of the document, and the creation and last modification dates are shown.

## Advanced viewer features
If you have save permission on the document you can annotate, highlight and redact the page previews in the viewer.

### Annotations
You can add annotations to the document by double clicking on a page or by selecting a part of it. Annotations are editable anytime later. 

![Annotations](https://github.com/SenseNet/sn-preview/raw/master/docs/images/preview-annotation.png "Annotations")

You can edit a selected annotation if you click the right or double click the left mouse button. You can change the annotation’s text, the font size, color, style (italic), weight (bold) or typeface. 

Annotations can be repositioned and resized. An annotation can be selected only when the ‘Annotations’ button is active.

If you select an annotation, you’ll see selection marks (resize points) in every corner and every side. You can resize the annotation box by dragging these marks. You can also reposition the annotation by dragging it around the page.

You can delete a selected annotation with the ‘Delete’ key or you can also right click on the annotation and choose the ‘Delete’ button. 

### Highlights
You can highlight any part of a document by double clicking on a page or by selecting an area. Highlights are editable anytime later.  

![Highlights](https://github.com/SenseNet/sn-preview/raw/master/docs/images/preview-highlights.png "Highlights")

Highlights can be repositioned and resized. If you select a highlighted area, you can reposition it by dragging it to the new position. Around a selected highlighted area you can see selection marks (resize points). You can resize the highlight box by dragging these marks.

A highlight can be selected only when the ‘Highlights’ button is active.

You can delete a selected highlight with the ‘Delete’ key or you can also right click on the highlight and choose the ‘Delete’ button from the context menu. 

### Redactions
You can add redactions to the document by double clicking on it or select a part of it. Redactions are editable anytime later.  

Redactions can be repositioned and resized. If you select a redaction box, you can reposition it by dragging it to the new position. You can see selection marks (resize points) around a selected redaction box. You can resize the highlight box by dragging these marks.

![Redactions](https://github.com/SenseNet/sn-preview/raw/master/docs/images/preview-redactions.png "Redactions")

A redaction can be selected only when the “Redactions” button is active.

You can delete a selected annotation with the ‘Delete’ key or you can also right click on the highlight and choose the ‘Delete’ button from the context menu. 

### Hiding/showing shapes
You can hide or show shapes (annotations, redactions, highlights) by clicking on this button so you can read the document easily. Annotations and highlights can be hidden by all the users who have ‘Restricted preview’ permission. Redactions can be hidden only by users with ‘Preview without redaction’ permission. All the users who don’t have ’Preview without redaction’ permission see a redacted versions of the preview images which means that they can’t see the unredacted version in the browser even if they browse the image only. 

### Hiding/showing watermark
If you have ‘Preview without watermark’ permission and a watermark is set on the document you can hide it and see the original document without watermark. You can switch back to watermarked version with the same button. 

### Save shapes
If you have the rights to save the document you can save the things that you’ve added/edited in the viewer, which means that you don’t save the original e.g. Word document, only the annotations, redactions and highlights will be saved to the file.

![Save shapes](https://github.com/SenseNet/sn-preview/raw/master/docs/images/preview-saveshapes.png "Save shapes")

## Preview permissions
There are three preview-related permissions in sensenet:

- **Restricted preview**: the user can see the preview image, but only with all the restrictions (with watermark and redactions if they are set on the preview).
- **Preview without watermark**: the user can see the preview image without watermark.
- **Preview without redaction**: the user can see the preview image without redaction.

If the user has at least one of the permissions above, than she can see the preview images of the document. Also she will have access to all content metadata, but she cannot download the original document. Only users with *Open* permission are able to access the original document.