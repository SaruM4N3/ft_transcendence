#stage 1: build
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
#1
RUN npm ci
COPY . . 
RUN npm run build

#stage 2: run
FROM node:22-alpine
WORKDIR /app
COPY --from=build /app/build ./build
COPY --from=build /app/package*.json ./
#2
RUN npm ci --omit=Developers
USER node
EXPOSE 3000
CMD ["node", "build"]