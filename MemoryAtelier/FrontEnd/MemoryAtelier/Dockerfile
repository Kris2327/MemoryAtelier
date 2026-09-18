FROM node:22-alpine AS build
WORKDIR /src
COPY MemoryAtelier/FrontEnd/MemoryAtelier/package*.json MemoryAtelier/FrontEnd/MemoryAtelier/
RUN npm ci --prefix MemoryAtelier/FrontEnd/MemoryAtelier
COPY MemoryAtelier/FrontEnd/MemoryAtelier/ MemoryAtelier/FrontEnd/MemoryAtelier/
RUN npm run build --prefix MemoryAtelier/FrontEnd/MemoryAtelier

# Angular SSR билдът излиза само в browser+server chunk-ове, напълно самостоятелни
# (виж dist/MemoryAtelier/server/server.mjs) — не се нуждае от node_modules, за да работи.
FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
COPY --from=build /src/MemoryAtelier/FrontEnd/MemoryAtelier/dist/MemoryAtelier ./dist/MemoryAtelier
EXPOSE 4000
CMD ["node", "dist/MemoryAtelier/server/server.mjs"]
