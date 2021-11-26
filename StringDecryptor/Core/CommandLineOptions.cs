namespace StringDecryptor.Core;

internal class CommandLineOptions {
    /// <summary>
    /// Gets or Sets DecoderType.
    /// </summary>
    [Option(HelpText = "The Decoder Type.", Required = true)]
    public DecoderType DecoderType { get; set; }

    /// <summary>
    /// Gets or Sets ModulePath.
    /// </summary>
    [Option(HelpText = "The Module Path.", Required = true)]
    public string ModulePath { get; set; }
}