﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Modules;

public static class ModuleLoader
{
    private const string MODULE_SECTION_PATH = "FeatureFlags:Modules";
    private const string MODULES_DIRECTORY = "Modules";
    private const string LOGGER_NAME = "ModuleManager";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LoadModules(IMvcCoreBuilder mvcCoreBuilder, IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
    {
        var modulesSection = configuration.GetSection(MODULE_SECTION_PATH);
        if (!modulesSection.Exists())
        {
            return;
        }

        var logger = CreateLogger(mvcCoreBuilder.Services);
        var modules = modulesSection.Get<Dictionary<string, bool>>();

        foreach (var (moduleName, isEnabled) in modules)
        {
            if (!isEnabled)
            {
                logger.LogInformation("Module '{ModuleName}' is disabled.", moduleName);
                continue;
            }

            LoadModule(moduleName, hostingEnvironment, mvcCoreBuilder, logger);
        }
    }

    private static void LoadModule(string moduleName, IWebHostEnvironment hostingEnvironment,
            IMvcCoreBuilder mvcCoreBuilder, ILogger logger)
    {
        var modulePath = GetModulePath(hostingEnvironment, moduleName);
        var moduleFile = Path.Combine(modulePath, $"{moduleName}.dll");

        if (!File.Exists(moduleFile))
        {
            logger.LogWarning("Module '{ModuleName}' does not exist at path: {ModulePath}",
                moduleName, moduleFile);
            return;
        }

        try
        {
            var assemblyLoadContext = new ModuleLoadContext();
            LoadModuleDependencies(modulePath, assemblyLoadContext, mvcCoreBuilder, logger);
            logger.LogInformation("Module '{ModuleName}' has been successfully loaded.", moduleName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load module '{ModuleName}'. Error: {Error}",
                moduleName, ex.Message);
        }
    }
    private static void LoadModuleDependencies(string modulePath, ModuleLoadContext assemblyLoadContext,
            IMvcCoreBuilder mvcCoreBuilder, ILogger logger)
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.Location)
            .ToHashSet();

        foreach (var dependencyDll in Directory.GetFiles(modulePath, "*.dll"))
        {
            if (loadedAssemblies.Contains(dependencyDll))
                continue;

            try
            {
                var assembly = assemblyLoadContext.LoadFromAssemblyPath(dependencyDll);
                AddApplicationPart(mvcCoreBuilder, assembly);
                logger.LogDebug("Successfully loaded dependency: {Dependency}", dependencyDll);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load dependency: {Dependency}", dependencyDll);
            }
        }
    }

    private static string GetModulePath(IWebHostEnvironment hostingEnvironment, string moduleName)
           => Path.Combine(hostingEnvironment.ContentRootPath, MODULES_DIRECTORY, moduleName);

    private static ILogger CreateLogger(IServiceCollection services)
        => services.BuildServiceProvider()
            .GetService<ILoggerFactory>()
            ?.CreateLogger(LOGGER_NAME)
            ?? throw new InvalidOperationException("Logger factory not found in services.");

    private static void AddApplicationPart(IMvcCoreBuilder mvcCoreBuilder, Assembly assembly)
    {
        mvcCoreBuilder.AddApplicationPart(assembly);

        var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, false);
        foreach (var relatedAssembly in relatedAssemblies)
        {
            var applicationPartFactory = ApplicationPartFactory.GetApplicationPartFactory(relatedAssembly);
            foreach (var part in applicationPartFactory.GetApplicationParts(relatedAssembly))
                mvcCoreBuilder.PartManager.ApplicationParts.Add(part);
        }
    }

    private class ModuleLoadContext : AssemblyLoadContext
    {
        public ModuleLoadContext() : base(isCollectible: true)
        {

        }
    }
}