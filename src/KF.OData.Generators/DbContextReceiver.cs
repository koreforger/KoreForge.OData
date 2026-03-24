using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KF.OData.Generators;

/// <summary>
/// Collects [assembly: GenerateODataFor(typeof(...))] attribute usages for OData controller generation.
/// </summary>
internal sealed class DbContextReceiver : ISyntaxReceiver
{
    public List<AttributeSyntax> CandidateAttributes { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is AttributeListSyntax attributeList
            && attributeList.Target?.Identifier.ValueText == "assembly")
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name.EndsWith("GenerateODataFor", StringComparison.Ordinal)
                    || name.EndsWith("GenerateODataForAttribute", StringComparison.Ordinal))
                {
                    CandidateAttributes.Add(attribute);
                }
            }
        }
    }
}
