// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Generator
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Operations;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Generates statically typed direct-method descriptors for each assembly that
    /// declares method controllers.
    /// </summary>
    [Generator]
    public sealed class MethodRouterDescriptorGenerator : IIncrementalGenerator
    {
        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var controllers = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax
                    {
                        BaseList: not null
                    },
                static (syntaxContext, _) => GetController(syntaxContext))
                .Where(static controller => controller is not null)
                .Collect();

            context.RegisterSourceOutput(controllers, static (productionContext, source) =>
            {
                var values = source.Where(static controller => controller is not null)
                    .Select(static controller => controller!)
                    .GroupBy(static controller => controller, SymbolEqualityComparer.Default)
                    .Select(static group => (INamedTypeSymbol)group.Key!)
                    .OrderBy(static controller => controller.ToDisplayString(),
                        StringComparer.Ordinal)
                    .ToArray();
                if (values.Length != 0)
                {
                    productionContext.AddSource("GeneratedMethodRouterDescriptors.g.cs",
                        Generate(values));
                }
            });

            var serviceRegistrations = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is InvocationExpressionSyntax,
                static (syntaxContext, _) => GetServiceRegistration(syntaxContext))
                .Where(static registration => registration is not null)
                .Collect();

            context.RegisterSourceOutput(serviceRegistrations,
                static (productionContext, source) =>
                {
                    var registrations = source.Where(static registration =>
                            registration is not null)
                        .Select(static registration => registration!)
                        .GroupBy(static registration => registration.Key,
                            StringComparer.Ordinal)
                        .Select(static group => group.First())
                        .OrderBy(static registration => registration.Key,
                            StringComparer.Ordinal)
                        .ToArray();
                    if (registrations.Length != 0)
                    {
                        productionContext.AddSource(
                            "GeneratedServiceForwardingTable.g.cs",
                            GenerateServiceForwardingTable(registrations));
                    }
                });
        }

        private static INamedTypeSymbol? GetController(
            GeneratorSyntaxContext context)
        {
            if (context.Node is not ClassDeclarationSyntax declaration)
            {
                return null;
            }

            var symbol = context.SemanticModel.GetDeclaredSymbol(declaration);
            return symbol is not null && symbol.AllInterfaces.Any(
                static type => type.ToDisplayString() ==
                    "Azure.IIoT.OpcUa.Core.Rpc.Router.IMethodController")
                ? symbol
                : null;
        }

        private static ServiceRegistration? GetServiceRegistration(
            GeneratorSyntaxContext context)
        {
            if (context.Node is not InvocationExpressionSyntax invocation ||
                context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol
                    method ||
                method.TypeArguments.Length != 1)
            {
                return null;
            }
            var methodName = method.Name;
            if (methodName is not "AddSingletonAsImplementedInterfaces" and
                not "AddScopedAsImplementedInterfaces" and
                not "AddTransientAsImplementedInterfaces" and not "AddAs")
            {
                return null;
            }
            if (method.ContainingType.ToDisplayString() !=
                "Microsoft.Extensions.DependencyInjection.ServiceCollectionForwardingEx")
            {
                return null;
            }
            var implementation = method.TypeArguments[0] as INamedTypeSymbol;
            if (implementation is null)
            {
                return null;
            }
            var explicitArguments = invocation.ArgumentList.Arguments.Skip(1).ToArray();
            var explicitOperations = methodName == "AddAs"
                ? explicitArguments.Select(argument => context.SemanticModel.GetOperation(
                    argument.Expression) as ITypeOfOperation).ToArray()
                : Array.Empty<ITypeOfOperation?>();
            var hasUnsupportedExplicitTypes = methodName == "AddAs" &&
                explicitOperations.Any(static operation => operation is null);
            var explicitServices = methodName == "AddAs"
                ? explicitOperations.Where(static operation => operation is not null)
                    .Select(static operation => operation!.TypeOperand).ToArray()
                : Array.Empty<ITypeSymbol>();
            var services = methodName == "AddAs"
                ? explicitServices
                : implementation.AllInterfaces.Where(static service =>
                    service.ToDisplayString() != "System.IDisposable" &&
                    service.ToDisplayString() != "System.IAsyncDisposable")
                    .Cast<ITypeSymbol>().ToArray();
            return new ServiceRegistration(implementation, services,
                methodName == "AddAs", hasUnsupportedExplicitTypes);
        }

        private static string GenerateServiceForwardingTable(
            IReadOnlyList<ServiceRegistration> registrations)
        {
            var automatic = registrations.Where(static registration =>
                !registration.IsExplicit).ToArray();
            var explicitRegistrations = registrations.Where(static registration =>
                registration.IsExplicit).ToArray();
            var source = new StringBuilder();
            source.AppendLine("// <auto-generated />");
            source.AppendLine("#pragma warning disable CS1591");
            source.AppendLine("namespace Microsoft.Extensions.DependencyInjection");
            source.AppendLine("{");
            source.AppendLine("    using System;");
            source.AppendLine("    using System.Runtime.CompilerServices;");
            source.AppendLine();
            source.AppendLine("    internal static class GeneratedServiceForwardingTable");
            source.AppendLine("    {");
            foreach (var unsupported in registrations.Where(static registration =>
                registration.HasUnsupportedExplicitTypes))
            {
                source.Append("#error AddAs<")
                    .Append(TypeName(unsupported.Implementation))
                    .AppendLine("> requires literal typeof(...) service types.");
            }
            source.AppendLine("        [ModuleInitializer]");
            source.AppendLine("        internal static void Register()");
            source.AppendLine("        {");
            source.AppendLine("            ServiceCollectionForwardingEx.RegisterGeneratedTable(");
            source.AppendLine("                AddForwarders, AddExplicitRegistration);");
            source.AppendLine("        }");
            source.AppendLine();
            source.AppendLine("        private static bool AddForwarders(IServiceCollection services,");
            source.AppendLine("            Type implementationType, ServiceLifetime lifetime)");
            source.AppendLine("        {");
            foreach (var registration in automatic)
            {
                source.Append("            if (implementationType == typeof(")
                    .Append(TypeName(registration.Implementation))
                    .AppendLine("))");
                source.AppendLine("            {");
                foreach (var service in registration.Services)
                {
                    source.Append("                ServiceCollectionForwardingEx.AddForward<")
                        .Append(TypeName(registration.Implementation))
                        .Append(", ")
                        .Append(TypeName(service))
                        .AppendLine(">(services, lifetime);");
                }
                source.AppendLine("                return true;");
                source.AppendLine("            }");
            }
            source.AppendLine("            return false;");
            source.AppendLine("        }");
            source.AppendLine();
            source.AppendLine("        private static bool AddExplicitRegistration(");
            source.AppendLine("            IServiceCollection services, Type implementationType,");
            source.AppendLine("            ServiceLifetime lifetime, Type[] serviceTypes)");
            source.AppendLine("        {");
            foreach (var registration in explicitRegistrations)
            {
                source.Append("            if (implementationType == typeof(")
                    .Append(TypeName(registration.Implementation))
                    .Append(") && serviceTypes.Length == ")
                    .Append(registration.Services.Count.ToString(CultureInfo.InvariantCulture));
                for (var i = 0; i < registration.Services.Count; i++)
                {
                    source.Append(" && serviceTypes[")
                        .Append(i.ToString(CultureInfo.InvariantCulture))
                        .Append("] == typeof(")
                        .Append(TypeName(registration.Services[i]))
                        .Append(')');
                }
                source.AppendLine(")");
                source.AppendLine("            {");
                source.Append("                ServiceCollectionForwardingEx.AddImplementation<")
                    .Append(TypeName(registration.Implementation))
                    .AppendLine(">(services, lifetime);");
                foreach (var service in registration.Services)
                {
                    source.Append("                ServiceCollectionForwardingEx.AddForward<")
                        .Append(TypeName(registration.Implementation))
                        .Append(", ")
                        .Append(TypeName(service))
                        .AppendLine(">(services, lifetime);");
                }
                source.AppendLine("                return true;");
                source.AppendLine("            }");
            }
            source.AppendLine("            return false;");
            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine("}");
            source.AppendLine("#pragma warning restore CS1591");
            return source.ToString();
        }

        private static string Generate(INamedTypeSymbol[] controllers)
        {
            var methods = controllers.SelectMany(GetMethods).ToArray();
            var source = new StringBuilder();
            source.AppendLine("// <auto-generated />");
            source.AppendLine("#pragma warning disable CS1591");
            source.AppendLine("namespace Azure.IIoT.OpcUa.Core.Rpc.Router");
            source.AppendLine("{");
            source.AppendLine("    using System;");
            source.AppendLine("    using System.Collections.Generic;");
            source.AppendLine("    using System.Text.Json;");
            source.AppendLine("    using System.Threading;");
            source.AppendLine("    using System.Threading.Tasks;");
            source.AppendLine();
            source.Append("    public sealed class ")
                .Append(DescriptorClassName(controllers[0].ContainingAssembly.Name))
                .AppendLine(" : IMethodRouterDescriptorProvider");
            source.AppendLine("    {");
            source.AppendLine("        public bool TryRegister(MethodRouter router,");
            source.AppendLine("            IMethodController controller,");
            source.AppendLine("            IMethodRouterJsonSerializer serializer)");
            source.AppendLine("        {");
            source.AppendLine("            ArgumentNullException.ThrowIfNull(router);");
            source.AppendLine("            ArgumentNullException.ThrowIfNull(controller);");
            source.AppendLine("            ArgumentNullException.ThrowIfNull(serializer);");
            source.AppendLine("            switch (controller)");
            source.AppendLine("            {");
            foreach (var controller in controllers)
            {
                var controllerMethods = methods.Where(method =>
                    SymbolEqualityComparer.Default.Equals(method.Controller, controller))
                    .ToArray();
                if (controllerMethods.Length == 0)
                {
                    continue;
                }
                source.Append("                case ")
                    .Append(TypeName(controller))
                    .AppendLine(" typedController:");
                foreach (var method in controllerMethods)
                {
                    foreach (var version in method.Versions)
                    {
                        source.Append("                    router.Register(\"")
                            .Append(Escape(method.Name + version))
                            .Append("\", new MethodRouteDescriptor(\"")
                            .Append(Escape(method.Method.Name))
                            .Append("\", ")
                            .Append(Filter(method))
                            .Append(", (payload, ct) => ")
                            .Append(method.WrapperName)
                            .AppendLine("(typedController, payload, ct, serializer)));");
                    }
                }
                source.AppendLine("                    return true;");
            }
            source.AppendLine("            }");
            source.AppendLine("            return false;");
            source.AppendLine("        }");
            foreach (var method in methods)
            {
                GenerateWrapper(source, method);
            }
            source.AppendLine("    }");
            source.AppendLine("}");
            source.AppendLine("#pragma warning restore CS1591");
            return source.ToString();
        }

        private static IEnumerable<MethodDescriptor> GetMethods(INamedTypeSymbol controller)
        {
            var versions = controller.GetAttributes()
                .Where(static attribute => attribute.AttributeClass?.ToDisplayString() ==
                    "Azure.IIoT.OpcUa.Core.Rpc.Router.VersionAttribute")
                .Select(static attribute => attribute.ConstructorArguments.Length == 1
                    ? attribute.ConstructorArguments[0].Value as string ?? string.Empty
                    : string.Empty)
                .ToArray();
            if (versions.Length == 0)
            {
                versions = [string.Empty];
            }
            var index = 0;
            foreach (var method in controller.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.MethodKind != MethodKind.Ordinary ||
                    method.DeclaredAccessibility != Accessibility.Public ||
                    method.IsStatic || IsIgnored(method) || !IsSupportedReturn(method.ReturnType))
                {
                    continue;
                }
                yield return new MethodDescriptor(controller, method, versions,
                    GetFilter(controller, method),
                    GetMethodName(method.Name), index++);
            }
        }

        private static void GenerateWrapper(StringBuilder source,
            MethodDescriptor descriptor)
        {
            source.AppendLine();
            source.Append("        private static async ValueTask<ReadOnlyMemory<byte>> ")
                .Append(descriptor.WrapperName)
                .Append('(')
                .Append(TypeName(descriptor.Controller))
                .AppendLine(" controller, ReadOnlyMemory<byte> payload, CancellationToken ct,");
            source.AppendLine("            IMethodRouterJsonSerializer serializer)");
            source.AppendLine("        {");
            var payloadParameters = descriptor.Method.Parameters
                .Where(static parameter => !IsCancellationToken(parameter.Type))
                .ToArray();
            if (payloadParameters.Length == 1)
            {
                var parameter = payloadParameters[0];
                source.Append("            var ")
                    .Append(parameter.Name)
                    .Append(" = MethodRouterJson.Deserialize<")
                    .Append(TypeName(parameter.Type))
                    .AppendLine(">(payload, serializer.GetTypeInfo<" +
                        TypeName(parameter.Type) + ">());");
            }
            else if (payloadParameters.Length > 1)
            {
                source.AppendLine("            using var document = JsonDocument.Parse(payload);");
                source.AppendLine("            var root = document.RootElement;");
                foreach (var parameter in payloadParameters)
                {
                    source.Append("            var ")
                        .Append(parameter.Name)
                        .Append(" = root.TryGetProperty(\"")
                        .Append(Escape(parameter.Name))
                        .Append("\", out var ")
                        .Append(parameter.Name)
                        .Append("Element) ? MethodRouterJson.Deserialize<")
                        .Append(TypeName(parameter.Type))
                        .Append(">(")
                        .Append(parameter.Name)
                        .Append("Element, serializer.GetTypeInfo<")
                        .Append(TypeName(parameter.Type))
                        .Append(">()) : ")
                        .Append(DefaultValue(parameter))
                        .AppendLine(";");
                }
            }
            var arguments = string.Join(", ", descriptor.Method.Parameters.Select(
                parameter => IsCancellationToken(parameter.Type) ? "ct" : parameter.Name));
            var returnType = descriptor.Method.ReturnType;
            var methodCall = "controller." + descriptor.Method.Name + "(" + arguments + ")";
            if (returnType.ToDisplayString() == "System.Threading.Tasks.Task")
            {
                source.Append("            await ")
                    .Append(methodCall)
                    .AppendLine(".ConfigureAwait(false);");
                source.AppendLine("            return ReadOnlyMemory<byte>.Empty;");
            }
            else if (returnType.ToDisplayString() == "System.Threading.Tasks.ValueTask")
            {
                source.Append("            await ")
                    .Append(methodCall)
                    .AppendLine(".ConfigureAwait(false);");
                source.AppendLine("            return ReadOnlyMemory<byte>.Empty;");
            }
            else
            {
                var resultType = ((INamedTypeSymbol)returnType).TypeArguments[0];
                if (IsAsyncEnumerable(returnType))
                {
                    source.Append("            return await MethodRouterJson.DrainAsync(")
                        .Append(methodCall)
                        .Append(", serializer.GetTypeInfo<List<")
                        .Append(TypeName(resultType))
                        .AppendLine(">>()).ConfigureAwait(false);");
                }
                else
                {
                    source.Append("            var result = await ")
                        .Append(methodCall)
                        .AppendLine(".ConfigureAwait(false);");
                    source.Append("            return MethodRouterJson.Serialize(result, serializer.GetTypeInfo<")
                        .Append(TypeName(resultType))
                        .AppendLine(">());");
                }
            }
            source.AppendLine("        }");
        }

        private static bool IsIgnored(IMethodSymbol method)
        {
            return method.GetAttributes().Any(static attribute =>
                attribute.AttributeClass?.ToDisplayString() ==
                    "Azure.IIoT.OpcUa.Core.Rpc.Router.IgnoreAttribute");
        }

        private static AttributeData? GetFilter(INamedTypeSymbol controller,
            IMethodSymbol method)
        {
            var methodFilter = method.GetAttributes().FirstOrDefault(static attribute =>
                DerivesFrom(attribute.AttributeClass,
                    "Azure.IIoT.OpcUa.Core.Rpc.Router.ExceptionFilterAttribute"));
            if (methodFilter is not null && methodFilter.AttributeClass is not null)
            {
                return methodFilter;
            }
            for (INamedTypeSymbol? current = controller; current is not null;
                current = current.BaseType)
            {
                var filter = current.GetAttributes().FirstOrDefault(static attribute =>
                    DerivesFrom(attribute.AttributeClass,
                        "Azure.IIoT.OpcUa.Core.Rpc.Router.ExceptionFilterAttribute"));
                if (filter is not null && filter.AttributeClass is not null)
                {
                    return filter;
                }
            }
            return null;
        }

        private static bool DerivesFrom(INamedTypeSymbol? type, string baseType)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (current.ToDisplayString() == baseType)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsSupportedReturn(ITypeSymbol returnType)
        {
            return returnType.ToDisplayString() == "System.Threading.Tasks.Task" ||
                returnType.ToDisplayString() == "System.Threading.Tasks.ValueTask" ||
                returnType is INamedTypeSymbol named && named.TypeArguments.Length == 1 &&
                (named.ConstructedFrom.ToDisplayString() ==
                    "System.Threading.Tasks.Task<TResult>" ||
                 named.ConstructedFrom.ToDisplayString() ==
                    "System.Threading.Tasks.ValueTask<TResult>" ||
                 named.ConstructedFrom.ToDisplayString() ==
                    "System.Collections.Generic.IAsyncEnumerable<T>");
        }

        private static bool IsAsyncEnumerable(ITypeSymbol type)
        {
            return type is INamedTypeSymbol named &&
                named.ConstructedFrom.ToDisplayString() ==
                    "System.Collections.Generic.IAsyncEnumerable<T>";
        }

        private static bool IsCancellationToken(ITypeSymbol type)
        {
            return type.ToDisplayString() == "System.Threading.CancellationToken";
        }

        private static string GetMethodName(string name)
        {
            return name.EndsWith("Async", StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "Async".Length)
                : name;
        }

        private static string TypeName(ITypeSymbol type)
        {
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        private static string DescriptorClassName(string assemblyName)
        {
            var name = new StringBuilder();
            foreach (var character in assemblyName)
            {
                name.Append(char.IsLetterOrDigit(character) ? character : '_');
            }
            return name.Append("MethodRouterDescriptors").ToString();
        }

        private static string DefaultValue(IParameterSymbol parameter)
        {
            if (!parameter.HasExplicitDefaultValue || parameter.ExplicitDefaultValue is null)
            {
                return "default";
            }
            return parameter.ExplicitDefaultValue switch
            {
                string value => "\"" + Escape(value) + "\"",
                char value => "'" + value.ToString().Replace("'", "\\'") + "'",
                bool value => value ? "true" : "false",
                float value => value.ToString("R", CultureInfo.InvariantCulture) + "F",
                double value => value.ToString("R", CultureInfo.InvariantCulture) + "D",
                decimal value => value.ToString(CultureInfo.InvariantCulture) + "M",
                _ => Convert.ToString(parameter.ExplicitDefaultValue,
                    CultureInfo.InvariantCulture) ?? "default"
            };
        }

        private static string Filter(MethodDescriptor descriptor)
        {
            return descriptor.Filter is null
                ? "null"
                : "new " + TypeName(descriptor.Filter.AttributeClass!) + "(" +
                    string.Join(", ", descriptor.Filter.ConstructorArguments
                        .Select(FormatConstant)) + ")" + FormatNamedArguments(
                            descriptor.Filter.NamedArguments);
        }

        private static string FormatNamedArguments(
            ImmutableArray<KeyValuePair<string, TypedConstant>> arguments)
        {
            return arguments.IsDefaultOrEmpty
                ? string.Empty
                : " { " + string.Join(", ", arguments.Select(argument =>
                    argument.Key + " = " + FormatConstant(argument.Value))) + " }";
        }

        private static string FormatConstant(TypedConstant constant)
        {
            if (constant.IsNull)
            {
                return "null";
            }
            if (constant.Kind == TypedConstantKind.Type)
            {
                return "typeof(" + TypeName((ITypeSymbol)constant.Value!) + ")";
            }
            if (constant.Kind == TypedConstantKind.Array)
            {
                return "new " + TypeName(constant.Type!) + " { " +
                    string.Join(", ", constant.Values.Select(FormatConstant)) + " }";
            }
            if (constant.Type?.TypeKind == TypeKind.Enum)
            {
                return "(" + TypeName(constant.Type) + ")" +
                    Convert.ToString(constant.Value, CultureInfo.InvariantCulture);
            }
            return constant.Value switch
            {
                string value => "\"" + Escape(value) + "\"",
                char value => "'" + value.ToString().Replace("'", "\\'") + "'",
                bool value => value ? "true" : "false",
                float value => value.ToString("R", CultureInfo.InvariantCulture) + "F",
                double value => value.ToString("R", CultureInfo.InvariantCulture) + "D",
                decimal value => value.ToString(CultureInfo.InvariantCulture) + "M",
                _ => Convert.ToString(constant.Value, CultureInfo.InvariantCulture) ??
                    throw new InvalidOperationException(
                        "Unsupported exception filter attribute argument.")
            };
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private sealed class ServiceRegistration
        {
            public INamedTypeSymbol Implementation { get; }
            public IReadOnlyList<ITypeSymbol> Services { get; }
            public bool IsExplicit { get; }
            public bool HasUnsupportedExplicitTypes { get; }
            public string Key { get; }

            public ServiceRegistration(INamedTypeSymbol implementation,
                IReadOnlyList<ITypeSymbol> services, bool isExplicit,
                bool hasUnsupportedExplicitTypes)
            {
                Implementation = implementation;
                Services = services;
                IsExplicit = isExplicit;
                HasUnsupportedExplicitTypes = hasUnsupportedExplicitTypes;
                Key = TypeName(implementation) + "|" + isExplicit.ToString(
                    CultureInfo.InvariantCulture) + "|" + string.Join("|",
                    services.Select(TypeName));
            }
        }

        private sealed class MethodDescriptor
        {
            public INamedTypeSymbol Controller { get; }
            public IMethodSymbol Method { get; }
            public IReadOnlyList<string> Versions { get; }
            public AttributeData? Filter { get; }
            public string Name { get; }
            public string WrapperName { get; }
            public IEnumerable<ITypeSymbol> Types { get; }

            public MethodDescriptor(INamedTypeSymbol controller, IMethodSymbol method,
                IReadOnlyList<string> versions, AttributeData? filter, string name,
                int index)
            {
                Controller = controller;
                Method = method;
                Versions = versions;
                Filter = filter;
                Name = name;
                WrapperName = "Invoke" + controller.Name + method.Name + index.ToString(
                    CultureInfo.InvariantCulture);
                Types = GetTypes(method);
            }

            private static IEnumerable<ITypeSymbol> GetTypes(IMethodSymbol method)
            {
                foreach (var parameter in method.Parameters)
                {
                    if (!IsCancellationToken(parameter.Type))
                    {
                        yield return parameter.Type;
                    }
                }
                if (method.ReturnType is INamedTypeSymbol result &&
                    result.TypeArguments.Length == 1)
                {
                    yield return result.TypeArguments[0];
                    if (IsAsyncEnumerable(method.ReturnType))
                    {
                    }
                }
            }
        }
    }
}
