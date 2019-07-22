using Mono.Linker;
using Mono.Linker.Steps;
using System;

namespace CustomSteps
{
    public class TestCustomStep : IStep
    {
        public void Process(LinkContext context)
        {
            Console.WriteLine("It's me, custom step!");
        }
    }
}
