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