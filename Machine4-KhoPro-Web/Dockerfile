FROM nginx:alpine
WORKDIR /usr/share/nginx/html
COPY . .
RUN rm -f Dockerfile README.md start-web.bat open-firewall-5173-admin.bat
RUN chmod +x /usr/share/nginx/html/entrypoint.sh
EXPOSE 80
ENTRYPOINT ["/usr/share/nginx/html/entrypoint.sh"]
