FROM node:22-alpine AS build
WORKDIR /src
COPY MemoryAtelier/FrontEnd/MemoryAtelier/package*.json MemoryAtelier/FrontEnd/MemoryAtelier/
RUN npm ci --prefix MemoryAtelier/FrontEnd/MemoryAtelier
COPY MemoryAtelier/FrontEnd/MemoryAtelier/ MemoryAtelier/FrontEnd/MemoryAtelier/
RUN npm run build --prefix MemoryAtelier/FrontEnd/MemoryAtelier

FROM nginx:alpine
COPY --from=build /src/MemoryAtelier/FrontEnd/MemoryAtelier/dist/MemoryAtelier/browser /usr/share/nginx/html
COPY MemoryAtelier/FrontEnd/MemoryAtelier/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
