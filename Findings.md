# Findings

We wanted to understand what it would take to write a linker-friendly REST API stack for ASP.NET Core - and what the impact would be across a few metrics:
- Working set
- Startup time (including first request)
- Size on disk

The set of steps would look like:
- Make a sample application work with the linker (using XML annotations)
- Conduct measurements and make comparisons between different modes of linking
- In parallel, use linker extensiblity to remove the need for annotations and runtime reflection/codegen

For the hackathon we're using a toy controller framework (uController). I'm doing this kind of comparison because it would be non-trivial to retrofit most of ASP.NET Core with linker-friendly features. However adding linker-friendliness to a constraint simplified framework is easy to do. This also provides a more useful measure of what's ideally possible. 

Our toy framework reuses ASP.NET Core's DI and routing system, but replaces most of MVC's dynamism with its own hardcoded logic. Instead of scanning all assemblies for types that look like controllers - the list in uController is hardcoded in source. This could manifest in a shipping product as part of the user experience (users list controller types/methods) or by doing up front discovery in a build/link-time step.

We began this project with the goal of using link-time IL rewriting to replace this manual registration, and to also attempt to replace more complex features like DI with link-time IL generation. We didn't make much progress on these fronts.

## Executive Summary

- It's possible for us to get ASP.NET Core to place where the linker can operate upon it (*link* action)
- It's going to be very hard for us to remove all dynamism from ASP.NET Core, and orthogonal to the linker dicsussion
- The primary benefit wrt size of the linker with `PublishTrimmed` is removing ready-to-run images
- If ASP.NET Core is linker-friendly we can reduce size by 50% and keep all of the startup performance

## Experimental Setup

The main axis of comparison is whether the linker is being run, and what *mode* it's running in. I'm defining the following terms:
- None - no linking/trimming
- Trimming - using the default `PublishTrimmed` project-level settings with `PublishReadyToRun`
- Aggro - using a custom target to set the link action to `link` for all assemblies
- Aggro-no-r2r - same as above but without `PublishReadyToRun`

Note that for *None*, *Trimming*, and *Aggro* - all of these modes have ready-to-run images. This choice is made for two reasons:
- Ready-to-run is the default for *None* for all of the shared-framework assemblies
- *Trimming* will result in the ready-to-run data being preserved for *most* but not all assemblies

Specifying that ready-to-run is enabled for all of these configurations makes it clear that we're comparing apples-to-apples, and also ensures that we're never testing a configuration where half of the assemblies have ready-to-run applied.

*Aggro-mode* goes well out the boundaries of what can work by default for an application, and requires either extensibility for the linker to function, or manually listing lots of roots in an `.xml` file. I took the approach of using the `.xml` descriptor, and used the error output of the DI system to generate parts of the file. I made no attempt to support *aggro-mode* for the template app.

I wrote a powershell script (contained in this repo) to automate the measurement process.

## Hypotheses

1. Publishing with *trimming* will result in a significant (~40%) size reduction versus the default.
2. Publishing with *aggro* mode will result in a further significant size reduction versus *trimming*.
3. Publishing with *aggro* mode will have an effect on the overall working set, as assemblies that need to be read are smaller.
4. Publishing with *trimming* or *aggro* mode will moderately improve the startup performance as size and number of files that need to be read are smaller.
5. A *hard-wired* startup experience will have better startup performance than the current MVC experience (scanning) by around 100ms. This means our toy framework should have better startup performance because its doing less work
6. There's a signficant reduction in working set to be realized from avoiding features like expressions or ref-emit used to generate dynamic code in ASP.NET Core.

## Results

| Workload    | Mode         | Working Set (MB) | Startup (ms) | Size on Disk (MB) |
|-------------|--------------|------------------|--------------|-------------------|
| Template    | None         | 43.25            | 1209.24240   | 84.35             |
| Template    | Trimming     | 42.421875        | 1207.92251   | 74.52             |
| Template    | Aggro        | n/a              | n/a          | n/a               |
| uController | None         | 39.43359375      | 865.06628    | 86.18             |
| uController | Trimming     | 38.73828125      | 911.25551    | 76.36             |
| uController | Aggro        | 36.4921875       | 868.71663    | 44.05             |
| uController | Aggro-no-r2r | 36.609375        | 1380.54862   | 25.68             |

## Conclusions

This was a fun hackathon project, and I ended up spending way more time on the linker aspect of this than I did on code generation. Given the improvement in startup times that we observed, this could be worthwhile regardless of whether the linker is involved.

**1. Publishing with *trimming* will result in a significant (~40%) size reduction versus the default.**

It depends: That's not a very satisfying answer is it. 

The most significant benefit of `PublishTrimmed` is that it removes the ready-to-run images from assemblies. I had to test some additional configurations to explore this.

Here's a small data sample.

| Workload    | None     | Trimming (PublishTrimmed) | Trimming + R2R | Trimming - R2R    |
|-------------|----------|---------------------------|--------------  |-------------------|
| uController | 84.35    | 45.25                     | 74.52          | 39.71             |

I had to set `PublishReadyToRun` to produce the *Trimming + R2R* configuration. This uses the built in setting to apply ready-to-run after trimming.

I had to write some custom MSBuild to produce the *Trimming - R2R* configuration. This tells the linker to remove ready-to-run from all of the assemblies.

Given that the *Trimming* (`PublishReadyToRun`) output size is between these two other data points, we have to conclude that *some* of the build output is ready-to-run but most is not.

Given that the *Trimming + R2R* output size is so close to the default we have to conclude the that actual effect of removing unreferenced assemblies is small (-~10mb), however the effect of removing ready to run is big (estimated ~35mb) based on the difference between *Trimming + R2R* and *Trimming - R2R*.

So this leaves us in a wierd spot, because what we're shipping in the product behind `PublishTrimmed` is somewhat of an inconsistent state.
- To optimize for size at the cost of startup, we should remove ready-to-run and deliver a significant size reduction (~6mb more)
- To optimize for startup and size where possible, we should apply ready-to-run to everything and accept a paltry size reduction (~10mb).

**2. Publishing with *aggro* mode will result in a further significant size reduction versus *trimming*.**

Proven: Using *aggro mode + R2R* reduces the app to 50% of original size, using *aggro mode - R2R* reduces the app to 30% of original size.

I'll repeat again that *aggro* mode does linking on all assemblies (including the application). Understanding what is required to make ASP.NET Core successful in this more was one of the key outputs of the hackathon.

We took the approach of using the XML manifest to list types that need to be preserved. Some others were working on writing linker extensibility to make this unnecessary. We made some moderate progress on that, but not enough to obsolete the XML file.

Here's roughly what was required to be listed:
- Add all DI service implementations
- Types used with `IOptions<>`
- Controller types
- POCO types used in serialization

Now we get to defining a term like *linker-friendly* (or *AOT-friendly*). I'm going to define *linker-friendly* as: you can run the linker all the whole app in *link* mode, and produce a working app.

I think we reached the goal of making ASP.NET Core linker friendly using the XML file, but that's not an experience we can give customers. If we want to pursue this, I think we have two options:
- Use linker extensibility to make types as preserved (pattern recognition) and keep the reflection code paths
- Use codegen (could be in the linker) to replace reflection code paths with generated code

Based on the work we did in the hackathon, the first path here is feasible without degrading the user experience. It's relatively costly and adds an additional cost to every reflection-based feature we build in the future.

The second path, we didn't get far enough to draw any conclusions.

----

Here's a breakdown of what's there when you go fully-aggro to reduce size.

Total: 25.68 mb - 183 files
Managed (total): 9.31 mb - 122 files
    ASP.NET Core: 1.66 mb - 52 files
    BCL: 7.44 - 65 files
Native (total): 16.3 mb - 55 files
    Windows shims: 844 kb - 40 files
Misc: 137kb - 6 files (json and config files)

So, about 2/3 of what's there is native dependencies. 

It might be possible to make more ASP.NET Core changes to reduce the amount of managed code here, but it would be very hard to have a big impact because of the proportion of size that is native.

**3. Publishing with *aggro* mode will have an effect on the overall working set, as assemblies that need to be read are smaller.**

Proven: this is confirmed by small - 8%

**4. Publishing with *trimming* or *aggro* mode will moderately improve the startup performance as size and number of files that need to be read are smaller.**

Not confirmed: this either doesn't improve or the effect was too small to be observed.

**5. A *hard-wired* startup experience will have better startup performance than the current MVC experience (scanning) by around 100ms. This means our toy framework should have better startup performance because its doing less work.**

Proven: We absolutely blew this out of the water. We ended up cutting about 300ms off of startup time without resulting in any tricky code-generation techniques. We need to do some further investigation here to see how we can fold these improvements into the product.

**6. There's a signficant reduction in working set to be realized from avoiding features like expressions or ref-emit used to generate dynamic code in ASP.NET Core.**

No result: We didn't get far enough to prove or disprove this. We have runtime knobs and dials that allow us to toggle off ref-emit in a few places, and it would be worth investigating. One of the team members was working on a compile-time DI replacement, but we didn't get far enough to see it work.
