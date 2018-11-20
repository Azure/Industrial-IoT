#!/bin/bash -e
CONFIG="/app/nginx.conf"
if [ -f "/app/certs/tls.key" ]; then
    sed -i -e "s/#listen 10443 ssl;/listen 10443 ssl;/g" ${CONFIG}
    
    sed -i -e "s/#ssl_certificate;/ssl_certificate \/app\/certs\/tls.crt;/g" ${CONFIG}
    sed -i -e "s/#ssl_certificate_key;/ssl_certificate_key \/app\/certs\/tls.key;/g" ${CONFIG}
    sed -i -e "s/#ssl_protocols;/ssl_protocols TLSv1.2;/g" ${CONFIG}
    
    echo "Starting reverse proxy with ssl endpoint"
else
    sed -i -e "s/listen 10443 ssl;/#listen 10443 ssl;/g" ${CONFIG}
    
    sed -i -e "s/ssl_certificate \/app\/certs\/tls.crt;/#ssl_certificate;/g" ${CONFIG}
    sed -i -e "s/ssl_certificate_key \/app\/certs\/tls.key;/#ssl_certificate_key;/g" ${CONFIG}
    sed -i -e "s/ssl_protocols TLSv1.2;/#ssl_protocols;/g" ${CONFIG}

    echo "Starting reverse proxy without ssl"
fi
mkdir -p /app/logs
nginx -c ${CONFIG}
