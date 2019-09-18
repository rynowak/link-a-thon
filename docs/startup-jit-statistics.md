# Collecting jitted method on startup

In order to optimize for performance - we should avoid jitting method on startup. This is done by the `/p:PublishReadyToRun=true` option. The option invokes `crossgen` to compile the MSIL to native code before they are bundled into a single binary. Unfortunately, subject to various imitations of the tool, we are still jitting some methods during startup. Here is the instruction to figure out what they are:

1. Acquire the tools
2. Hack the system
3. Measure

## Acquire the tools
1. PerfView
2. dotnet-trace

Clone and build [PerfView](https://github.com/Microsoft/perfview) on Windows - it will be required for visualizing the data. On Windows, open PerfView.sln and build the solution (under the Release configuration). 

Clone and build [Diagnostics](https://github.com/dotnet/diagnostics) on Linux - it will be required for collecting the performance trace. Just run `build.sh`.

## Hack the system

As of the time of writing (Summer 2019), `dotnet-trace` only support attaching to a running process. By the time we attach to the process, the startup is already over. We need to make sure we attach before that happens to capture startup behavior. Here I have a hack that repurpose a previously built test-only feature named `auto-trace` to accomplish the goal. The feature is lightly documented [here](https://github.com/dotnet/coreclr/blob/master/Documentation/workflow/AutomatedStressTestingForDiagnosticServer.md).

To enable `auto-trace`, we need to rebuild the CoreCLR. Modify the `clrfeatures.cmake` as follow:

```
cd ~/git/link-a-thon/coreclr
vi ./clrfeatures.cmake
```

Now enable `FEATURE_AUTO_TRACE` by changing it from `0` to `1`.

```
...
if(NOT DEFINED FEATURE_AUTO_TRACE)
  set(FEATURE_AUTO_TRACE 1)
endif(NOT DEFINED FEATURE_AUTO_TRACE)
...
```

Note that `./build-coreclr.sh` might determine the build is already there and not building, we need to make sure it does rebuild. The easiest way to make sure that happen is to clean the tree there.

```
cd ~/git/link-a-thon/coreclr
git clean -fdx . 
cd ~/git/link-a-thon
./build-coreclr.sh
```

## Measure

After the hack, build the application as usual as documented [here](https://github.com/rynowak/link-a-thon/blob/master/docs/build-and-run.md). The application should just work as usual.

To start capturing trace. Set the environment variables as follow:

```
export COMPlus_AutoTrace_N_Tracers=1
export COMPlus_AutoTrace_Command="/home/andrewau/git/diagnostics/src/Tools/dotnet-trace/run.sh collect --providers=Microsoft-Windows-DotNETRuntime:10:5 "
```

The `10` above is the JitKeyword, and `5` means verbose. Together, this options can be used to capture the `MethodJittingStarted` and `MethodLoadVerbose_V1` events. These events will be used to produce the JIT statistics. We might want to change what to capture later and the options for tuning what to capture is documented [here](https://docs.microsoft.com/en-us/dotnet/framework/performance/method-etw-events).

Because the `run.sh` in the `diagnostics` repo expect it to be executed in the same directory, but `auto-trace` will simply launch it at the working directory, we need to hack that script. In particular, adding this line to `./run.sh`

```
cd ~/git/diagnostics/src/Tools/dotnet-trace
vi ./run.sh
```

The changed code should look like this
```
#!/bin/bash
 
cd /home/andrewau/git/diagnostics/src/Tools/dotnet-trace

 echo $USER@`hostname` "$PWD"
```

Running the application would automatically launch `dotnet-trace`. Press enter when the start up activities are consider completed. (e.g. after processing the first request). That should produce a `trace.nettrace` file at the `~/git/diagnostics/src/Tools/dotnet-trace` directory. Once `dotnet-trace` acknowledge the trace is saved. We can kill the server with Control-C.

For reason that I don't understand, the terminal is toasted. It can still execution command as usual, but it doesn't echo typed characters. I would just close it.

Transfer that file to the Windows machine and open it with PerfView. The `Advanced Group` should have a `JITStats` node. Open it can show the statistics.