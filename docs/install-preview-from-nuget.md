# Install sensenet Preview from NuGet

This article is **for developers** about installing the **Preview** component for [sensenet ECM](https://github.com/SenseNet) from NuGet. Before you can do that, please install at least the core layer, [sensenet Services](/docs/install-sn-from-nuget), which is a prerequisite of this component.

>About choosing the components you need, take a look at [this article](/docs/sensenet-components) that describes the main components and their relationships briefly.

![sensenet Preview](https://github.com/SenseNet/sn-resources/raw/master/images/sn-components/sn-components_preview.png "sensenet Preview")

## The SenseNet.Preview.Install package
This component contains the preview modules needed in sensenet ECM to **access and display** preview images. It also contains the mechanism for **initiating preview generation**, when it is available.

> This package **does not contain** the task executor tool for actually generating preview images, because the tool offered by Sense/Net Inc. uses the [Aspose](http://aspose.com) libraries that cannot be published here. The source code written by us however is available here.

This means that even community customers can install the Preview component and display preview images using the **Document Viewer**, if they create their own *custom tool* for generating preview images.

> This package also contains demo files and pre-generated preview images so that you can install and try it freely, without having to buy the sensenet ECM or [Aspose](http://aspose.com) licence.

## The Aspose package
There is a separate NuGet package that contains the preview generator tool and other server-side modules needed for generating preview images. It is built using the [Aspose](http://aspose.com) libraries and it is available only from a private sensenet ECM NuGet feed. 

If you need this feature, you have two options:

- Become a sensenet ECM *Enterprise* customer and receive this package for free.
- Use the *Community* version of sensenet ECM (available publicly on github and NuGet) and purchase an *Aspose licence* separately. In that case you will be able to compile and use our preview generator tool, without a sensenet ECM Enterprise licence.

## Installation
To get started, stop your web site and install the preview package the usual way:

1. Open your web application that already contains the *Services* component installed in *Visual Studio*.
2. Install the following NuGet package (either in the Package Manager console or the Manage NuGet Packages window)

[![NuGet](https://img.shields.io/nuget/v/SenseNet.Preview.Install.svg)](https://www.nuget.org/packages/SenseNet.Preview.Install)

> `Install-Package SenseNet.Preview.Install -Pre`

### Install the Preview component
1. Open a command line and go to the *[web]\Admin\bin* folder of your project.
2. Execute the install-preview command with the SnAdmin tool.

```text
.\snadmin install-preview
```

Optionally, if you have installed the *WebPages* and *Workspaces* components, you may add the usual `importdemo:true` parameter to the line above. That will give you a couple of pregenerated preview images for trying out the feature (you will be able to find them in the Budapest document workspace in the Content Repository).

### Install the Aspose component

> Accessible for **Enterprise** customers.

1. Open a command line and go to the *[web]\Admin\bin* folder of your project.
2. Execute the install-preview-aspose command with the SnAdmin tool.

```text
.\snadmin install-preview-aspose
```


If there were no errors, you are good to go! Hit F5 in Visual Studio and start experimenting with sensenet ECM Preview!