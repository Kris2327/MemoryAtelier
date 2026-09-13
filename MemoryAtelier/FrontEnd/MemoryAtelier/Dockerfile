FROM node:22-alpine AS build
WORKDIR /src
COPY MemoryAtelier/FrontEnd/MemoryAtelier/package*.json MemoryAtelier/FrontEnd/MemoryAtelier/
RUN npm ci --prefix MemoryAtelier/FrontEnd/MemoryAtelier
COPY MemoryAtelier/FrontEnd/MemoryAtelier/ MemoryAtelier/FrontEnd/MemoryAtelier/
RUN npm run build --prefix MemoryAtelier/FrontEnd/MemoryAtelier

FROM node:22-alpine
WORKDIR /app
COPY --from=build /src/MemoryAtelier/FrontEnd/MemoryAtelier/dist/MemoryAtelier ./dist/MemoryAtelier
COPY --from=build /src/MemoryAtelier/FrontEnd/MemoryAtelier/package*.json ./
RUN npm ci --omit=dev
ENV PORT=4000
EXPOSE 4000
CMD ["node", "dist/MemoryAtelier/server/server.mjs"]
