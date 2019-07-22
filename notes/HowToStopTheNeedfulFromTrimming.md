# How to stop the needful from trimming

Notes by @anurse

You can opt-out of the linker XML using MSBuild property 'UseLinkerXml=false'

This is a guide on what breaks with aggressive linking

* `webHostBuilder.UseStartup` means the linker doesn't know Startup.Configure is called.
    * Solutions:
        * 