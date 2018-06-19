FROM vbfox/fable-build:aspnet-2.1.300-stretch-mono-5.12.0.226-node-10.4.1-yarn-1.7.0 AS builder

WORKDIR /build

# Package lock files are copied independently and their respective package
# manager are executed after.
#
# This is voluntary as docker will cache images and only re-create them if
# the already-copied files have changed, by doing that as long as no package
# is installed or updated we keep the cached container and don't need to
# re-download.

# Initialize node_modules
COPY package.json yarn.lock ./
RUN yarn install

# Initialize paket packages
COPY paket.dependencies paket.lock NuGet.config ./
COPY .paket .paket
RUN mono .paket/paket.exe restore

# Copy everything else and run the build
COPY . ./
RUN rm -rf deploy && \
    ./build.sh BundleClient

FROM microsoft/dotnet:2.1.1-aspnetcore-runtime-alpine3.7
WORKDIR /app
COPY --from=builder /build/deploy ./
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]
