
echo "tags reported by git:"
git tag

currentSemVer=`git semver`
echo "current semantic version reported by semver: $currentSemVer"

source_tag=""
source_tag=`/usr/bin/git tag --points-at $BUILD_SOURCEVERSION`

if [ -z $source_tag ]
then
    assemblySemFileVer="$currentSemVer+x"
    informationalVersion="$currentSemVer+x@$BUILD_SOURCEVERSION"
    assemblySemVer="$currentSemVer.0"
else
    assemblySemFileVer="$source_tag"
    informationalVersion="$source_tag@$BUILD_SOURCEVERSION"
    assemblySemVer="$source_tag.0"
fi
echo "AssemblyFileVersion: $assemblySemFileVer"
echo "AssemblyInformationalVersion: $informationalVersion"
echo "AssemblyVersion: $assemblySemVer"

assemblyInfo=$(find . -type f -name AssemblyInfo.cs)
if [[ -z assemblyInfo ]]; then
    echo "AssemblyInfo.cs was not found. Exiting…"
    exit 1
fi
echo "AssemblyInfo.cs found: $assemblyInfo"

sed -i "s:\(.*AssemblyFileVersion(\"\).*\(\")]\):\1$assemblySemFileVer\2:g" "$assemblyInfo"
sed -i "s:\(.*AssemblyInformationalVersion(\"\).*\(\")]\):\1$informationalVersion\2:g" "$assemblyInfo"
sed -i "s:\(.*AssemblyVersion(\"\).*\(\")]\):\1$assemblySemVer\2:g" "$assemblyInfo"

echo "resulting AssemblyInfo.cs file:"
cat "$assemblyInfo"
