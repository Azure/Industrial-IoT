FROM nginx:latest
COPY ./reverse-proxy/nginx.conf /app/nginx.conf
EXPOSE 10080
ENTRYPOINT ["nginx", "-c", "/app/nginx.conf"]

