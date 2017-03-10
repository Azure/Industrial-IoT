#!/bin/bash
# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e

repo_root=$(cd "$(dirname "$0")/.." && pwd)

skip_unittests=OFF
skip_dotnet=0
use_zlog=OFF

build_root="${repo_root}/build"
build_rel_root="${build_root}/release"
build_sdk_root="$(cd "$(dirname "$0")/../.." && pwd)/azure-iot-gateway-sdk"
build_clean=0
build_pack_only=0
build_configs=()
build_runtime=

usage ()
{
    echo "build.sh [options]"
    echo "options"
    echo " -c --clean                  Build clean (Removes previous build output)."
    echo " -C --config ^<value^>         [Debug, Release] build configuration"
    echo " -i --sdk-root ^<value^>       [../azure-iot-gateway-sdk] Gateway SDK repo root."
    echo " -o --output ^<value^>         [/build/release] Root in which to place release."
    echo " -x --xtrace                 print a trace of each command"
    exit 1
}

# -----------------------------------------------------------------------------
# -- Parse arguments
# -----------------------------------------------------------------------------
process_args ()
{
    save_next_arg=0
    for arg in $*; do
		  if [ $save_next_arg == 1 ]; then
			build_rel_root="$arg"
			save_next_arg=0
		elif [ $save_next_arg == 2 ]; then
			build_configs+=("$arg")
			save_next_arg=0
		elif [ $save_next_arg == 3 ]; then
			build_sdk_root="$arg"
			save_next_arg=0
		else
			case "$arg" in
				-x | --xtrace)
					set -x;;
				-o | --build-root)
					save_next_arg=1;;
				-C | --config)
					save_next_arg=2;;
				-c | --clean)
					build_clean=1;;
				-i | --sdk-root)
					save_next_arg=3;;
				*)
					usage;;
			esac
		fi
    done

    if [ ! -e "${build_sdk_root}/tools/build.sh" ]; then
        echo "No gateway sdk installed at ${build_sdk_root}... "
	    build_sdk_root=
    fi
}

# -----------------------------------------------------------------------------
# -- build the sdk
# -----------------------------------------------------------------------------
sdk_build()
{
    if [ -z "${build_sdk_root}" ]; then
        echo "Skipping sdk build..."
    else
        echo -e "\033[1mBuilding sdk...\033[0m"
        for c in ${build_configs[@]}; do
            if [ -e "${build_root}/sdk/${c}.done" ]; then
                echo "Skipping building sdk ${c}..."
            else
                echo -e "\033[1m    ${c}...\033[0m"

                mkdir -p "${build_root}/sdk/${c}"

                rm -r -f "${build_sdk_root}/build" \
                    || return 1
                rm -r -f "${build_sdk_root}/install-deps" \
                    || return 1

                pushd ${build_sdk_root}/tools > /dev/null
					( ./build.sh --config ${c} --enable-dotnet-core-binding --disable-ble-module ) \
						|| return $build_error
                popd > /dev/null

                cp -r "${build_sdk_root}/build/"* "${build_root}/sdk/${c}" || \
                    return 1

                echo "${c}" >> "${build_root}/sdk/${c}.done"
            fi
        done
    fi
	return 0
}

# -----------------------------------------------------------------------------
# -- build module and publish
# -----------------------------------------------------------------------------
module_build()
{
	echo -e "\033[1mBuilding module...\033[0m"
	for c in ${build_configs[@]}; do
		echo -e "\033[1m    ${c}...\033[0m"
		mkdir -p "${build_root}/module/${c}"

		pushd "${repo_root}/src/Opc.Ua.Client.Module" > /dev/null
			dotnet restore \
				|| return $?
			dotnet build -c ${c} --framework netstandard1.6 \
				|| return $?
		popd > /dev/null

		pushd "${repo_root}/bld/publish" > /dev/null
			dotnet restore \
				|| return $?
			dotnet publish -c ${c} -o "${build_root}/module/${c}" --framework netcoreapp1.1 \
				|| return $?
		popd > /dev/null
	done
	return 0
}

# -----------------------------------------------------------------------------
# -- Copy everything into a final release folder
# -----------------------------------------------------------------------------
release_all()
{
	for c in ${build_configs[@]}; do
        rm -rf "${build_rel_root}/${c}"
        mkdir -p "${build_rel_root}/${c}"

        pushd "${build_root}/module/${c}" > /dev/null
			cp * "${build_rel_root}/${c}" > /dev/null
            find ./runtimes/unix -type f -print0 | xargs -0 -I%%% cp %%% "${build_rel_root}/${c}" || \
                return 1
        popd > /dev/null

        pushd "${build_rel_root}/${c}" > /dev/null
            rm -f publish.*
        popd > /dev/null

        if [ "${build_sdk_root}" ]; then
            pushd "${build_root}/sdk/${c}" > /dev/null
                find . -wholename *dotnetcore.so -type f -print0 | xargs -0 \
                        -I%%% cp %%% "${build_rel_root}/${c}" || \
                    return 1
                find . -wholename *iothub*.so -type f -print0 | xargs -0 \
                        -I%%% cp %%% "${build_rel_root}/${c}" || \
                    return 1
                find . -wholename *gateway*.so -type f -print0 | xargs -0 \
                        -I%%% cp %%% "${build_rel_root}/${c}" || \
                    return 1
                find . -wholename *aziotsharedutil*.so -type f -print0 | xargs -0 \
                        -I%%% cp %%% "${build_rel_root}/${c}" || \
                    return 1
                cp -r "samples/dotnet_core_module_sample/dotnet_core_module_sample" \
                        "${build_rel_root}/${c}/sample_gateway" || \
                    return 1
            popd > /dev/null
        fi
        cp -r "${repo_root}/samples/"*.json "${build_rel_root}/${c}" || \
            return 1
	done
	return 0
}

pushd "${repo_root}" > /dev/null
process_args $*

if [ -z "$build_configs" ]; then
	build_configs=(Debug Release)
fi

echo "Building ${build_configs[@]}..."

if [ $build_clean == 1 ]; then
    echo "Cleaning previous build output..."
    rm -r -f "${build_root}"
fi

mkdir -p "${build_root}"

sdk_build || exit 1
module_build || exit 1
release_all || exit 1

popd > /dev/null

[ $? -eq 0 ] || exit $?

