# ═══════════════════════════════════════════════════════════════════════════════
# Logistics Web (React) — Multi-stage Dockerfile
# ═══════════════════════════════════════════════════════════════════════════════

# ── Stage 1: Build React app ──────────────────────────────────────────────────
FROM node:20-alpine AS builder
WORKDIR /app

# Accept build-time Entra ID values (injected by docker-compose build args)
ARG VITE_API_BASE_URL
ARG VITE_ENTRA_CLIENT_ID
ARG VITE_ENTRA_TENANT_ID
ARG VITE_ENTRA_API_SCOPE

# Make them available to Vite during build
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_ENTRA_CLIENT_ID=$VITE_ENTRA_CLIENT_ID
ENV VITE_ENTRA_TENANT_ID=$VITE_ENTRA_TENANT_ID
ENV VITE_ENTRA_API_SCOPE=$VITE_ENTRA_API_SCOPE

# Install dependencies — cached unless package.json changes
COPY src/web/package*.json ./
RUN npm ci --prefer-offline

# Copy source and build
COPY src/web/ .
RUN npm run build

# ── Stage 2: Nginx to serve the built SPA ─────────────────────────────────────
FROM nginx:1.27-alpine AS final

# Remove default nginx config
RUN rm /etc/nginx/conf.d/default.conf

# Copy our custom SPA-aware nginx config
COPY src/web/nginx.conf /etc/nginx/conf.d/default.conf

# Copy built React app
COPY --from=builder /app/dist /usr/share/nginx/html

# Non-root nginx (uses port 8080 internally; mapped to 3000 via docker-compose)
RUN chown -R nginx:nginx /usr/share/nginx/html && \
    chown -R nginx:nginx /var/cache/nginx && \
    chown -R nginx:nginx /var/log/nginx && \
    touch /var/run/nginx.pid && \
    chown -R nginx:nginx /var/run/nginx.pid
USER nginx

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
