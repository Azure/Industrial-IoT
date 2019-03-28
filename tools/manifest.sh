#!/bin/bash -ex

ignoredTags=("x.y.z" "a.b.c")
#WINVERSION="10.0.17763.194"
MANIFESTFILENAMEBASE="manifest"
INCLUDE_ARM="false"

while [ "$#" -gt 0 ]; do
    case "$1" in
        --registry)                     MY_REGISTRY="$2" ;;
        --user)                         MY_USERNAME="$2" ;;
        --password)                     MY_PASSWORD="$2" ;;
        --image-name)                   MY_SUBPATH="$2" ;;
        --namespace)                    MY_ACRNAMESPACE="$2" ;;
        --tag)                          MY_IMAGETAG="$2" ;;
        --tag-prefix)                   MY_IMAGETAGPREFIX="$2" ;;
        --include-arm)                  INCLUDE_ARM="$2" ;;
    esac
    shift
done

IMAGE="${MY_REGISTRY}/${MY_ACRNAMESPACE}${MY_SUBPATH}"

# only handle public images
if [ ${MY_ACRNAMESPACE} != "public/" ]
then
    echo "Manifests are only supported for public images in the ACR. Exiting…"
    exit
fi

function ExtractSemanticVersion {
    [[ $1 =~ ([0-9]*).* ]]
    if [[ ${#BASH_REMATCH[*]} > 1 ]]; then
        majorVersion=${BASH_REMATCH[1]}
    fi
    [[ $1 =~ [0-9]*.([0-9]*).* ]]
    if [[ ${#BASH_REMATCH[*]} > 1 ]]; then
        minorVersion=${BASH_REMATCH[1]}
    fi
    [[ $1 =~ [0-9]*.[0-9]*.([0-9]*) ]]
    if [[ ${#BASH_REMATCH[*]} > 1 ]]; then
        patchVersion=${BASH_REMATCH[1]}
    fi
    [[ $1 =~ ([0-9]*.[0-9]*).* ]]
    if [[ ${#BASH_REMATCH[*]} > 1 ]]; then
        majorMinorVersion=${BASH_REMATCH[1]}
    fi
}

# always process latest
echo ""
echo "Generating manifest for tag latest"
manifestFile="$MANIFESTFILENAMEBASE.latest.yaml"
echo "image: $IMAGE:latest" > $manifestFile
echo "manifests:" >> $manifestFile
echo "  -" >> $manifestFile
echo "    image: $IMAGE:${MY_IMAGETAGPREFIX}linux-amd64" >> $manifestFile
echo "    platform:" >> $manifestFile
echo "      architecture: amd64" >> $manifestFile
echo "      os: linux" >> $manifestFile
if [ ${INCLUDE_ARM} != "false" ]
then
echo "  -" >> $manifestFile
echo "    image: $IMAGE:${MY_IMAGETAGPREFIX}linux-arm32v7" >> $manifestFile
echo "    platform:" >> $manifestFile
echo "      architecture: arm" >> $manifestFile
echo "      os: linux" >> $manifestFile
fi
echo "  -" >> $manifestFile
echo "    image: $IMAGE:${MY_IMAGETAGPREFIX}windows-amd64" >> $manifestFile
echo "    platform:" >> $manifestFile
echo "      architecture: amd64" >> $manifestFile
echo "      os: windows" >> $manifestFile
#echo "      os.version: $WINVERSION" >> $manifestFile

echo "Generated manifest file $manifestFile:"
cat $manifestFile

# push manifest into registry
echo ""
echo "Push manifest to registry"
~/manifest-tool --username $MY_USERNAME --password $MY_PASSWORD push from-spec $manifestFile

if [[ $MY_IMAGETAG = "latest" ]]; then
    echo "Only latest processing is required. Exiting..."
    exit
fi

# stop in case of an ignored tag
for i in ${ignoredTags[@]}; do
    if [[ $MY_IMAGETAG = $i ]]; then
        echo "Manifest generation for tag $MY_IMAGETAG is ignored. Exiting..."
        exit
    fi
done

# for tagged images, we need to push an additional manifest
echo ""
echo "Generating manifest for tag $MY_IMAGETAG"
manifestFile="$MANIFESTFILENAMEBASE.$MY_IMAGETAG.yaml"
echo "image: $IMAGE:$MY_IMAGETAG" > $manifestFile
ExtractSemanticVersion $MY_IMAGETAG
echo "tags: [ '$majorVersion', '$majorMinorVersion' ]" >> $manifestFile
echo "manifests:" >> $manifestFile
echo "  -" >> $manifestFile
echo "    image: $IMAGE:${MY_IMAGETAGPREFIX}linux-amd64" >> $manifestFile
echo "    platform:" >> $manifestFile
echo "      architecture: amd64" >> $manifestFile
echo "      os: linux" >> $manifestFile
if [ ${INCLUDE_ARM} != "false" ]
then
echo "  -" >> $manifestFile
echo "    image: $IMAGE:${MY_IMAGETAGPREFIX}linux-arm32v7" >> $manifestFile
echo "    platform:" >> $manifestFile
echo "      architecture: arm" >> $manifestFile
echo "      os: linux" >> $manifestFile
fi
echo "  -" >> $manifestFile
echo "    image: $IMAGE:${MY_IMAGETAGPREFIX}windows-amd64" >> $manifestFile
echo "    platform:" >> $manifestFile
echo "      architecture: amd64" >> $manifestFile
echo "      os: windows" >> $manifestFile
#echo "      os.version: $WINVERSION" >> $manifestFile
echo "Generated manifest file $manifestFile:"
cat $manifestFile

# push manifest into registry
echo ""
echo "Push manifest to registry"
~/manifest-tool --username $MY_USERNAME --password $MY_PASSWORD push from-spec $manifestFile
