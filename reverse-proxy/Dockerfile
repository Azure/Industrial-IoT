FROM nginx:1.13

RUN    groupadd app \
    && useradd --no-log-init -r -g app app

RUN    mkdir -p /app/logs \
    && chown -R app:app /app \
    && mkdir -p /var/lib/nginx /var/cache/nginx /var/tmp/nginx \
    && chown -R app:app /var/lib/nginx /var/cache/nginx /var/tmp/nginx

COPY ./nginx.conf /app/nginx.conf
COPY ./run.sh /app/run.sh
    
WORKDIR /app
VOLUME ["/app/certs"]
USER app
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "/app/run.sh"]

