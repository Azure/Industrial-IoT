FROM microsoft/dotnet

ADD / /build/module

RUN \
		set -ex \
	&& \
		apt-get update && apt-get install -y \
			build-essential \
			libcurl4-openssl-dev \
			git \
			cmake \
			libssl-dev \
			valgrind \
			uuid-dev \
			libglib2.0-dev \
	&& \
		git clone --no-checkout https://github.com/Azure/azure-iot-gateway-sdk /build/gateway \
	&& \
        git -C /build/gateway checkout 287beed07490d98a24a4e9ddd33ec7127fc3acbf \
	&& \
		git -C /build/gateway submodule update --recursive --init \
	&& \
        bash /build/module/bld/build.sh -C Release -i /build/gateway -o /gateway \
	&& \
		ldconfig /gateway/Release

WORKDIR /gateway/Release
ENTRYPOINT ["sample_gateway"]
