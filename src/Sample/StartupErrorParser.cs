using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample
{
#if false
    public class StartupErrorParser
    {

        private static void HandleDIError(AggregateException ex)
        {
            var assemblies = new Dictionary<string, List<string>>();

            foreach (var exception in ex.InnerExceptions)
            {
                (var type, var assembly) = ParseException(exception);

                if (!assemblies.TryGetValue(assembly, out var types))
                {
                    types = new List<string>();
                    assemblies.Add(assembly, types);
                }

                types.Add(type);
            }

            WriteXml(assemblies);
        }

        private static void WriteXml(Dictionary<string, List<string>> items)
        {
            var document = new XDocument();

            var root = new XElement(XName.Get("linker"));
            document.Add(root);

            foreach (var kvp in items)
            {
                var assemblyElement = new XElement(XName.Get("assembly"));
                assemblyElement.Add(new XAttribute(XName.Get("fullname"), kvp.Key));
                root.Add(assemblyElement);

                foreach (var type in kvp.Value)
                {
                    var typeElement = new XElement(XName.Get("type"));
                    typeElement.Add(new XAttribute(XName.Get("fullname"), type));
                    assemblyElement.Add(typeElement);
                }
            }

            document.Save("Linker.xml");
        }

        private static (string, string) ParseException(Exception exception)
        {
            var match = Regex.Match(exception.Message, "A suitable constructor for type '(?<typename>.*)' could not be located");
            if (!match.Success)
            {
                match = Regex.Match(exception.Message, "'(?<typename>.*)', on 'Microsoft.Extensions.Options.OptionsMonitor`1[TOptions]' violates the constraint of type 'TOptions'.");
            }

            if (!match.Success)
            {
                throw exception;
            }

            var type = match.Groups[1].Value;

            // Look for closed generics and strip it out.
            var index = type.IndexOf('`');
            if (index >= 0)
            {
                do
                {
                    index++;
                }
                while (char.IsDigit(type[index]));

                type = type.Substring(0, index);
            }


            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = assembly.GetTypes();
                foreach (var t in types)
                {
                    if (t.FullName == type)
                    {
                        return (type, new AssemblyName(assembly.FullName).Name);
                    }
                }
            }

            throw new InvalidOperationException($"Couldn't find assembly for type {type}");
        }
    }
#endif
}
