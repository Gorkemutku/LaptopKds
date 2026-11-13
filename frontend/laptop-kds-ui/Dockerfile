# ==========================================
# 1. Aşama: Derleme (Build Stage)
# ==========================================
FROM node:20-alpine AS build
WORKDIR /app

# Bağımlılıkları yükle
COPY package*.json ./
RUN npm ci

# Kaynak kodları kopyala ve derle
COPY . .
RUN npm run build

# ==========================================
# 2. Aşama: Nginx ile Sunum (Runtime Stage)
# ==========================================
FROM nginx:alpine AS runtime

# Özel Nginx yapılandırmasını kopyala
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Derlenmiş statik dosyaları Nginx web kök dizinine kopyala
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
