# Multi-stage build: compile the Angular app with Node, serve the static files with nginx.
# Build context is the web/ folder (see compose.yaml).

# ---------- build ----------
FROM node:24-alpine AS build
WORKDIR /app

# Install packages first so this layer is only rebuilt when the lock file changes.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---------- runtime ----------
# The unprivileged image runs nginx as a non-root user and listens on 8080, like the API container.
FROM nginxinc/nginx-unprivileged:alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/pulsewallet-web/browser /usr/share/nginx/html

EXPOSE 8080
