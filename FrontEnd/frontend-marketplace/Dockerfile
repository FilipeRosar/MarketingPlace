# Etapa 1: Build do Angular
FROM node:20 AS build
WORKDIR /app

COPY package*.json ./
RUN npm install --legacy-peer-deps

COPY . .
RUN npm run build --configuration=production

# Etapa 2: Servidor Nginx
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

RUN rm -rf ./*

# Copia os arquivos gerados
COPY --from=build /app/dist/frontend-marketplace/browser .

# --- CORREÇÃO AQUI ---
# Renomeia o index.csr.html para index.html caso o Angular tenha gerado com esse nome
RUN if [ -f index.csr.html ]; then mv index.csr.html index.html; fi

# Configuração do Nginx atualizada para garantir que ele encontre o arquivo correto
RUN echo 'server { \
    listen 80; \
    location / { \
        root /usr/share/nginx/html; \
        index index.html index.htm; \
        try_files $uri $uri/ /index.html; \
    } \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
