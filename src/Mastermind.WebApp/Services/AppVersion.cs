namespace Mastermind.WebApp.Services;

// The version the build was stamped with (<Version> in Directory.Build.props), shown as
// major.minor.patch. It comes from the assembly's name, which trimming never strips,
// rather than from an attribute a trimmed WebAssembly publish could drop.
public static class AppVersion
{
    // Every SDK-built assembly carries a version, so it is never null here.
    public static string Current { get; } = Format(typeof(AppVersion).Assembly.GetName().Version!);

    // A two-part version such as 1.3 has no patch (Build is -1), which reads as 1.3.0.
    public static string Format(Version version) => $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
}
