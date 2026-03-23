using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KF.OData.Generators;

/// <summary>
/// Roslyn source generator that inspects DbContext classes and generates:
/// - OData CRUD controllers per non-ignored DbSet entity
/// - IEdmModelConfigurator per DbContext for EDM registration
/// </summary>
[Generator]
public sealed class ODataSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new DbContextReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not DbContextReceiver receiver)
            return;

        var compilation = context.Compilation;

        foreach (var candidateClass in receiver.CandidateContexts)
        {
            var semanticModel = compilation.GetSemanticModel(candidateClass.SyntaxTree);
            var symbol = semanticModel.GetDeclaredSymbol(candidateClass) as INamedTypeSymbol;
            if (symbol is null)
                continue;

            var contextInfo = DbContextAnalyzer.TryExtract(symbol, compilation);
            if (contextInfo is null)
                continue;

            // Generate controllers for non-ignored entities
            foreach (var entity in contextInfo.EntitySets.Where(e => !e.IsIgnored))
            {
                if (entity.KeyPropertyName is null)
                {
                    // Report diagnostic for entities without a detectable key
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "KFODATA001",
                            "No primary key detected",
                            "Entity '{0}' has no detectable primary key. Generated controller will throw at runtime.",
                            "KoreForge.OData",
                            DiagnosticSeverity.Warning,
                            isEnabledByDefault: true),
                        candidateClass.GetLocation(),
                        entity.EntityTypeName));
                }

                var controllerSource = ControllerEmitter.Emit(contextInfo, entity);
                context.AddSource($"{entity.EntityTypeName}Controller.g.cs", controllerSource);
            }

            // Generate EDM configurator per context
            var edmSource = EdmConfiguratorEmitter.Emit(contextInfo);
            context.AddSource($"{contextInfo.ContextPrefix}EdmConfigurator.g.cs", edmSource);
        }
    }
}
