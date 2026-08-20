#!/usr/bin/env dotnet-script
// Flatten compiler-generated async/iterator state machines in Cobertura XML
// so crap4dotnet can match coverage to the original source methods.

using System.Text.RegularExpressions;
using System.Xml.Linq;

if (Args.Count < 2)
{
    Console.Error.WriteLine("Usage: dotnet script flatten-cobertura.csx -- <output.xml> <coverage.cobertura.xml> [more.xml ...]");
    Environment.Exit(1);
}

var outputPath = Path.GetFullPath(Args[0]);
var inputs = Args.Skip(1).Select(Path.GetFullPath).ToList();
foreach (var input in inputs.Where(p => !File.Exists(p)))
{
    Console.Error.WriteLine($"Coverage file not found: {input}");
    Environment.Exit(1);
}

var merged = CoberturaAsyncCoverageFlattener.FlattenAndMerge(inputs.Select(File.ReadAllText));
var outputDir = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(outputDir))
{
    Directory.CreateDirectory(outputDir);
}
File.WriteAllText(outputPath, merged);
Console.WriteLine($"Wrote flattened Cobertura to {outputPath}");

public static class CoberturaAsyncCoverageFlattener
{
    private static readonly Regex StateMachineClassName = new Regex(
        @"^(?<parent>.+)/(<(?<method>[^>]+)>d__\d+)",
        RegexOptions.Compiled);

    public static string FlattenAndMerge(IEnumerable<string> coberturaXmlDocuments)
    {
        var documents = coberturaXmlDocuments
            .Select(xml => XDocument.Parse(xml, LoadOptions.PreserveWhitespace))
            .ToList();
        if (documents.Count == 0)
        {
            throw new InvalidOperationException("No Cobertura documents to flatten.");
        }

        foreach (var document in documents)
        {
            FlattenDocument(document);
        }

        var primary = documents[0];
        for (var i = 1; i < documents.Count; i++)
        {
            MergeLineHits(primary, documents[i]);
        }

        return primary.ToString();
    }

    public static void FlattenDocument(XDocument document)
    {
        foreach (var package in document.Descendants("package").ToList())
        {
            var classes = package.Element("classes") ?? package;
            var byName = classes.Elements("class")
                .GroupBy(c => Attr(c, "name"))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            foreach (var classElement in classes.Elements("class").ToList())
            {
                var name = Attr(classElement, "name");
                var match = StateMachineClassName.Match(name);
                if (!match.Success)
                {
                    continue;
                }

                var parentName = match.Groups["parent"].Value;
                var methodName = match.Groups["method"].Value;
                XElement parent;
                if (!byName.TryGetValue(parentName, out parent))
                {
                    parent = CreateParentClass(classElement, parentName);
                    classes.Add(parent);
                    byName[parentName] = parent;
                }

                CopyLines(classElement, parent);
                EnsureMethod(parent, methodName, classElement);
            }
        }
    }

    private static void CopyLines(XElement sourceClass, XElement parentClass)
    {
        var parentLines = parentClass.Element("lines") ?? CreateLines(parentClass);
        var existing = parentLines.Elements("line")
            .ToDictionary(l => Attr(l, "number"), l => l, StringComparer.Ordinal);

        var sourceLines = sourceClass.Element("lines");
        if (sourceLines == null)
        {
            return;
        }

        foreach (var line in sourceLines.Elements("line").ToList())
        {
            var number = Attr(line, "number");
            XElement dest;
            if (existing.TryGetValue(number, out dest))
            {
                dest.SetAttributeValue("hits", MaxHits(dest, line));
            }
            else
            {
                var added = new XElement(line);
                parentLines.Add(added);
                existing[number] = added;
            }
        }
    }

    private static void EnsureMethod(XElement parentClass, string methodName, XElement stateMachineClass)
    {
        var methods = parentClass.Element("methods") ?? CreateMethods(parentClass);
        if (methods.Elements("method").Any(m =>
                string.Equals(Attr(m, "name"), methodName, StringComparison.Ordinal)))
        {
            return;
        }

        var methodsElement = stateMachineClass.Element("methods");
        var template = methodsElement == null ? null : methodsElement.Elements("method").FirstOrDefault();
        if (template == null)
        {
            return;
        }

        var clone = new XElement(template);
        clone.SetAttributeValue("name", methodName);
        methods.Add(clone);
    }

    private static void MergeLineHits(XDocument target, XDocument source)
    {
        var targetClasses = target.Descendants("class")
            .GroupBy(c => Attr(c, "filename") + "|" + Attr(c, "name"))
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var targetPackages = target.Descendants("package")
            .GroupBy(p => Attr(p, "name"))
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var targetPackagesRoot = target.Descendants("packages").FirstOrDefault();
        if (targetPackagesRoot == null)
        {
            return;
        }

        foreach (var sourcePackage in source.Descendants("package"))
        {
            var packageName = Attr(sourcePackage, "name");
            XElement targetPackage;
            if (!targetPackages.TryGetValue(packageName, out targetPackage))
            {
                targetPackage = new XElement(sourcePackage);
                targetPackagesRoot.Add(targetPackage);
                targetPackages[packageName] = targetPackage;
                foreach (var added in targetPackage.Descendants("class"))
                {
                    targetClasses[Attr(added, "filename") + "|" + Attr(added, "name")] = added;
                }

                continue;
            }

            var targetClassesContainer = targetPackage.Element("classes") ?? targetPackage;
            foreach (var sourceClass in sourcePackage.Descendants("class"))
            {
                var key = Attr(sourceClass, "filename") + "|" + Attr(sourceClass, "name");
                XElement targetClass;
                if (!targetClasses.TryGetValue(key, out targetClass))
                {
                    var clone = new XElement(sourceClass);
                    targetClassesContainer.Add(clone);
                    targetClasses[key] = clone;
                    continue;
                }

                CopyLines(sourceClass, targetClass);
            }
        }
    }

    private static XElement CreateParentClass(XElement stateMachineClass, string parentName)
    {
        var parent = new XElement("class");
        parent.SetAttributeValue("name", parentName);
        parent.SetAttributeValue("filename", Attr(stateMachineClass, "filename"));
        parent.SetAttributeValue("line-rate", Attr(stateMachineClass, "line-rate"));
        parent.SetAttributeValue("branch-rate", Attr(stateMachineClass, "branch-rate"));
        parent.SetAttributeValue("complexity", Attr(stateMachineClass, "complexity"));
        parent.Add(new XElement("methods"));
        parent.Add(new XElement("lines"));
        return parent;
    }

    private static XElement CreateLines(XElement parentClass)
    {
        var lines = new XElement("lines");
        parentClass.Add(lines);
        return lines;
    }

    private static XElement CreateMethods(XElement parentClass)
    {
        var methods = new XElement("methods");
        parentClass.AddFirst(methods);
        return methods;
    }

    private static int MaxHits(XElement left, XElement right)
    {
        int leftHits;
        int rightHits;
        if (!int.TryParse(Attr(left, "hits"), out leftHits))
        {
            leftHits = 0;
        }
        if (!int.TryParse(Attr(right, "hits"), out rightHits))
        {
            rightHits = 0;
        }
        return Math.Max(leftHits, rightHits);
    }

    private static string Attr(XElement element, string name)
    {
        var attribute = element.Attribute(name);
        return attribute == null ? "" : attribute.Value;
    }
}
