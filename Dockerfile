FROM microsoft/dotnet:1.1-sdk

COPY /src /build

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
    &&  sed 's/LogSystem[[:blank:]]*=.*/LogSystem=file/' /lds/etc/ualds.conf > /lds/etc/ualds.conf \
    &&  sed 's/LogFile[[:blank:]]*=.*/LogFile=\/app\/Logs\/opcualds.log/' /lds/etc/ualds.conf > /lds/etc/ualds.conf \
    &&  sed 's/LogFileSize[[:blank:]]*=.*/LogFileSize=10/' /lds/etc/ualds.conf > /lds/etc/ualds.conf \
    &&  sed 's/CertificateStorePath[[:blank:]]*=.*/CertificateStorePath=\/app\/Shared\/ualds/' /lds/etc/ualds.conf > /lds/etc/ualds.conf \
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
    &&  echo "service lds start" >> /lds/start.sh \
    &&  echo "until [ -e /app/Shared/ualds/own/certs/ualdscert.der ]; do" >> /lds/start.sh \
    &&  echo "    sleep 3 " >> /lds/start.sh \
    &&  echo "done" >> /lds/start.sh \
    &&  echo 'cp /app/Shared/ualds/own/certs/ualdscert.der "/app/Shared/CertificateStores/UA Applications/certs"' >> /lds/start.sh \
    &&  echo 'chmod u+x "/app/Shared/CertificateStores/UA Applications/certs/ualdscert.der"' >> /lds/start.sh \
    &&  echo 'rm -rf /app/Shared/ualds/trusted/certs' >> /lds/start.sh \
    &&  echo 'ln -s "/app/Shared/CertificateStores/UA Applications/certs" /app/Shared/ualds/trusted/certs' >> /lds/start.sh \
    &&  echo 'exec dotnet $@' >> /lds/start.sh \
    &&  chmod +x /lds/start.sh

EXPOSE 5353

WORKDIR /build
RUN dotnet restore
RUN dotnet publish -c Release -o out
WORKDIR /build/out
ENTRYPOINT ["/lds/start.sh"]

