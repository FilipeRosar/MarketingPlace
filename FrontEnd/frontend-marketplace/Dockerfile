# Etapa 1: Build do Angular
FROM node:20 AS build
WORKDIR /app

# Copia package.json e instala dependências
COPY package*.json ./
RUN npm install --legacy-peer-deps

# Copia o código fonte e gera o build de produção
COPY . .
RUN npm run build --configuration=production

# Etapa 2: Servidor Nginx para rodar o site
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Remove arquivos padrão do nginx
RUN rm -rf ./*

# Copia os arquivos gerados pelo Angular (dist) para o Nginx
# OBS: Verifique no seu angular.json se a pasta de saída é "dist/marketplace-artesanato" ou apenas "dist"
COPY --from=build /app/dist/frontend-marktplace/browser .

# Configuração necessária para o roteamento do Angular funcionar no Nginx
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
