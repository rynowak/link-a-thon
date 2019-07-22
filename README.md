# link-a-thon

Welcome to the link-a-thon (our very own project) - a.k.a. uController.

The goals of this project are to explore building linker-friendly web frameworks and learn something about:
- Optimizing for memory consumption
- Optimizing for size (of published app)
- Optimizing for startup time
- Using code-generation to make frameworks more static
- Using linker extensibility to recognize patterns


## What's here

This is a rebranded version of David Fowler's toy web framework (https://github.com/davidfowl/Web.Framework). We're using this as a starting point because it's far less complex that MVC.

Also there's a sample project here that's ready to be used with the linker.


## Getting started

Make sure you have a relatively new dotnet SDK install (https://github.com/dotnet/core-sdk#installers-and-binaries)

I've got this working with 3.0.100-preview8-013379

There's a sample in the `src/Sample` directory which can do some hello world type stuff.


# Samples

Publishing the sample:

```txt
dotnet publish -c Release -r [win10-64|linux-x64]
```

You'll see a message like the following, which means that its working:

```txt
Optimizing assemblies for size, which may change the behavior of the app. Be sure to test after publishing. See: https://aka.ms/dotnet-illink
```

Running the published sample:

```txt
cd bin\Release\netcoreapp3.0\win10-x64\publish\
dotnet Sample.dll
(visit http://localhost:5000/)
```

If you're messing with publishing, you probably want to delete the published output between runs. The whole point is to omit binaries that are unused (optimizing for size), and incremental publishing won't delete the files if they already exist. 

Run the linker aggressively (things will break):

This will trim out ALL un-used code (according to the linker). Expect DI to mess up.

```txt
dotnet publish ./src/Sample -c Release -r win-x64 -p:LinkAggressively=true
```

You'll see this message:

```
Linker has gone aggro. Yeeting EVERYTHING.
```

## Things we want to do

This is a brainstormed list rather than a set of instructions and assignments. I tried to organize these into work-streams. There's a lot we can do parallelize.

Linker extensbility:
- Create a project to run a *pass* in the linker
- Use linker extensiblity to pattern-match DI and root types that are registered as services
- Use linker extensiblity to codegen:
    - Controller thunks
    - Model binding
    - DI
    - Routing

Linker deltas:
- Turn on `LinkAggressively` and then add stuff to the Linker.xml file until the project works again. (Do this in a branch)
- Understand what's making up the bulk of the remaining managed code and where it makes sense to be there (including BCL, ASP.NET Core types)
- Play with the linker analyzer and `\\fxcore\tools\apireviewer` to understand this info

Metrics:
- What's the delta between this toy web framework and an MVC REST API (ignore views)? in terms of:
    - Working set (after first request)
    - Size on disk
    - Startup time
- What are the costly parts of MVC with the same set of factors? (measure and profile)
- What alternative design choices can we try out with uController and what's the difference in terms of metrics?
- How much does trimming with the default settings affect these metrics?

## Resources

These is the best set of instructions/guide for the linker: 
- https://github.com/dotnet/core/blob/master/samples/linker-instructions.md 
- https://github.com/dotnet/core/blob/master/samples/linker-instructions-advanced.md

Not all of it is correct :(

-----

To compare the output of the linker before/after you can use `\\fxcore\tools\apireviewer`. You should compare `obj\Release\netcoreapp3.0\<rid>` with `obj\Release\netcoreapp3.0\<rid>\linked`.

-----

Mono has a tool for examining what decisions the linker made and why. You can read about that here: https://github.com/mono/linker/blob/master/src/analyzer/README.md

The file that you need is output in `obj\Release\netcoreapp3.0\<rid>\linked`

I haven't tried this tool yet. I think if you want to try it you will need to build it from source.