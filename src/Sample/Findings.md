# Findings

I did a comparison of the new project template (`dotnet new webapi`) with a toy controller framework (uController). 

I'm doing this kind of comparison because it would be non-trivial to retrofit most of ASP.NET Core with linker-friendly features. However adding linker-friendliness to a constraint simplified framework is easy to do. This also provides a more useful measure of what's ideally possible. 

Our toy framework reuses ASP.NET Core's DI and routing system, but replaces most of MVC's dynamism with its own hardcoded logic. Instead of scanning all assemblies for types that look like controllers - the list in uController is hardcoded in source. This could manifest in a shipping product as part of the user experience (users list controller types/methods) or by doing up front discovery in a build/link-time step.

We began this project with the goal of using link-time IL rewriting to replace this manual registration, and to also attempt to replace more complex features like DI with link-time IL generation. We didn't make much progress on these fronts.

The measureable goal was to determine the impact of removing dynamism and adding applying linking across a few metrics:
- Working set
- Startup time (including first request)
- Size on disk

## Experimental Setup

The main axis of comparison is whether the linker is being run, and what *mode* it's running in. I'm defining the following terms:
- None - no linking/trimming
- Trimming - using the default `PublishTrimmed` project-level settings
- Aggro - using a custom target to set the link action to `link` for all assemblies

*Aggro-mode* goes well out the boundaries of what can work by default for an application, and requires either extensibility for the linker to function, or manually listing lots of roots in an `.xml` file. I took the approach of using the `.xml` descriptor, and used the error output of the DI system to generate parts of the file. I made no attempt to support *aggro-mode* for the template app.

I wrote a powershell script (contained in this repo) to automate the measurement process.

## Hypothesis

1. A *hard-wired* startup experience will have better startup performance than the current MVC experience (scanning) by around 100ms.
2. 

## Results

| Workload    | Mode     | Working Set (MB) | Startup (ms) | Size on Disk (MB) |
|-------------|----------|------------------|--------------|-------------------|
| Template    | None     | 43.5             | 1223.30758   | 84.34             |
| Template    | Trimming | 43.4765625       | 1579.73156   | 44.57             |
| Template    | Aggro    | n/a              | n/a          | n/a               |
| uController | None     | 39.50390625      | 1177.10803   | 85.04             |
| uController | Trimming | 39.3203125       | 1317.14828   | 45.25             |
| uController | Aggro    | 36.80078125      | 1839.00328   | 25.68             |

##