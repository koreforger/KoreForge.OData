using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KF.OData.Generators;

/// <summary>
/// Collects DbContext classes that have DbSet properties -- candidates for OData controller generation.
/// </summary>
internal sealed class DbContextReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> CandidateContexts { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax classDecl)
        {
            // Quick filter: check if any base type looks like DbContext
            if (classDecl.BaseList?.Types.Count > 0)
            {
                CandidateContexts.Add(classDecl);
            }
        }
    }
}
