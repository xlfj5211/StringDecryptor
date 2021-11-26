global using AsmResolver;
global using AsmResolver.DotNet;
global using AsmResolver.DotNet.Code.Cil;
global using AsmResolver.DotNet.Collections;
global using AsmResolver.PE.DotNet.Cil;
global using CommandLine;
global using Echo.Concrete.Values.ValueType;
global using Echo.Core.Emulation;
global using Echo.DataFlow;
global using Echo.DataFlow.Analysis;
global using Echo.Platforms.AsmResolver;
global using Echo.Platforms.AsmResolver.Emulation;
global using Echo.Platforms.AsmResolver.Emulation.Values;
global using Echo.Platforms.AsmResolver.Emulation.Values.Cli;
global using Serilog;
global using Serilog.Sinks.SystemConsole.Themes;
global using StringDecryptor.Core;
global using System.Collections;
global using System.Reflection;
global using System.Reflection.Emit;

Console.Title = "StringDecryptor";

var decryptors = typeof(IStringDecryptor).Module.GetTypes()
            .Where(type => !type.IsInterface && typeof(IStringDecryptor).IsAssignableFrom(type))
            .Select(type => (IStringDecryptor)Activator.CreateInstance(type)!);

Parser.Default.ParseArguments<CommandLineOptions>(args)
    .WithParsed((options) => {

        var context = new Context(options.ModulePath);
        var decryptor = decryptors.First(decryptor => decryptor.Type == options.DecoderType);

        if (decryptor.Initialize(context)) {
            decryptor.Decrypt(context);
        }
        else {
            context.Logger.Warning("Initialization Failure in {0}.", decryptor.Type);
        }

        context.WriteModule();

        context.Logger.Information("Finished.");

        Console.Title = "StringDecryptor - Finished";

        Console.ReadKey();
    })
    .WithNotParsed((errors) => {
        Console.WriteLine("Errors: {0}", string.Join(", ", errors.Select(ex => ex.Tag)));
        Console.WriteLine();
        Console.WriteLine("Available Decryptors: {0}", string.Join(", ", decryptors.Select(decryptor => decryptor.Type)));
        Console.ReadKey();
    });