FROM microsoft/dotnet:1.1-sdk

COPY / /build

RUN \
        set -ex \
    && \
        apt-get update && apt-get install -y \
            build-essential \
            git cmake \
            libcurl4-openssl-dev libssl-dev \
            libavahi-compat-libdnssd-dev \
            dbus rsyslog avahi-daemon avahi-utils \
    && \
        git clone https://github.com/marcschier/UA-LDS.git /lds \
    &&  cd /lds \
    &&  git submodule init \
    &&  git submodule update \
    && \
        rm -rf /lds/build && mkdir /lds/build && cd /lds/build \
    &&  ls -l /lds  \
    &&  cmake .. && cmake --build . \
    && \
        cp /lds/docker-initd.sh /etc/init.d/lds \
    &&  echo "service rsyslog start" >> /etc/init.d/lds \
    &&  echo "service dbus start" >> /etc/init.d/lds \
    &&  echo "service avahi-daemon restart --no-drop-root --daemonize --syslog" >> /etc/init.d/lds \
    &&  echo "./lds/build/bin/ualds -c /lds/etc/ualds.conf " >> /etc/init.d/lds \
    &&  chmod +x /etc/init.d/lds \
    && \
        echo "#!/bin/bash" > /lds/start.sh \
    &&  echo "service start lds" >> /lds/start.sh \
    &&  echo "export LD_LIBRARY_PATH=/build/src/GatewayApp.NetCore/bin/Debug/netcoreapp1.1" >> /lds/start.sh \
    &&  echo 'exec /build/src/GatewayApp.NetCore/bin/Debug/netcoreapp1.1/GatewayApp.NetCore $@' >> /lds/start.sh \
    &&  chmod +x /lds/start.sh

EXPOSE 5353

WORKDIR /build
RUN dotnet restore
RUN dotnet publish -c Release -f netcoreapp1.1 -r debian.8-x64 -o bin/Debug/netcoreapp1.1

ENTRYPOINT ["/lds/start.sh"]
