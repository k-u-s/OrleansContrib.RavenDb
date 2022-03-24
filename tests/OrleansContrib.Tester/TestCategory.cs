using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace OrleansContrib.Tester;

[TraitDiscoverer("CategoryDiscoverer", "TestExtensions")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class TestCategoryAttribute : Attribute, ITraitAttribute
{
    public TestCategoryAttribute(string category) { }
}

public class CategoryDiscoverer : ITraitDiscoverer
{
    public CategoryDiscoverer(IMessageSink diagnosticMessageSink)
    {
    }

    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        var ctorArgs = traitAttribute.GetConstructorArguments().ToList();
        yield return new KeyValuePair<string, string>("Category", ctorArgs[0].ToString());
    }
}
