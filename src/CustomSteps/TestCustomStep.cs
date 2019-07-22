using Mono.Linker;
using Mono.Linker.Steps;
using System;

namespace CustomSteps
{
    public class TestCustomStep : IStep
    {
        public void Process(LinkContext context)
        {
            if (Environment.GetEnvironmentVariable("DEBUG_STEP") == "1")
            {
                while (!System.Diagnostics.Debugger.IsAttached);
            }

            Console.WriteLine("It's me, custom step!");
        }
    }
}
