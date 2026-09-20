# Pin Node to a version that satisfies Angular 22 engine requirements (see package.json / lockfile).
FROM node:22.22-bookworm-slim

WORKDIR /app

COPY frontend/care-home-web/package.json frontend/care-home-web/package-lock.json ./
# npm ci requires a perfectly synced lockfile; npm install resolves the demo image reliably.
RUN npm install --no-audit --no-fund

COPY frontend/care-home-web/ ./

EXPOSE 4200

HEALTHCHECK --interval=15s --timeout=5s --start-period=90s --retries=12 \
    CMD node -e "require('http').get('http://127.0.0.1:4200', (r) => process.exit(r.statusCode === 200 ? 0 : 1)).on('error', () => process.exit(1))"

CMD ["npx", "ng", "serve", "--host", "0.0.0.0", "--port", "4200", "--proxy-config", "proxy.conf.docker.json"]
