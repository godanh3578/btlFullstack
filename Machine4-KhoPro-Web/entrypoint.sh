#!/bin/sh
GATEWAY_URL=${GATEWAY_URL:-"http://localhost:7000/gateway"}
USER_SERVICE_URL=${USER_SERVICE_URL:-"http://localhost:8083/api"}

sed "s|__GATEWAY_URL__|${GATEWAY_URL}|g; s|__USER_SERVICE_URL__|${USER_SERVICE_URL}|g" \
  /usr/share/nginx/html/env-config.js.template \
  > /usr/share/nginx/html/env-config.js

nginx -g "daemon off;"
