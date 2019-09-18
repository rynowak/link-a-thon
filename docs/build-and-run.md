# Instructions for running the repo.

The instruction is divided into four parts:
1. Preparing the source code
2. Installing dependencies
3. Building the application
4. Running the application.

## Preparing the source code
Obviously, the first step is the clone the repo.

```
cd ~/git
git clone https://github.com/rynowak/link-a-thon
```

Note that the repo contains a submodule that brings us the source code of CoreCLR. We need to obtain it.

```
cd ~/git/link-a-thon
git submodule update --init
```

This will check out the commit referenced in the submodule - a recent version of the single-exe branch. To use a different version of coreclr, check out the desired commit instead.

## Installing dependencies

With that, the source code is ready to be built. Now we setup the necessary tools for the build.

Install .NET Core rc1 bits from [here](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/3.0.1xx/dotnet-sdk-latest-linux-x64.tar.gz). A random .NET Core SDK version might not work.

To install this, download this into `~/dotnet` and expand the tarball.
```
mkdir ~/dotnet
pushd ~/dotnet
wget https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/3.0.1xx/dotnet-sdk-latest-linux-x64.tar.gz
tar xvfz dotnet-sdk-latest-linux-x64.tar.gz
rm dotnet-sdk-latest-linux-x64.tar.gz
popd
```

Make sure you have the dependencies for building CoreCLR installed, please refer to [here](https://github.com/dotnet/coreclr/blob/master/Documentation/building/linux-instructions.md) for a reference. 

## Building the application

With all the tools installed, we can start building. Build the CoreCLR bits first, this will take a while:
```
cd ~/git/link-a-thon
./build-coreclr.sh
```

After building the CoreCLR, we can build the application itself.

```
dotnet publish -c Release -r linux-x64 /p:PublishTrimmed=true /p:LinkAggressively=true /p:UsePublishFilterList=true /p:PublishReadyToRun=true /p:UseStaticHost=true /p:SelfContained=true src/ApiTemplate/ApiTemplate.csproj -o publish
```

Note that this build produce a single binary `publish/ApiTemplate`. The size of the binary on my machine is 30,686,485 bytes.

## Running the application

After building the application, we can run it:
```
cd ~/git/link-a-thon
publish/ApiTemplate
```

## Running the application

The application should be able to predict weather!
```
wget http://localhost:5000/WeatherForecast
```
