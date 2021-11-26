namespace StringDecryptor.Core;

internal class Context {

    private readonly ModuleDefinition _moduleDefinition;
    private readonly ILogger _logger;

    /// <summary>
    /// <see cref="Context"/> Constructor.
    /// </summary>
    public Context(string modulePath)
        : this(ModuleDefinition.FromFile(modulePath)) { }

    /// <summary>
    /// <see cref="Context"/> Constructor.
    /// </summary>
    public Context(ModuleDefinition moduleDefinition) {
        _moduleDefinition = moduleDefinition;
        _logger = new LoggerConfiguration().WriteTo.Console(theme: AnsiConsoleTheme.Code).CreateLogger();
    }

    /// <summary>
    /// Writes Decrypted Module Into Disk.
    /// </summary>
    public void WriteModule() {
        string modulePath = _moduleDefinition.FilePath;

        _moduleDefinition.Write(modulePath.Insert(modulePath.Length - 4, ".Decrypted"));
    }

    /// <summary>
    /// Gets Context Module.
    /// </summary>
    public ModuleDefinition Module => _moduleDefinition;

    /// <summary>
    /// Gets Context Logger.
    /// </summary>
    public ILogger Logger => _logger;

}