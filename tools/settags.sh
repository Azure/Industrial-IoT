#!/bin/bash -e

source_tag=""
if [ -e /usr/bin/git ]
then
    source_tag=`/usr/bin/git tag --points-at $BUILD_SOURCEVERSION`
else
    source_tag=`"$PROGRAMFILES\Git\bin\git.exe" tag --points-at $BUILD_SOURCEVERSION`
fi
if [ -z "$source_tag" ] && [ -e "version.props"  ]
then
    echo "Use version.props for source_tag generation"
    version_prefix=`sed -n 's:.*<VersionPrefix>\(.*\)</VersionPrefix>.*:\1:p' < "version.props" `
    source_tag="$version_prefix"
fi

cr_imagetag=""
cr_imagetagprefix=""
cr_latesttag=""
cr_acrnamespace="internal/"
echo "branch: $BUILD_SOURCEBRANCHNAME"

if [ "$BUILD_SOURCEBRANCHNAME" == "master" ]
then
    # image is a release image
    echo "Building release images from master."
    if [ -z "$source_tag" ]
    then
        echo "No source tag exists"
        cr_latesttag="latest"
        cr_imagetag="latest"
        cr_imagetagprefix=""
        cr_acrnamespace="public/"
    else
        echo "Using source tag for release: $source_tag"
        cr_latesttag="latest"
        cr_imagetag="${source_tag}"
        cr_imagetagprefix="${source_tag}-"
        cr_acrnamespace="public/"
    fi
else
    if [ "$BUILD_SOURCEBRANCHNAME" == "develop" ]
    then
        # Develop branch is preview branch
        echo "Building preview images from develop."
        if [ -z "$source_tag" ]
        then
            echo "No source tag exists"
            cr_latesttag="preview"
            cr_imagetag="preview"
            cr_imagetagprefix="preview-"
            # TODO cr_acrnamespace="public/"
        else
            echo "Using source tag for preview: $source_tag"
            cr_latesttag="${source_tag}-preview"
            cr_imagetag="${source_tag}-preview"
            cr_imagetagprefix="${source_tag}-preview-"
            # TODO cr_acrnamespace="public/"
        fi
    else
        # image goes into the internal subtree
        echo "Building internal images from branch: $BUILD_SOURCEBRANCHNAME"
        if [ -z "$source_tag" ]
        then
            echo "No source tag exists"
            cr_latesttag="${BUILD_SOURCEBRANCHNAME}"
            cr_imagetag="$BUILD_SOURCEBRANCHNAME"
            cr_imagetagprefix="${BUILD_SOURCEBRANCHNAME}-"
        else
            echo "Using source tag for internal: $source_tag"
            cr_latesttag="${source_tag}-${BUILD_SOURCEBRANCHNAME}"
            cr_imagetag="${source_tag}-${BUILD_SOURCEBRANCHNAME}"
            cr_imagetagprefix="${source_tag}-${BUILD_SOURCEBRANCHNAME}-"
        fi
    fi
fi
echo "##vso[task.setvariable variable=MY_IMAGETAG;]$cr_imagetag"
echo "MY_IMAGETAG set to $cr_imagetag"
echo "##vso[task.setvariable variable=MY_IMAGETAGPREFIX;]$cr_imagetagprefix"
echo "MY_IMAGETAGPREFIX set to $cr_imagetagprefix"
echo "##vso[task.setvariable variable=MY_ACRNAMESPACE;]$cr_acrnamespace"
echo "MY_ACRNAMESPACE set to $cr_acrnamespace"
echo "##vso[task.setvariable variable=MY_LATESTTAG;]$cr_latesttag"
echo "MY_LATESTTAG set to $cr_latesttag"
