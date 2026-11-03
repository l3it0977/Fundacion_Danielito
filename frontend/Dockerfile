# Interfaz web del Sistema Danielito: se compila con Node y se sirve con nginx.
# Se construye con el contexto en esta carpeta (frontend/).

FROM node:22-alpine AS compilar
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Dirección de la API tal como la ve el navegador. Con "/api" todo sale por el mismo origen
# (nginx reenvía /api a la API), así no hace falta configurar CORS. Se "hornea" al compilar:
# cambiarla obliga a reconstruir la imagen.
ARG VITE_API_URL=/api
ENV VITE_API_URL=$VITE_API_URL

RUN npm run build

FROM nginx:1.27-alpine

# Las plantillas de /etc/nginx/templates pasan por envsubst al arrancar el contenedor.
ENV API_UPSTREAM=http://api:8080

COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=compilar /app/dist /usr/share/nginx/html

EXPOSE 80
